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
    public void Execute(string commandText, IReadOnlyCollection<ObjectId>? preselection = null)
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null)
        {
            return;
        }

        try
        {
            ObjectId[] validIds = preselection?
                .Where(id => id.IsValid && !id.IsErased && id.Database == document.Database)
                .ToArray() ?? [];

            if (validIds.Length > 0)
            {
                document.Editor.SetImpliedSelection(validIds);
            }

            string command = commandText.EndsWith('\n') ? commandText : commandText + "\n";
            document.SendStringToExecute(command, true, false, false);
        }
        catch (System.Exception exception)
        {
            WriteError(document.Editor, "command", exception);
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
                foreach (ObjectId id in ids.Where(id => id.IsValid && !id.IsErased))
                {
                    if (transaction.GetObject(id, OpenMode.ForWrite, false) is Polyline polyline)
                    {
                        polyline.Closed = !polyline.Closed;
                    }
                }

                transaction.Commit();
            }

            document.Editor.Regen();
        }
        catch (System.Exception exception)
        {
            WriteError(document.Editor, "polyline update", exception);
        }
    }

    public void EditBlock(ObjectId id)
    {
        Document? document = AcApplication.DocumentManager.MdiActiveDocument;
        if (document is null || !id.IsValid || id.IsErased)
        {
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
            WriteError(document.Editor, "block edit", exception);
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
            .Where(id => id.IsValid && !id.IsErased && id.Database == document.Database)
            .ToArray();
        if (ids.Length == 0)
        {
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
            WriteError(document.Editor, "selection zoom", exception);
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
        editor.WriteMessage($"\n[CAD Productivity Palette] Unable to run {operation}: {exception.Message}");
    }
}
