using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CadProductivityPalette.Core;

public sealed record ReviewEntity(
    string Handle,
    string Type,
    string Layer,
    bool ByLayer,
    bool? Closed = null,
    string? Text = null
);

public sealed record ReviewSnapshot(
    string DrawingName,
    int Units,
    IReadOnlyList<ReviewEntity> Entities
);

public sealed record ReviewFinding(
    string RuleId,
    string Name,
    string Severity,
    string Description,
    IReadOnlyList<string> Handles
)
{
    public int Count => Handles.Count;
}

public sealed record ReviewReport(
    string DrawingName,
    int Units,
    int EntityCount,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReviewFinding> Findings
)
{
    public int CandidateCount => Findings.Where(f => f.Severity == "검토").Sum(f => f.Count);
}

public static class DrawingReviewEngine
{
    public static ReviewReport Analyze(ReviewSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Entities.Any(e => string.IsNullOrWhiteSpace(e.Handle)))
            throw new ArgumentException("객체 Handle이 비어 있습니다.");
        if (
            snapshot
                .Entities.Select(e => e.Handle)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != snapshot.Entities.Count
        )
            throw new ArgumentException("중복 Handle이 있습니다.");
        ReviewFinding Match(
            string id,
            string name,
            string severity,
            string description,
            Func<ReviewEntity, bool> predicate
        ) =>
            new(
                id,
                name,
                severity,
                description,
                snapshot.Entities.Where(predicate).Select(e => e.Handle).ToArray()
            );
        List<ReviewFinding> findings =
        [
            Match(
                "OPEN_POLYLINE",
                "열린 Polyline",
                "검토",
                "열린 2D Polyline입니다. 의도한 객체인지 확인하세요.",
                e =>
                    e.Type.Equals("LWPOLYLINE", StringComparison.OrdinalIgnoreCase)
                    && e.Closed == false
            ),
            Match(
                "DIRECT_COLOR",
                "ByLayer가 아닌 색상",
                "검토",
                "객체별 색상 또는 ByBlock 설정입니다. 자동 수정하지 않습니다.",
                e => !e.ByLayer
            ),
            Match(
                "EMPTY_TEXT",
                "빈 Text",
                "검토",
                "공백 또는 빈 문자열인 Text / MText 후보입니다.",
                e =>
                    (
                        e.Type.Equals("TEXT", StringComparison.OrdinalIgnoreCase)
                        || e.Type.Equals("MTEXT", StringComparison.OrdinalIgnoreCase)
                    ) && string.IsNullOrWhiteSpace(e.Text)
            ),
            Match(
                "LAYER_ZERO",
                "Layer 0 사용",
                "참고",
                "Layer 0 사용은 정상일 수 있습니다. 프로젝트 레이어 정책에 따라 판단하세요.",
                e => e.Layer == "0"
            ),
        ];
        if (snapshot.Units == 0)
            findings.Add(
                new(
                    "UNSPECIFIED_UNITS",
                    "도면 단위 미지정",
                    "검토",
                    "INSUNITS=0입니다. 제출 도면의 단위와 축척을 확인하세요.",
                    ["DRAWING"]
                )
            );
        return new(
            snapshot.DrawingName,
            snapshot.Units,
            snapshot.Entities.Count,
            DateTimeOffset.UtcNow,
            findings
        );
    }
}

public static class DrawingReviewWriter
{
    public static string CsvCell(string? value)
    {
        string text = value ?? string.Empty;
        if (
            text.TrimStart().StartsWith('=')
            || text.TrimStart().StartsWith('+')
            || text.TrimStart().StartsWith('-')
            || text.TrimStart().StartsWith('@')
        )
            text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    public static void Write(ReviewReport report, string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        string text;
        if (extension == ".json")
            text = JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                }
            );
        else if (extension == ".csv")
        {
            StringBuilder csv = new("도면명,도면단위,객체수,규칙,항목,구분,후보수,Handle,설명\r\n");
            foreach (var f in report.Findings)
                csv.AppendJoin(
                        ',',
                        new[]
                        {
                            report.DrawingName,
                            report.Units.ToString(CultureInfo.InvariantCulture),
                            report.EntityCount.ToString(CultureInfo.InvariantCulture),
                            f.RuleId,
                            f.Name,
                            f.Severity,
                            f.Count.ToString(CultureInfo.InvariantCulture),
                            string.Join(';', f.Handles),
                            f.Description,
                        }.Select(CsvCell)
                    )
                    .Append("\r\n");
            text = csv.ToString();
        }
        else
            throw new ArgumentException("CSV 또는 JSON 파일을 지정하세요.", nameof(path));
        string full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        string temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, text, new UTF8Encoding(extension == ".csv"));
            File.Move(temporary, full, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
