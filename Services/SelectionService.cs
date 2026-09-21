using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using CadProductivityPalette.Models;

namespace CadProductivityPalette.Services;

public sealed class SelectionService
{
    public SelectionInfo Read(Document? document)
    {
        if (document is null)
        {
            return SelectionInfo.Empty;
        }

        try
        {
            PromptSelectionResult result = document.Editor.SelectImplied();
            if (result.Status != PromptStatus.OK || result.Value.Count == 0)
            {
                return SelectionInfo.Empty;
            }

            ObjectId[] ids = result.Value.GetObjectIds()
                .Where(id => id.IsValid && !id.IsErased)
                .ToArray();
            if (ids.Length == 0)
            {
                return SelectionInfo.Empty;
            }

            using Transaction transaction = document.Database.TransactionManager.StartOpenCloseTransaction();
            List<Entity> entities = ids
                .Select(id => transaction.GetObject(id, OpenMode.ForRead, false) as Entity)
                .Where(entity => entity is not null)
                .Cast<Entity>()
                .ToList();

            if (entities.Count == 0)
            {
                return SelectionInfo.Empty;
            }

            if (entities.Count > 1)
            {
                string commonLayer = entities.Select(entity => entity.Layer).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1
                    ? entities[0].Layer
                    : "Varies";
                string commonType = entities.Select(GetTypeName).Distinct(StringComparer.Ordinal).Count() == 1
                    ? GetTypeName(entities[0])
                    : "Mixed objects";

                return new SelectionInfo
                {
                    Heading = $"{entities.Count} Objects",
                    Description = commonType,
                    Kind = SelectionKind.Multiple,
                    ObjectIds = ids,
                    Properties =
                    [
                        new PropertyItem("Layer", commonLayer)
                    ]
                };
            }

            Entity entity = entities[0];
            List<PropertyItem> properties =
            [
                new("Layer", entity.Layer),
                new("Color", GetColor(entity))
            ];

            SelectionKind kind = AddSpecificProperties(entity, transaction, properties);
            return new SelectionInfo
            {
                Heading = GetTypeName(entity),
                Description = "이동·복사 등은 아래 도구에서 실행하세요.",
                Kind = kind,
                ObjectIds = ids,
                Properties = properties
            };
        }
        catch (System.Exception exception)
        {
            document.Editor.WriteMessage($"\n[CAD Productivity Palette] Selection read failed: {exception.Message}");
            return SelectionInfo.Empty;
        }
    }

    private static SelectionKind AddSpecificProperties(
        Entity entity,
        Transaction transaction,
        ICollection<PropertyItem> properties)
    {
        switch (entity)
        {
            case Polyline polyline:
                properties.Add(new("Length", Format(polyline.Length)));
                properties.Add(new("Area", TryArea(polyline)));
                properties.Add(new("State", polyline.Closed ? "Closed" : "Open"));
                return SelectionKind.Polyline;

            case Line line:
                properties.Add(new("Length", Format(line.Length)));
                return SelectionKind.Line;

            case DBText text:
                properties.Add(new("Content", Collapse(text.TextString)));
                properties.Add(new("Height", Format(text.Height)));
                properties.Add(new("Text Style", GetSymbolName(text.TextStyleId, transaction)));
                return SelectionKind.Text;

            case MText mtext:
                properties.Add(new("Content", Collapse(mtext.Text)));
                properties.Add(new("Height", Format(mtext.TextHeight)));
                properties.Add(new("Text Style", GetSymbolName(mtext.TextStyleId, transaction)));
                return SelectionKind.Text;

            case BlockReference block:
                ObjectId definitionId = block.IsDynamicBlock ? block.DynamicBlockTableRecord : block.BlockTableRecord;
                properties.Add(new("Block Name", GetSymbolName(definitionId, transaction)));
                properties.Add(new("Attributes", block.AttributeCollection.Count > 0 ? "Yes" : "No"));
                return SelectionKind.Block;

            case Hatch hatch:
                properties.Add(new("Pattern", hatch.PatternName));
                properties.Add(new("Scale", Format(hatch.PatternScale)));
                return SelectionKind.Hatch;

            case Dimension dimension:
                properties.Add(new("Dim Style", GetSymbolName(dimension.DimensionStyle, transaction)));
                properties.Add(new("Measurement", Format(dimension.Measurement)));
                return SelectionKind.Dimension;

            case Circle circle:
                properties.Add(new("Radius", Format(circle.Radius)));
                properties.Add(new("Diameter", Format(circle.Radius * 2.0)));
                properties.Add(new("Area", Format(Math.PI * circle.Radius * circle.Radius)));
                return SelectionKind.Circle;

            case Arc arc:
                properties.Add(new("Radius", Format(arc.Radius)));
                properties.Add(new("Length", Format(arc.GetDistanceAtParameter(arc.EndParam))));
                return SelectionKind.Arc;

            default:
                return SelectionKind.Other;
        }
    }

    private static string GetTypeName(Entity entity) => entity switch
    {
        Polyline => "Polyline",
        Line => "Line",
        DBText => "Text",
        MText => "MText",
        BlockReference => "Block Reference",
        Hatch => "Hatch",
        Dimension => "Dimension",
        Circle => "Circle",
        Arc => "Arc",
        _ => entity.GetType().Name
    };

    private static string GetColor(Entity entity)
    {
        if (entity.Color.IsByLayer)
        {
            return "ByLayer";
        }

        if (entity.Color.IsByBlock)
        {
            return "ByBlock";
        }

        return $"ACI {entity.ColorIndex}";
    }

    private static string GetSymbolName(ObjectId id, Transaction transaction)
    {
        return transaction.GetObject(id, OpenMode.ForRead, false) is SymbolTableRecord record
            ? record.Name
            : "—";
    }

    private static string TryArea(Polyline polyline)
    {
        try
        {
            return Format(polyline.Area);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            return "—";
        }
    }

    private static string Collapse(string? value)
    {
        string collapsed = string.Join(" ", (value ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Length switch
        {
            0 => "(empty)",
            > 64 => collapsed[..61] + "...",
            _ => collapsed
        };
    }

    private static string Format(double value) => value.ToString("0.###");
}
