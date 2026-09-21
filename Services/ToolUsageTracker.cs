using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using CadProductivityPalette.Models;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CadProductivityPalette.Services;

public sealed class ToolUsageTracker
{
    private readonly ToolUsageService _usageService = new();
    private Document? _subscribedDocument;
    private bool _started;

    private ToolUsageTracker()
    {
    }

    public static ToolUsageTracker Instance { get; } = new();

    public event EventHandler? UsageChanged;

    public bool HasUsage => _usageService.HasUsage;

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        AcApplication.DocumentManager.DocumentActivated += OnDocumentActivated;
        AcApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
        AttachDocument(AcApplication.DocumentManager.MdiActiveDocument);
    }

    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        _started = false;
        AcApplication.DocumentManager.DocumentActivated -= OnDocumentActivated;
        AcApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
        DetachDocument();
    }

    public IReadOnlyList<ToolUsageItem> GetRecent()
    {
        return _usageService.GetRecent(ToolCatalog.AllTools);
    }

    public IReadOnlyList<ToolUsageItem> GetFrequent()
    {
        return _usageService.GetFrequent(ToolCatalog.AllTools);
    }

    public void Clear()
    {
        _usageService.Clear();
        UsageChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnDocumentActivated(object sender, DocumentCollectionEventArgs eventArgs)
    {
        AttachDocument(eventArgs.Document);
    }

    private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Document, _subscribedDocument))
        {
            DetachDocument();
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
        if (_subscribedDocument is not null)
        {
            _subscribedDocument.CommandEnded += OnCommandEnded;
        }
    }

    private void DetachDocument()
    {
        if (_subscribedDocument is null)
        {
            return;
        }

        _subscribedDocument.CommandEnded -= OnCommandEnded;
        _subscribedDocument = null;
    }

    private void OnCommandEnded(object sender, CommandEventArgs eventArgs)
    {
        ToolAction? tool = ToolCatalog.FindByGlobalCommandName(eventArgs.GlobalCommandName);
        if (tool is null)
        {
            return;
        }

        _usageService.Record(tool);
        UsageChanged?.Invoke(this, EventArgs.Empty);
    }
}
