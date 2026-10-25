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
