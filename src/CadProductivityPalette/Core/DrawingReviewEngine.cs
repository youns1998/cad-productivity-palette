namespace CadProductivityPalette.Core;

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
