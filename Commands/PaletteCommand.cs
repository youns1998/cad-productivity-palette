using Autodesk.AutoCAD.Runtime;
using CadProductivityPalette.UI;

namespace CadProductivityPalette.Commands;

public sealed class PaletteCommand
{
    [CommandMethod("CADTOOLS", CommandFlags.Modal)]
    public void TogglePalette()
    {
        PaletteHost.Toggle();
    }
}
