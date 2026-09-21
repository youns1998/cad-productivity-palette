using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CadProductivityPalette.PluginApplication))]
[assembly: CommandClass(typeof(CadProductivityPalette.Commands.PaletteCommand))]
