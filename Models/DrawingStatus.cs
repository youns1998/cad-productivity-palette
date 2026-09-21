using Autodesk.AutoCAD.DatabaseServices;

namespace CadProductivityPalette.Models;

public enum IssueSeverity
{
    Normal,
    Warning,
    Information
}

public sealed record DrawingIssue(
    string Name,
    int Count,
    string Description,
    IssueSeverity Severity,
    ObjectId[] ObjectIds)
{
    public string Badge => Severity switch
    {
        IssueSeverity.Warning => "검토",
        IssueSeverity.Information => "참고",
        _ => "없음"
    };
    public bool CanSelect => ObjectIds.Length > 0;
    public string ActionLabel => CanSelect ? "선택 + 줌" : string.Empty;
}

public sealed class DrawingStatus
{
    public static DrawingStatus Empty { get; } = new();

    public string CurrentLayer { get; init; } = "—";
    public string CurrentTextStyle { get; init; } = "—";
    public string CurrentDimStyle { get; init; } = "—";
    public int ObjectCount { get; init; }
    public IReadOnlyList<DrawingIssue> Issues { get; init; } = [];
    public DateTime RefreshedAt { get; init; } = DateTime.Now;
}
