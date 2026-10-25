using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(CadProductivityPalette.PluginApplication))]
[assembly: CommandClass(typeof(CadProductivityPalette.Commands.PaletteCommand))]

[assembly: CommandClass(typeof(CadProductivityPalette.Commands.ReviewCommands))]
