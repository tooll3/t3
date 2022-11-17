using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using T3.Core.Logging;

namespace T3.Editor.Gui.UiHelpers
{
    public static class FileOperations
    {
        private const string ResourcesFolder = "Resources";

        public static string PickResourceFilePath(string initialPath = "")
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = String.IsNullOrEmpty(initialPath)
                                                      ? GetAbsoluteResourcePath()
                                                      : GetAbsoluteDirectory(initialPath);
                openFileDialog.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return null;

                var absolutePath = openFileDialog.FileName;
                return ConvertToRelativeFilepath(absolutePath);
            }
        }

        public static string PickResourceDirectory()
        {
            using (var folderBrowser = new OpenFileDialog())
            {
                folderBrowser.InitialDirectory = GetAbsoluteResourcePath();
                folderBrowser.ValidateNames = false;
                folderBrowser.CheckFileExists = false;
                folderBrowser.CheckPathExists = true;
                folderBrowser.FileName = "Folder Selection.";
                if (folderBrowser.ShowDialog() != DialogResult.OK)
                    return null;

                var absoluteFolderPath = System.IO.Path.GetDirectoryName(folderBrowser.FileName);
                return ConvertToRelativeFilepath(absoluteFolderPath);
            }
        }

        public enum FilePickerTypes
        {
            None,
            File,
            Folder,
        }

        public static bool DrawSoundFilePicker(FilePickerTypes type, ref string value)
        {
            ImGui.SetNextItemWidth(-70);
            var tmp = value;
            if (tmp == null)
                tmp = string.Empty;

            var modified = ImGui.InputText("##filepath", ref tmp, 255);
            modified |= DrawFileSelector(type, ref tmp);
            if (modified && tmp != null)
            {
                value = tmp;
            }

            return modified;
        }

        public static bool DrawFileSelector(FilePickerTypes type, ref string value)
        {
            var modified = false;
            ImGui.SameLine();
            if (ImGui.Button("...", new Vector2(30, 0)))
            {
                string newPath = type == FilePickerTypes.File
                                     ? FileOperations.PickResourceFilePath(value)
                                     : FileOperations.PickResourceDirectory();
                if (!string.IsNullOrEmpty(newPath))
                {
                    value = newPath;
                    modified = true;
                }
            }

            if (type == FilePickerTypes.File)
            {
                ImGui.SameLine();
                if (ImGui.Button("Edit", new Vector2(40, 0)))
                {
                    if (!File.Exists(value))
                    {
                        Log.Error("Can't open non-existing file " + value);
                    }
                    else
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(value) { UseShellExecute = true });
                        }
                        catch (Win32Exception e)
                        {
                            Log.Warning("Can't open editor: " + e.Message);
                        }
                    }
                }
            }

            return modified;
        }

        private static string ConvertToRelativeFilepath(string absoluteFilePath)
        {
            var currentApplicationPath = Path.GetFullPath(".");
            var firstCharUppercase = currentApplicationPath.Substring(0, 1).ToUpper();
            currentApplicationPath = firstCharUppercase + currentApplicationPath.Substring(1, currentApplicationPath.Length - 1) + "\\";
            var relativeFilePath = absoluteFilePath.Replace(currentApplicationPath, "");
            return relativeFilePath;
        }

        private static string GetAbsoluteResourcePath()
        {
            return Path.Combine(Path.GetFullPath("."), ResourcesFolder);
        }

        private static string GetAbsoluteDirectory(string relativeFilepath)
        {
            var absolutePath = GetAbsoluteResourcePath();
            return Path.GetDirectoryName(Path.Combine(absolutePath, relativeFilepath.Replace(ResourcesFolder + "\\", "")));
        }
    }
}