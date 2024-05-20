using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows;

static class OperatorFormatMigrationHelper
{
    public static void Draw()
    {
        FormInputs.AddSectionHeader("This is an internal tool for migrating operators to new versions. Use with caution.");
        var filepathModified = FormInputs.AddFilePicker("Operator Folder",
                                                        ref _soundtrackFilePath,
                                                        "operator folder",
                                                        warning,
                                                        FileOperations.FilePickerTypes.Folder
                                                       );

        if (ImGui.Button("Migrate Operators"))
        {
            Log.Debug("...should do something here");
        }

    }
    
    private static string _soundtrackFilePath = "";
    private static string warning = "Please select a soundtrack file";
}