namespace CadProductivityPalette.Models;

public sealed record ToolAction(
    string Name,
    string CommandText,
    string ToolTip,
    string ActionKey = "command");

public sealed record ToolGroup(string Name, IReadOnlyList<ToolAction> Tools);
