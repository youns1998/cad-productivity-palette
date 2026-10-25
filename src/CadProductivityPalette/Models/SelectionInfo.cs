using Autodesk.AutoCAD.DatabaseServices;

namespace CadProductivityPalette.Models;

public enum SelectionKind
{
    None,
    Multiple,
    Polyline,
    Line,
    Text,
    Block,
    Hatch,
    Dimension,
    Circle,
    Arc,
    Other
}

public sealed class SelectionInfo
{
    public static SelectionInfo Empty { get; } = new()
    {
        Heading = "선택된 객체 없음",
        Description = "도면에서 객체를 선택하면 속성과 전용 편집 도구가 표시됩니다."
    };

    public string Heading { get; init; } = "선택된 객체 없음";
    public string Description { get; init; } = string.Empty;
    public SelectionKind Kind { get; init; }
    public IReadOnlyList<PropertyItem> Properties { get; init; } = [];
    public ObjectId[] ObjectIds { get; init; } = [];
}
