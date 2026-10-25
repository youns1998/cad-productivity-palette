using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CadProductivityPalette.Core;
using CadProductivityPalette.Models;

namespace CadProductivityPalette.Services;

public sealed class DrawingStatusService
{
    public DrawingStatus Analyze(Document? document)
    {
        if (document is null)
            return DrawingStatus.Empty;
        try
        {
            Database db = document.Database;
            using Transaction tx = db.TransactionManager.StartOpenCloseTransaction();
            BlockTable blocks = (BlockTable)tx.GetObject(db.BlockTableId, OpenMode.ForRead);
            BlockTableRecord model = (BlockTableRecord)
                tx.GetObject(blocks[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            List<ReviewEntity> entities = [];
            Dictionary<string, ObjectId> ids = new(StringComparer.OrdinalIgnoreCase);
            foreach (ObjectId id in model)
            {
                if (
                    !id.IsValid
                    || id.IsErased
                    || tx.GetObject(id, OpenMode.ForRead, false) is not Entity e
                )
                    continue;
                string handle = e.Handle.ToString();
                ids[handle] = id;
                string type =
                    e is Polyline ? "LWPOLYLINE"
                    : e is DBText ? "TEXT"
                    : e is MText ? "MTEXT"
                    : e.GetType().Name;
                string? text =
                    e is DBText t ? t.TextString
                    : e is MText m ? m.Text
                    : null;
                entities.Add(
                    new(
                        handle,
                        type,
                        e.Layer,
                        e.Color.IsByLayer,
                        e is Polyline p ? p.Closed : null,
                        text
                    )
                );
            }
            ReviewReport report = DrawingReviewEngine.Analyze(
                new(System.IO.Path.GetFileName(document.Name), (int)db.Insunits, entities)
            );
            List<DrawingIssue> issues = report
                .Findings.Select(f => new DrawingIssue(
                    f.Name,
                    f.Count,
                    f.Description,
                    f.Count == 0 ? IssueSeverity.Normal
                        : f.Severity == "검토" ? IssueSeverity.Warning
                        : IssueSeverity.Information,
                    f.Handles.Where(ids.ContainsKey).Select(h => ids[h]).ToArray()
                ))
                .ToList();
            LayerTable layers = (LayerTable)tx.GetObject(db.LayerTableId, OpenMode.ForRead);
            int locked = layers
                .Cast<ObjectId>()
                .Count(id => ((LayerTableRecord)tx.GetObject(id, OpenMode.ForRead)).IsLocked);
            int unused = blocks
                .Cast<ObjectId>()
                .Select(id => (BlockTableRecord)tx.GetObject(id, OpenMode.ForRead))
                .Count(b =>
                    !b.IsLayout
                    && !b.IsAnonymous
                    && !b.IsFromExternalReference
                    && !b.IsDependent
                    && b.GetBlockReferenceIds(true, true).Count == 0
                );
            issues.Add(
                new(
                    "잠긴 Layer",
                    locked,
                    "정상적인 보호 설정일 수 있습니다.",
                    IssueSeverity.Information,
                    []
                )
            );
            issues.Add(
                new(
                    "미사용 Block",
                    unused,
                    "제거 여부는 Purge에서 직접 확인하세요.",
                    IssueSeverity.Information,
                    []
                )
            );
            string Symbol(ObjectId id) =>
                tx.GetObject(id, OpenMode.ForRead, false) is SymbolTableRecord s ? s.Name : "—";
            return new DrawingStatus
            {
                CurrentLayer = Symbol(db.Clayer),
                CurrentTextStyle = Symbol(db.Textstyle),
                CurrentDimStyle = Symbol(db.Dimstyle),
                ObjectCount = entities.Count,
                Issues = issues,
                RefreshedAt = DateTime.Now,
                Report = report,
            };
        }
        catch (System.Exception error)
        {
            document.Editor.WriteMessage($"\n[CAD 생산성 도구] 도면 점검 실패: {error.Message}");
            return DrawingStatus.Empty;
        }
    }
}
