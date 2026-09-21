using Autodesk.AutoCAD.Runtime;
using CadProductivityPalette.UI;

namespace CadProductivityPalette;

public sealed class PluginApplication : IExtensionApplication
{
    public void Initialize()
    {
        // The palette is created lazily by CADTOOLS so NETLOAD stays lightweight.
    }

    public void Terminate()
    {
        PaletteHost.Shutdown();
    }
}
