using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadProductivityPalette.Models;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;

namespace CadProductivityPalette.Services;

public sealed class AutoCadCommandService
{
    public bool Execute(string commandText, IReadOnlyCollection<ObjectId>? preselection = null)
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
        {
            return false;
        }

        try
        {
            ObjectId[] validIds = preselection?
                .Where(id => IsUsableId(id, document.Database))
                .ToArray() ?? [];

            if (preselection is not null && validIds.Length == 0)
            {
                WriteNotice(document.Editor, "선택 객체가 현재 도면에 없습니다. 객체를 다시 선택해 주세요.");
                return false;
            }

            if (validIds.Length > 0)
            {
                document.Editor.SetImpliedSelection(validIds);
            }

            string command = commandText.EndsWith('\n') ? commandText : commandText + "\n";
            document.SendStringToExecute(command, true, false, false);
            return true;
        }
        catch (System.Exception exception)
        {
            WriteError(document.Editor, "명령 실행", exception);
            return false;
        }
    }

    public void TogglePolylineClosed(IReadOnlyCollection<ObjectId> ids)
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
        {
            return;
        }

        try
        {
            using (document.LockDocument())
            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                int updatedCount = 0;
                foreach (ObjectId id in ids.Where(id => IsUsableId(id, document.Database)))
                {
                    if (transaction.GetObject(id, OpenMode.ForWrite, false) is Polyline polyline)
                    {
                        polyline.Closed = !polyline.Closed;
                        updatedCount++;
                    }
                }

                if (updatedCount == 0)
                {
                    WriteNotice(document.Editor, "현재 도면에서 변경할 폴리라인을 찾지 못했습니다.");
                    return;
                }

                transaction.Commit();
            }

            document.Editor.Regen();
        }
        catch (System.Exception exception)
        {
            WriteError(document.Editor, "폴리라인 변경", exception);
        }
    }

    public void EditBlock(ObjectId id)
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
        {
            return;
        }

        if (!IsUsableId(id, document.Database))
        {
            WriteNotice(document.Editor, "블록 선택이 현재 도면과 일치하지 않습니다. 블록을 다시 선택해 주세요.");
            return;
        }

        try
        {
            string? blockName = null;
            using (Transaction transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
            {
                if (transaction.GetObject(id, OpenMode.ForRead, false) is BlockReference reference)
                {
                    ObjectId definitionId = reference.IsDynamicBlock
                        ? reference.DynamicBlockTableRecord
                        : reference.BlockTableRecord;
                    blockName = ((BlockTableRecord)transaction.GetObject(definitionId, OpenMode.ForRead)).Name;
                }
            }

            if (!string.IsNullOrWhiteSpace(blockName))
            {
                string escapedName = blockName.Replace("\"", "\"\"");
                Execute($"_.BEDIT\n\"{escapedName}\"");
            }
        }
        catch (System.Exception exception)
        {
            WriteError(document.Editor, "블록 편집", exception);
        }
    }

    public void SelectAndZoom(DrawingIssue issue)
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
        {
            return;
        }

        ObjectId[] ids = issue.ObjectIds
            .Where(id => IsUsableId(id, document.Database))
            .ToArray();
        if (ids.Length == 0)
        {
            WriteNotice(document.Editor, "선택할 객체가 현재 도면에 없습니다. 도면 점검을 다시 실행해 주세요.");
            return;
        }

        try
        {
            document.Editor.SetImpliedSelection(ids);
            Extents3d? extents = GetExtents(document.Database, ids);
            if (extents is not null)
            {
                ZoomTo(document.Editor, extents.Value, 1.15);
            }
        }
        catch (System.Exception exception)
        {
            WriteError(document.Editor, "선택 객체 줌", exception);
        }
    }

    private static Extents3d? GetExtents(Database database, IEnumerable<ObjectId> ids)
    {
        Extents3d? combined = null;
        using Transaction transaction = database.TransactionManager.StartOpenCloseTransaction();
        foreach (ObjectId id in ids)
        {
            if (transaction.GetObject(id, OpenMode.ForRead, false) is not Entity entity)
            {
                continue;
            }

            try
            {
                Extents3d entityExtents = entity.GeometricExtents;
                if (combined is null)
                {
                    combined = entityExtents;
                }
                else
                {
                    Extents3d value = combined.Value;
                    value.AddExtents(entityExtents);
                    combined = value;
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                // Some proxy or non-graphical entities do not expose extents.
            }
        }

        return combined;
    }

    private static void ZoomTo(Editor editor, Extents3d worldExtents, double margin)
    {
        using ViewTableRecord view = editor.GetCurrentView();

        Matrix3d worldToDisplay = Matrix3d.PlaneToWorld(view.ViewDirection);
        worldToDisplay = Matrix3d.Displacement(view.Target - Point3d.Origin) * worldToDisplay;
        worldToDisplay = Matrix3d.Rotation(-view.ViewTwist, view.ViewDirection, view.Target) * worldToDisplay;
        worldToDisplay = worldToDisplay.Inverse();

        Extents3d displayExtents = worldExtents;
        displayExtents.TransformBy(worldToDisplay);

        double width = Math.Max(displayExtents.MaxPoint.X - displayExtents.MinPoint.X, 1e-6);
        double height = Math.Max(displayExtents.MaxPoint.Y - displayExtents.MinPoint.Y, 1e-6);
        double viewRatio = view.Width / view.Height;
        double extentRatio = width / height;

        if (extentRatio > viewRatio)
        {
            height = width / viewRatio;
        }
        else
        {
            width = height * viewRatio;
        }

        view.Width = width * margin;
        view.Height = height * margin;
        view.CenterPoint = new Point2d(
            (displayExtents.MinPoint.X + displayExtents.MaxPoint.X) / 2.0,
            (displayExtents.MinPoint.Y + displayExtents.MaxPoint.Y) / 2.0);
        editor.SetCurrentView(view);
    }

    private static void WriteError(Editor editor, string operation, System.Exception exception)
    {
        editor.WriteMessage($"\n[CAD Productivity Palette] {operation} 처리 실패: {exception.Message}");
    }

    private static void WriteNotice(Editor editor, string message)
    {
        editor.WriteMessage($"\n[CAD Productivity Palette] {message}");
    }

    private static bool IsUsableId(ObjectId id, Database database)
    {
        return !id.IsNull && id.IsValid && !id.IsErased && id.Database == database;
    }
}
