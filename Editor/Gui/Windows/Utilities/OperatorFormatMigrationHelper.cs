using System.IO;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows;

/// <summary>
///
/// Helps to convert from old operator format to new operator format
///
///
/// 
/// </summary>
///
///

// OLD:

// using T3.Core.DataTypes;
// using T3.Core.Operator;
// using T3.Core.Operator.Attributes;
// using T3.Core.Operator.Slots;
//
// namespace T3.Operators.Types.Id_cce36a29_8f66_492d_bf8f_b924fa1ae384
// {
//     public class SetContextVariableExample : Instance<SetContextVariableExample>
//     {
//         [Output(Guid = "30ec9e5d-97af-4621-9e20-ef651bfd2ec3")]
//         public readonly Slot<Command> Output = new();
//
//
//     }
// }

// NEW:
// using System.Runtime.InteropServices;
// using T3.Core.DataTypes;
// using T3.Core.Operator;
// using T3.Core.Operator.Attributes;
// using T3.Core.Operator.Slots;
//
// namespace examples.lib.exec
// {
//     [Guid("cce36a29-8f66-492d-bf8f-b924fa1ae384")]
//     public class SetContextVariableExample : Instance<SetContextVariableExample>
//     {
//         [Output(Guid = "30ec9e5d-97af-4621-9e20-ef651bfd2ec3")]
//         public readonly Slot<Command> Output = new();
//
//
//     }
// }
static class OperatorFormatMigrationHelper
{
    public static void Draw()
    {
        FormInputs.AddSectionHeader("This is an internal tool for migrating operators to new versions. Use with caution.");
        var filepathModified = FormInputs.AddFilePicker("Operator Folder",
                                                        ref _otherOpDirectory,
                                                        "operator folder",
                                                        _warning,
                                                        FileOperations.FilePickerTypes.Folder
                                                       );
        if (filepathModified)
        {
            _warning = Directory.Exists(_otherOpDirectory) ? null : "Please select a valid folder";
        }

        if (ImGui.Button("Migrate Operators"))
        {
            var files = Directory.GetFiles(_otherOpDirectory, "*.cs", SearchOption.AllDirectories);
            foreach (var filepath in files)
            {
                var fileContent = File.ReadAllText(filepath);

                var match = Regex.Match(fileContent, @"namespace T3.Operators.Types.Id_(\w{8}_\w{4}_\w{4}_\w{4}_\w{12})");
                if (!match.Success)
                    continue;
                
                var nameSpaceGuid = match.Groups[1].Value;
                var operatorGuid = nameSpaceGuid.Replace("_", "-");

                var directory = Path.GetDirectoryName(filepath);
                if (string.IsNullOrEmpty(directory))
                    continue;
                
                // Derive namespace from directory and replace the old namespace
                var nameSpace = directory.Replace(_otherOpDirectory, "").Replace("\\", ".");
                if (nameSpace.StartsWith("."))
                    nameSpace = nameSpace[1..];
                
                var newContent = fileContent.Replace(
                                                     $"namespace T3.Operators.Types.Id_{nameSpaceGuid}",
                                                     $"namespace {nameSpace}");
                
                // Insert Guid() attribute to class
                var symbolName = Path.GetFileNameWithoutExtension(filepath);
                newContent = newContent.Replace($"public sealed class {symbolName}",
                                                $"[Guid(\"{operatorGuid}\")]\n    public class {symbolName}");
                
                // insert interop using
                const string usingSystemRuntimeInterOp = "using System.Runtime.InteropServices;";
                if (!newContent.Contains(usingSystemRuntimeInterOp))
                {
                    newContent = usingSystemRuntimeInterOp + "\n" + newContent;
                }
                
                Log.Debug($"writing new format {filepath}: {newContent}");
                File.WriteAllText(filepath, newContent);
            }
        }
    }

    private static string _otherOpDirectory = "..\\t3-main\\Operators";
    private static string _warning = "Please select a soundtrack file";
}