using CadProductivityPalette.Models;

namespace CadProductivityPalette.Services;

// One catalog: a tool appears either in the always-visible essentials or in a group.
public static class ToolCatalog
{
    private static readonly HashSet<string> EssentialCommands =
        ["_.LINE", "_.PLINE", "_.MOVE", "_.COPY", "_.OFFSET", "_.TRIM", "_.MTEXT", "_.DIM"];

    public static IReadOnlyList<ToolGroup> Groups { get; } = CreateQuickToolGroups();

    public static IReadOnlyList<ToolAction> EssentialTools { get; } =
        Groups.SelectMany(group => group.Tools)
            .Where(tool => EssentialCommands.Contains(tool.CommandText))
            .ToArray();

    public static IReadOnlyList<ToolGroup> AdditionalGroups { get; } =
        Groups.Select(group => new ToolGroup(group.Name,
                group.Tools.Where(tool => !EssentialCommands.Contains(tool.CommandText)).ToArray()))
            .Where(group => group.Tools.Count > 0)
            .ToArray();

    private static IReadOnlyList<ToolGroup> CreateQuickToolGroups()
    {
        return
        [
            new("DRAW",
            [
                new("Line", "_.LINE", "Create line segments."),
                new("Polyline", "_.PLINE", "Create a 2D polyline."),
                new("Rectangle", "_.RECTANG", "Create a rectangular polyline."),
                new("Circle", "_.CIRCLE", "Create a circle.")
            ]),
            new("MODIFY",
            [
                new("Move", "_.MOVE", "Move selected objects."),
                new("Copy", "_.COPY", "Copy selected objects."),
                new("Offset", "_.OFFSET", "Create parallel or concentric copies."),
                new("Trim", "_.TRIM", "Trim objects to boundaries."),
                new("Extend", "_.EXTEND", "Extend objects to boundaries."),
                new("Fillet", "_.FILLET", "Round and join object edges."),
                new("Chamfer", "_.CHAMFER", "Bevel and join object edges."),
                new("Join", "_.JOIN", "Join compatible objects."),
                new("Rotate", "_.ROTATE", "Rotate selected objects."),
                new("Mirror", "_.MIRROR", "Create a mirrored copy."),
                new("Scale", "_.SCALE", "Scale selected objects."),
                new("Stretch", "_.STRETCH", "Stretch crossed geometry."),
                new("Align", "_.ALIGN", "Align objects in 2D or 3D."),
                new("Match Props", "_.MATCHPROP", "Copy properties between objects.")
            ]),
            new("ANNOTATION",
            [
                new("Hatch", "_.HATCH", "Create hatch or fill."),
                new("Text", "_.TEXT", "Create single-line text."),
                new("MText", "_.MTEXT", "Create multiline text."),
                new("Dimension", "_.DIM", "Create a context-aware dimension."),
                new("Linear Dim", "_.DIMLINEAR", "Create a linear dimension."),
                new("Aligned Dim", "_.DIMALIGNED", "Create an aligned dimension.")
            ]),
            new("LAYER",
            [
                new("Isolate", "_.LAYISO", "Isolate selected object layers."),
                new("Unisolate", "_.LAYUNISO", "Restore isolated layers."),
                new("Layer Off", "_.LAYOFF", "Turn off a selected object layer."),
                new("All Layers On", "_.LAYON", "Turn on all drawing layers.")
            ]),
            new("BLOCK",
            [
                new("Create Block", "_.BLOCK", "Create a block definition."),
                new("Insert", "_.INSERT", "Insert a block reference."),
                new("Block Editor", "_.BEDIT", "Open a block definition in Block Editor."),
                new("Explode", "_.EXPLODE", "Explode a compound object.")
            ]),
            new("UTILITY",
            [
                new("Distance", "_.DIST", "Measure distance and angle."),
                new("Area", "_.AREA", "Measure area and perimeter."),
                new("Purge", "_.PURGE", "Open AutoCAD Purge for unused definitions."),
                new("Zoom Extents", "_.ZOOM\n_Extents", "Zoom to all drawing extents.")
            ])
        ];
    }
}
