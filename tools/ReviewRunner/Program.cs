using System.Text.Json;
using CadProductivityPalette.Core;
using CadProductivityPalette.Services;

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
if (args.Length == 1 && args[0] == "--self-test")
{
    int checks = 0;
    void Check(bool condition, string name)
    {
        checks++;
        if (!condition)
            throw new InvalidOperationException("FAIL: " + name);
        Console.WriteLine("PASS " + name);
    }
    var clean = DrawingReviewEngine.Analyze(
        new(
            "clean.dwg",
            4,
            [
                new("1", "LWPOLYLINE", "A-WALL", true, true),
                new("2", "TEXT", "A-TEXT", true, null, "도면명"),
            ]
        )
    );
    Check(clean.CandidateCount == 0, "clean drawing has no review candidates");
    var report = DrawingReviewEngine.Analyze(
        new(
            "demo.dwg",
            0,
            [
                new("A", "LWPOLYLINE", "0", false, false),
                new("B", "TEXT", "A-TEXT", true, null, " "),
                new("C", "MTEXT", "A-TEXT", true, null, "note"),
            ]
        )
    );
    Check(
        report.Findings.Single(f => f.RuleId == "OPEN_POLYLINE").Handles.SequenceEqual(["A"]),
        "open polyline handles"
    );
    Check(
        report.Findings.Single(f => f.RuleId == "DIRECT_COLOR").Count == 1,
        "direct color candidate"
    );
    Check(report.Findings.Single(f => f.RuleId == "EMPTY_TEXT").Count == 1, "empty text candidate");
    Check(
        report.Findings.Single(f => f.RuleId == "LAYER_ZERO").Severity == "참고",
        "layer zero is informational"
    );
    Check(report.Findings.Any(f => f.RuleId == "UNSPECIFIED_UNITS"), "undefined units detected");
    Check(report.CandidateCount == 4, "candidate count includes drawing-level unit issue");
    bool rejected = false;
    try
    {
        DrawingReviewEngine.Analyze(
            new("bad", 4, [new("A", "LINE", "0", true), new("a", "LINE", "0", true)])
        );
    }
    catch (ArgumentException)
    {
        rejected = true;
    }
    Check(rejected, "duplicate handles rejected");
    Check(DrawingReviewWriter.CsvCell("a,\"b\"") == "\"a,\"\"b\"\"\"", "CSV escaping");
    Check(
        DrawingReviewWriter.CsvCell("=1+2").StartsWith("\"'="),
        "CSV spreadsheet formula neutralized"
    );
    Check(ToolCatalog.AllTools.Count == 36, "36 commands retained");
    Check(
        ToolCatalog.AllTools.Select(t => t.CommandText).Distinct().Count() == 36,
        "commands are unique"
    );
    Check(
        ToolCatalog.FindByGlobalCommandName("_.line")?.Name == "Line",
        "language-independent command normalization"
    );
    Check(ToolCatalog.EssentialTools.Count == 8, "eight essential tools");
    string folder = Path.Combine(
        Path.GetTempPath(),
        "cad-portfolio-" + Guid.NewGuid().ToString("N")
    );
    Directory.CreateDirectory(folder);
    try
    {
        DrawingReviewWriter.Write(report, Path.Combine(folder, "report.csv"));
        DrawingReviewWriter.Write(report, Path.Combine(folder, "report.json"));
        Check(
            File.ReadAllText(Path.Combine(folder, "report.csv")).Contains("UNSPECIFIED_UNITS"),
            "CSV contains rule evidence"
        );
        Check(
            JsonSerializer
                .Deserialize<ReviewReport>(
                    File.ReadAllText(Path.Combine(folder, "report.json")),
                    options
                )
                ?.EntityCount == 3,
            "JSON report round trip"
        );
        var usage = new ToolUsageService(Path.Combine(folder, "usage.json"));
        var tool = ToolCatalog.AllTools[0];
        usage.Record(tool);
        usage.Record(tool);
        Check(usage.GetFrequent(ToolCatalog.AllTools).Count == 1, "tool frequency threshold");
        Check(
            new ToolUsageService(Path.Combine(folder, "usage.json"))
                .GetRecent(ToolCatalog.AllTools)
                .Count == 1,
            "history persists"
        );
        usage.Clear();
        Check(!usage.HasUsage, "history clears");
        File.WriteAllText(Path.Combine(folder, "usage.json"), "broken-json");
        Check(
            !new ToolUsageService(Path.Combine(folder, "usage.json")).HasUsage,
            "corrupt history falls back safely"
        );
        var settings = new UserSettingsService(Path.Combine(folder, "settings.json"));
        Check(settings.IsSimpleMode, "simple mode default");
        settings.SetSimpleMode(false);
        Check(
            !new UserSettingsService(Path.Combine(folder, "settings.json")).IsSimpleMode,
            "mode persists"
        );
    }
    finally
    {
        foreach (string file in Directory.GetFiles(folder))
            File.Delete(file);
        Directory.Delete(folder);
    }
    Console.WriteLine($"Passed {checks} checks.");
    return;
}
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: ReviewRunner <snapshot.json> <output-prefix> | --self-test");
    Environment.ExitCode = 2;
    return;
}
try
{
    var snapshot =
        JsonSerializer.Deserialize<ReviewSnapshot>(File.ReadAllText(args[0]), options)
        ?? throw new ArgumentException("Snapshot is null.");
    var report = DrawingReviewEngine.Analyze(snapshot);
    DrawingReviewWriter.Write(report, args[1] + ".csv");
    DrawingReviewWriter.Write(report, args[1] + ".json");
    Console.WriteLine(
        $"{report.DrawingName}: {report.EntityCount} entities, {report.CandidateCount} review candidates."
    );
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    Environment.ExitCode = 1;
}
