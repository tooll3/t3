using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using T3.Core.Logging;
using T3.Gui.InputUi;

namespace T3.Gui.UiHelpers
{
    public static class FileOperations
    {
        public static string PickResourceFilePath(string initialPath= "")
        {
            var path = String.IsNullOrEmpty(initialPath)
                           ? GetAbsoluteResourcePath()
                           : initialPath;
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = path;
                openFileDialog.Filter = "jpg files (*.jpg)|*.jpg|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

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
            File,
            Folder,
        }
        
        public static bool DrawFilePicker(FilePickerTypes type, ref string value)
        {
            ImGui.SetNextItemWidth(-70);
            var modified = ImGui.InputText("##filepath", ref ProjectSettings.Config.SoundtrackFilepath, 255);
            
            modified |= DrawSelector(type, ref value);
            return modified;
        }
        
        
        public static bool DrawSelector(FilePickerTypes type, ref string value)
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
                        Process.Start(value);
                    }
                    catch (Win32Exception e)
                    {
                        Log.Warning("Can't open editor: " + e.Message);
                    }
                }
            }
            return modified;
        }
        
        private static string ConvertToRelativeFilepath(string absoluteFilePath)
        {
            var currentApplicationPath = System.IO.Path.GetFullPath(".");
            var firstCharUppercase = currentApplicationPath.Substring(0, 1).ToUpper();
            currentApplicationPath = firstCharUppercase + currentApplicationPath.Substring(1, currentApplicationPath.Length - 1) + "\\";
            var relativeFilePath = absoluteFilePath.Replace(currentApplicationPath, "");
            return relativeFilePath;
        }

        private static string GetAbsoluteResourcePath()
        {
            return System.IO.Path.Combine(System.IO.Path.GetFullPath("."), "Resources");
        }
        
        
        
        
    }
}