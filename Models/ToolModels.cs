namespace CadProductivityPalette.Models;

public sealed record ToolAction(
    string Name,
    string CommandText,
    string ToolTip,
    string ActionKey = "command");

public sealed record ToolGroup(string Name, IReadOnlyList<ToolAction> Tools);

public sealed record ToolUsageItem(ToolAction Tool, string Meta)
{
    public string Name => Tool.Name;
    public string ToolTip => Tool.ToolTip;
}
