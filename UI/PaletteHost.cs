using System.Drawing;
using Autodesk.AutoCAD.Windows;

namespace CadProductivityPalette.UI;

public static class PaletteHost
{
    private static readonly Guid PaletteId = new("5A5E0375-B68D-4BAE-92FC-F5C96B1D2D6A");
    private static PaletteSet? _paletteSet;
    private static ProductivityPalette? _view;

    public static void Toggle()
    {
        EnsureCreated();
        if (_paletteSet is null)
        {
            return;
        }

        _paletteSet.Visible = !_paletteSet.Visible;
        if (_paletteSet.Visible)
        {
            _view?.Refresh();
        }
    }

    public static void Shutdown()
    {
        _view?.Dispose();
        _view = null;
        _paletteSet = null;
    }

    private static void EnsureCreated()
    {
        if (_paletteSet is not null)
        {
            return;
        }

        _view = new ProductivityPalette();
        _paletteSet = new PaletteSet("CAD Productivity Palette", PaletteId)
        {
            DockEnabled = DockSides.Left | DockSides.Right,
            MinimumSize = new Size(320, 420),
            Size = new Size(370, 760),
            KeepFocus = false,
            Style = PaletteSetStyles.ShowAutoHideButton
                    | PaletteSetStyles.ShowCloseButton
                    | PaletteSetStyles.ShowPropertiesMenu
        };
        _paletteSet.AddVisual("Productivity", _view);
    }
}
