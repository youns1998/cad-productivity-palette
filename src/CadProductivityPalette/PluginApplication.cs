using Autodesk.AutoCAD.Runtime;
using CadProductivityPalette.Services;
using CadProductivityPalette.UI;

namespace CadProductivityPalette;

public sealed class PluginApplication : IExtensionApplication
{
    public void Initialize()
    {
        ToolUsageTracker.Instance.Start();
    }

    public void Terminate()
    {
        PaletteHost.Shutdown();
        ToolUsageTracker.Instance.Stop();
    }
}
