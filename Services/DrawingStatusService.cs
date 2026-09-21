using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CadProductivityPalette.Models;

namespace CadProductivityPalette.Services;

public sealed class DrawingStatusService
{
    public DrawingStatus Analyze(Document? document)
    {
        if (document is null)
        {
            return DrawingStatus.Empty;
        }

        try
        {
            Database database = document.Database;
            using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();

            List<ObjectId> modelObjects = [];
            List<ObjectId> openPolylines = [];
            List<ObjectId> nonByLayer = [];
            List<ObjectId> emptyText = [];
            List<ObjectId> layerZeroObjects = [];

            BlockTable blockTable = (BlockTable)transaction.GetObject(database.BlockTableId, OpenMode.ForRead);
            BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace],
                OpenMode.ForRead);

            foreach (ObjectId id in modelSpace)
            {
                if (!id.IsValid || id.IsErased || transaction.GetObject(id, OpenMode.ForRead, false) is not Entity entity)
                {
                    continue;
                }

                modelObjects.Add(id);

                if (!entity.Color.IsByLayer)
                {
                    nonByLayer.Add(id);
                }

                if (string.Equals(entity.Layer, "0", StringComparison.OrdinalIgnoreCase))
                {
                    layerZeroObjects.Add(id);
                }

                if (entity is Polyline { Closed: false })
                {
                    openPolylines.Add(id);
                }

                switch (entity)
                {
                    case DBText text:
                        if (string.IsNullOrWhiteSpace(text.TextString))
                        {
                            emptyText.Add(id);
                        }
                        break;
                    case MText mtext:
                        if (string.IsNullOrWhiteSpace(mtext.Text))
                        {
                            emptyText.Add(id);
                        }
                        break;
                }
            }

            int lockedLayers = CountLockedLayers(database, transaction);
            int unusedBlocks = CountUnusedBlocks(blockTable, transaction);

            List<DrawingIssue> issues =
            [
                MakeIssue("열린 Polyline", openPolylines, "Model Space의 열린 2D Polyline입니다. 닫아야 하는 객체인지 검토하세요."),
                MakeIssue("ByLayer가 아닌 색상", nonByLayer, "직접 지정한 색상 또는 ByBlock 색상입니다. 의도한 설정일 수 있습니다."),
                MakeIssue("빈 Text", emptyText, "내용이 비어 있는 Text / MText 후보입니다."),
                new DrawingIssue("Layer 0 사용", layerZeroObjects.Count,
                    "Model Space의 Layer 0 객체입니다. Layer 사용 정책에 따라 검토하세요.",
                    IssueSeverity.Information, [.. layerZeroObjects]),
                MakeCountIssue("미사용 Block", unusedBlocks, "참조되지 않는 일반 Block 정의 수입니다. 제거 여부는 Purge에서 확인하세요."),
                MakeCountIssue("잠긴 Layer", lockedLayers, "현재 잠겨 있는 Layer 수입니다. 정상적인 보호 설정일 수 있습니다.")
            ];

            return new DrawingStatus
            {
                CurrentLayer = GetSymbolName(database.Clayer, transaction),
                CurrentTextStyle = GetSymbolName(database.Textstyle, transaction),
                CurrentDimStyle = GetSymbolName(database.Dimstyle, transaction),
                ObjectCount = modelObjects.Count,
                Issues = issues,
                RefreshedAt = DateTime.Now
            };
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage($"\n[CAD 생산성 도구] 도면 점검 실패: {exception.Message}");
            return DrawingStatus.Empty;
        }
    }


    private static int CountLockedLayers(Database database, Transaction transaction)
    {
        LayerTable table = (LayerTable)transaction.GetObject(database.LayerTableId, OpenMode.ForRead);
        return table.Cast<ObjectId>()
            .Select(id => (LayerTableRecord)transaction.GetObject(id, OpenMode.ForRead))
            .Count(record => record.IsLocked);
    }

    private static int CountUnusedBlocks(BlockTable table, Transaction transaction)
    {
        int count = 0;
        foreach (ObjectId id in table)
        {
            BlockTableRecord record = (BlockTableRecord)transaction.GetObject(id, OpenMode.ForRead);
            if (!record.IsLayout
                && !record.IsAnonymous
                && !record.IsFromExternalReference
                && !record.IsDependent
                && record.GetBlockReferenceIds(true, true).Count == 0)
            {
                count++;
            }
        }

        return count;
    }


    private static DrawingIssue MakeIssue(string name, List<ObjectId> ids, string description)
    {
        return new DrawingIssue(
            name,
            ids.Count,
            description,
            ids.Count == 0 ? IssueSeverity.Normal : IssueSeverity.Warning,
            [.. ids]);
    }

    private static DrawingIssue MakeCountIssue(string name, int count, string description)
    {
        return new DrawingIssue(
            name,
            count,
            description,
            IssueSeverity.Information,
            []);
    }

    private static string GetSymbolName(ObjectId id, Transaction transaction)
    {
        return transaction.GetObject(id, OpenMode.ForRead, false) is SymbolTableRecord record
            ? record.Name
            : "—";
    }
}
