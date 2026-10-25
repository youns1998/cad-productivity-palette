using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CadProductivityPalette.Core;

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
