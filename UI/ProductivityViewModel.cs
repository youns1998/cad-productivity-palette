using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using CadProductivityPalette.Models;
using CadProductivityPalette.Services;
using CadProductivityPalette.Utils;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CadProductivityPalette.UI;

public sealed class ProductivityViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly IReadOnlyList<ToolAction> AllCatalogTools =
        ToolCatalog.Groups.SelectMany(group => group.Tools).ToArray();

    private readonly SelectionService _selectionService = new();
    private readonly DrawingStatusService _drawingStatusService = new();
    private readonly AutoCadCommandService _commandService = new();
    private readonly ToolUsageService _toolUsageService = new();
    private readonly UserSettingsService _userSettingsService = new();
    private readonly DispatcherTimer _refreshTimer;
    private Document? _subscribedDocument;
    private bool _refreshSelectionPending;
    private bool _refreshStatusPending;
    private string _selectionHeading = SelectionInfo.Empty.Heading;
    private string _selectionDescription = SelectionInfo.Empty.Description;
    private string _currentLayer = "—";
    private string _currentTextStyle = "—";
    private string _currentDimStyle = "—";
    private string _objectCount = "0";
    private string _lastRefresh = "점검 대기";
    private ObjectId[] _selectionIds = [];
    private SelectionKind _selectionKind;
    private int _selectedTabIndex;
    private bool _isSimpleMode;
    private bool _isActive;
    private bool _disposed;

    public ProductivityViewModel()
    {
        _isSimpleMode = _userSettingsService.IsSimpleMode;
        QuickToolGroups = ToolCatalog.AdditionalGroups;
        ExecuteToolCommand = new RelayCommand<ToolAction>(ExecuteTool);
        ExecuteUsageToolCommand = new RelayCommand<ToolUsageItem>(ExecuteUsageTool);
        ExecuteContextCommand = new RelayCommand<ToolAction>(ExecuteContextAction);
        SelectIssueCommand = new RelayCommand<DrawingIssue>(SelectIssue, issue => issue.CanSelect);
        RefreshCommand = new RelayCommand(RefreshVisible);
        ClearUsageCommand = new RelayCommand(ClearUsage, () => _toolUsageService.HasUsage);
        RefreshUsageTools();

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };
        _refreshTimer.Tick += OnRefreshTimerTick;

        AcApplication.DocumentManager.DocumentActivated += OnDocumentActivated;
        AcApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
        AttachDocument(AcApplication.DocumentManager.MdiActiveDocument);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<ToolGroup> QuickToolGroups { get; }
    public IReadOnlyList<ToolAction> EssentialTools => ToolCatalog.EssentialTools;
    public ObservableCollection<PropertyItem> SelectionProperties { get; } = [];
    public ObservableCollection<ToolAction> ContextActions { get; } = [];
    public ObservableCollection<ToolUsageItem> RecentTools { get; } = [];
    public ObservableCollection<ToolUsageItem> FrequentTools { get; } = [];
    public ObservableCollection<DrawingIssue> DrawingIssues { get; } = [];
    public ObservableCollection<DrawingIssue> DrawingNotes { get; } = [];
    public bool HasRecentTools => RecentTools.Count > 0;
    public bool HasFrequentTools => FrequentTools.Count > 0;
    public bool IsFullMode => !IsSimpleMode;
    public string ReviewSummary => DrawingIssues.Count == 0 ? "아직 점검 결과가 없습니다."
        : DrawingIssues.Any(issue => issue.Count > 0)
            ? $"{DrawingIssues.Count(issue => issue.Count > 0)}개 항목 검토 · 오류 확정이 아닙니다."
            : "검토 대상 없음 · 아래 3개 항목 기준";

    public RelayCommand<ToolAction> ExecuteToolCommand { get; }
    public RelayCommand<ToolUsageItem> ExecuteUsageToolCommand { get; }
    public RelayCommand<ToolAction> ExecuteContextCommand { get; }
    public RelayCommand<DrawingIssue> SelectIssueCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ClearUsageCommand { get; }

    public string SelectionHeading
    {
        get => _selectionHeading;
        private set => SetField(ref _selectionHeading, value);
    }

    public string SelectionDescription
    {
        get => _selectionDescription;
        private set => SetField(ref _selectionDescription, value);
    }

    public string CurrentLayer
    {
        get => _currentLayer;
        private set => SetField(ref _currentLayer, value);
    }

    public string CurrentTextStyle
    {
        get => _currentTextStyle;
        private set => SetField(ref _currentTextStyle, value);
    }

    public string CurrentDimStyle
    {
        get => _currentDimStyle;
        private set => SetField(ref _currentDimStyle, value);
    }

    public string ObjectCount
    {
        get => _objectCount;
        private set => SetField(ref _objectCount, value);
    }

    public string LastRefresh
    {
        get => _lastRefresh;
        private set => SetField(ref _lastRefresh, value);
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetField(ref _selectedTabIndex, value) && _isActive)
            {
                ScheduleRefresh(selection: value == 0, status: value == 1);
            }
        }
    }

    public bool IsSimpleMode
    {
        get => _isSimpleMode;
        set
        {
            if (!SetField(ref _isSimpleMode, value))
            {
                return;
            }

            _userSettingsService.SetSimpleMode(value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFullMode)));
            if (value && SelectedTabIndex != 0)
            {
                SelectedTabIndex = 0;
            }
        }
    }

    private void RefreshVisible()
    {
        ScheduleRefresh(selection: SelectedTabIndex == 0, status: SelectedTabIndex == 1);
    }

    public void SetActive(bool active)
    {
        if (_disposed || _isActive == active)
        {
            return;
        }

        _isActive = active;
        if (_isActive)
        {
            ScheduleRefresh(selection: SelectedTabIndex == 0, status: SelectedTabIndex == 1);
            return;
        }

        _refreshTimer.Stop();
        _refreshSelectionPending = false;
        _refreshStatusPending = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
        AcApplication.DocumentManager.DocumentActivated -= OnDocumentActivated;
        AcApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
        DetachDocument();
    }

    private void ExecuteTool(ToolAction tool)
    {
        if (_commandService.Execute(tool.CommandText))
        {
            _toolUsageService.Record(tool);
            RefreshUsageTools();
        }
    }

    private void ExecuteUsageTool(ToolUsageItem item)
    {
        ExecuteTool(item.Tool);
    }

    private void ClearUsage()
    {
        _toolUsageService.Clear();
        RefreshUsageTools();
    }

    private void RefreshUsageTools()
    {
        Replace(RecentTools, _toolUsageService.GetRecent(AllCatalogTools));
        Replace(FrequentTools, _toolUsageService.GetFrequent(AllCatalogTools));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasRecentTools)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasFrequentTools)));
        ClearUsageCommand.RaiseCanExecuteChanged();
    }

    private void ExecuteContextAction(ToolAction action)
    {
        switch (action.ActionKey)
        {
            case "toggle-polyline":
                _commandService.TogglePolylineClosed(_selectionIds);
                RefreshVisible();
                break;
            case "edit-block" when _selectionIds.Length > 0:
                _commandService.EditBlock(_selectionIds[0]);
                break;
            default:
                _commandService.Execute(action.CommandText, _selectionIds);
                break;
        }
    }

    private void SelectIssue(DrawingIssue issue)
    {
        _commandService.SelectAndZoom(issue);
    }

    private void OnDocumentActivated(object sender, DocumentCollectionEventArgs eventArgs)
    {
        AttachDocument(eventArgs.Document);
        RefreshVisible();
    }

    private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Document, _subscribedDocument))
        {
            DetachDocument();
            ClearForNoDocument();
        }
    }

    private void AttachDocument(Document? document)
    {
        if (ReferenceEquals(document, _subscribedDocument))
        {
            return;
        }

        DetachDocument();
        _subscribedDocument = document;
        if (_subscribedDocument is null)
        {
            return;
        }

        _subscribedDocument.ImpliedSelectionChanged += OnImpliedSelectionChanged;
        _subscribedDocument.CommandEnded += OnCommandFinished;
        _subscribedDocument.CommandCancelled += OnCommandFinished;
        _subscribedDocument.CommandFailed += OnCommandFinished;
    }

    private void DetachDocument()
    {
        if (_subscribedDocument is null)
        {
            return;
        }

        _subscribedDocument.ImpliedSelectionChanged -= OnImpliedSelectionChanged;
        _subscribedDocument.CommandEnded -= OnCommandFinished;
        _subscribedDocument.CommandCancelled -= OnCommandFinished;
        _subscribedDocument.CommandFailed -= OnCommandFinished;
        _subscribedDocument = null;
    }

    private void OnImpliedSelectionChanged(object? sender, EventArgs eventArgs)
    {
        ScheduleRefresh(selection: SelectedTabIndex == 0, status: false);
    }

    private void OnCommandFinished(object sender, CommandEventArgs eventArgs)
    {
        ScheduleRefresh(selection: SelectedTabIndex == 0, status: SelectedTabIndex == 1);
    }

    private void ScheduleRefresh(bool selection, bool status)
    {
        if (_disposed || !_isActive)
        {
            return;
        }

        _refreshSelectionPending |= selection;
        _refreshStatusPending |= status;
        _refreshTimer.Stop();
        _refreshTimer.Start();
    }

    private void OnRefreshTimerTick(object? sender, EventArgs eventArgs)
    {
        _refreshTimer.Stop();
        Document? activeDocument = AcApplication.DocumentManager.MdiActiveDocument;
        if (!ReferenceEquals(activeDocument, _subscribedDocument))
        {
            AttachDocument(activeDocument);
        }

        if (activeDocument is null)
        {
            ClearForNoDocument();
            return;
        }

        // Modeless WPF callbacks can be pumped while an AutoCAD command is active.
        // Defer database reads until the command completes to avoid nested command/
        // transaction work; CommandEnded/Cancelled/Failed will also schedule a pass.
        if (!string.IsNullOrWhiteSpace(activeDocument.CommandInProgress))
        {
            _refreshTimer.Start();
            return;
        }

        bool refreshSelection = _refreshSelectionPending;
        bool refreshStatus = _refreshStatusPending;
        _refreshSelectionPending = false;
        _refreshStatusPending = false;

        if (refreshSelection)
        {
            ApplySelection(_selectionService.Read(activeDocument));
        }

        if (refreshStatus)
        {
            ApplyStatus(_drawingStatusService.Analyze(activeDocument));
        }
    }

    private void ApplySelection(SelectionInfo selection)
    {
        _selectionIds = selection.ObjectIds;
        _selectionKind = selection.Kind;
        SelectionHeading = selection.Heading;
        SelectionDescription = selection.Description;
        Replace(SelectionProperties, selection.Properties);
        BuildContextActions();
    }

    private void ApplyStatus(DrawingStatus status)
    {
        CurrentLayer = status.CurrentLayer;
        CurrentTextStyle = status.CurrentTextStyle;
        CurrentDimStyle = status.CurrentDimStyle;
        ObjectCount = status.ObjectCount.ToString("N0");
        LastRefresh = $"갱신 {status.RefreshedAt:HH:mm:ss}";
        Replace(DrawingIssues, status.Issues.Where(issue => issue.Severity != IssueSeverity.Information));
        Replace(DrawingNotes, status.Issues.Where(issue => issue.Severity == IssueSeverity.Information));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReviewSummary)));
    }

    private void ClearForNoDocument()
    {
        ApplySelection(SelectionInfo.Empty);
        ApplyStatus(DrawingStatus.Empty);
        LastRefresh = "활성 도면 없음";
    }

    private void BuildContextActions()
    {
        ContextActions.Clear();
        IEnumerable<ToolAction> actions = _selectionKind switch
        {
            SelectionKind.Polyline =>
            [
                new("닫기 / 열기", string.Empty, "선택한 Polyline을 닫거나 엽니다.", "toggle-polyline"),
                new("Polyline 편집", "_.PEDIT", "PEDIT 실행 후 Join 등 필요한 옵션을 선택합니다.")
            ],
            SelectionKind.Text =>
            [
                new("Text 편집", "_.TEXTEDIT", "선택한 Text 또는 MText를 편집합니다.")
            ],
            SelectionKind.Block =>
            [
                new("Block 편집", string.Empty, "선택한 Block 정의를 편집합니다.", "edit-block")
            ],
            SelectionKind.Hatch =>
            [
                new("Hatch 편집", "_.HATCHEDIT", "선택한 Hatch를 편집합니다.")
            ],
            SelectionKind.Dimension =>
            [
                new("Dimension 편집", "_.DIMEDIT", "DIMEDIT 실행 후 편집 옵션과 대상을 지정합니다.")
            ],
            _ => []
        };

        foreach (ToolAction action in actions)
        {
            ContextActions.Add(action);
        }
    }


    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (T value in values)
        {
            target.Add(value);
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
