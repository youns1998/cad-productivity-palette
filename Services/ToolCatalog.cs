using CadProductivityPalette.Models;

namespace CadProductivityPalette.Services;

public static class ToolCatalog
{
    private static readonly HashSet<string> EssentialCommands =
        ["_.LINE", "_.PLINE", "_.MOVE", "_.COPY", "_.OFFSET", "_.TRIM", "_.MTEXT", "_.DIM"];

    public static IReadOnlyList<ToolGroup> Groups { get; } = CreateQuickToolGroups();

    public static IReadOnlyList<ToolAction> AllTools { get; } =
        Groups.SelectMany(group => group.Tools).ToArray();

    private static readonly IReadOnlyDictionary<string, ToolAction> ToolsByGlobalCommandName =
        AllTools.ToDictionary(
            tool => NormalizeCommandName(tool.CommandText),
            StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<ToolAction> EssentialTools { get; } =
        AllTools
            .Where(tool => EssentialCommands.Contains(tool.CommandText))
            .ToArray();

    public static ToolAction? FindByGlobalCommandName(string? commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return null;
        }

        return ToolsByGlobalCommandName.TryGetValue(NormalizeCommandName(commandName), out ToolAction? tool)
            ? tool
            : null;
    }

    private static string NormalizeCommandName(string commandText)
    {
        string firstToken = commandText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
        return firstToken.TrimStart('.', '_', '\'', '-').ToUpperInvariant();
    }

    private static IReadOnlyList<ToolGroup> CreateQuickToolGroups()
    {
        return
        [
            new("그리기",
            [
                new("Line", "_.LINE", "선분을 작성합니다."),
                new("Polyline", "_.PLINE", "2D Polyline을 작성합니다."),
                new("Rectangle", "_.RECTANG", "직사각형 Polyline을 작성합니다."),
                new("Circle", "_.CIRCLE", "원을 작성합니다.")
            ]),
            new("수정",
            [
                new("Move", "_.MOVE", "선택한 객체를 이동합니다."),
                new("Copy", "_.COPY", "선택한 객체를 복사합니다."),
                new("Offset", "_.OFFSET", "평행하거나 동심인 객체를 작성합니다."),
                new("Trim", "_.TRIM", "경계를 기준으로 객체를 자릅니다."),
                new("Extend", "_.EXTEND", "경계까지 객체를 연장합니다."),
                new("Fillet", "_.FILLET", "객체 모서리를 둥글게 연결합니다."),
                new("Chamfer", "_.CHAMFER", "객체 모서리를 모따기하여 연결합니다."),
                new("Join", "_.JOIN", "연결 가능한 객체를 하나로 결합합니다."),
                new("Rotate", "_.ROTATE", "선택한 객체를 회전합니다."),
                new("Mirror", "_.MIRROR", "대칭 복사본을 작성합니다."),
                new("Scale", "_.SCALE", "선택한 객체의 크기를 변경합니다."),
                new("Stretch", "_.STRETCH", "교차 선택한 형상을 늘이거나 줄입니다."),
                new("Align", "_.ALIGN", "객체를 2D 또는 3D에서 정렬합니다."),
                new("Match Props", "_.MATCHPROP", "다른 객체의 속성을 복사해 적용합니다.")
            ]),
            new("주석",
            [
                new("Hatch", "_.HATCH", "Hatch 또는 채우기를 작성합니다."),
                new("Text", "_.TEXT", "한 줄 Text를 작성합니다."),
                new("MText", "_.MTEXT", "여러 줄 Text를 작성합니다."),
                new("Dimension", "_.DIM", "객체에 맞는 치수를 작성합니다."),
                new("Linear Dim", "_.DIMLINEAR", "수평 또는 수직 치수를 작성합니다."),
                new("Aligned Dim", "_.DIMALIGNED", "객체에 정렬된 치수를 작성합니다.")
            ]),
            new("Layer",
            [
                new("Layer Isolate", "_.LAYISO", "선택한 객체의 Layer만 표시합니다."),
                new("Layer 복원", "_.LAYUNISO", "분리했던 Layer 표시 상태를 복원합니다."),
                new("Layer 끄기", "_.LAYOFF", "선택한 객체의 Layer를 끕니다."),
                new("모든 Layer 켜기", "_.LAYON", "도면의 모든 Layer를 켭니다.")
            ]),
            new("Block",
            [
                new("Block 생성", "_.BLOCK", "새 Block 정의를 작성합니다."),
                new("Insert", "_.INSERT", "Block 참조를 삽입합니다."),
                new("Block 편집", "_.BEDIT", "Block Editor에서 Block 정의를 편집합니다."),
                new("Explode", "_.EXPLODE", "결합된 객체를 구성 요소로 분해합니다.")
            ]),
            new("측정 · 정리",
            [
                new("Distance", "_.DIST", "거리와 각도를 측정합니다."),
                new("Area", "_.AREA", "면적과 둘레를 측정합니다."),
                new("Purge", "_.PURGE", "미사용 정의를 확인하는 Purge 창을 엽니다."),
                new("Zoom Extents", "_.ZOOM\n_Extents", "도면 전체 범위로 Zoom합니다.")
            ])
        ];
    }
}
