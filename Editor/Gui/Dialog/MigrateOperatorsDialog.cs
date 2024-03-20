using System.IO;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.Model;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Dialog
{
    [Obsolete("This dialog has been deprecated due to the new project structure.")]
    public class MigrateOperatorsDialog : ModalDialog
    {
        public MigrateOperatorsDialog()
        {
            DialogSize = new Vector2(1200, 400);
            ItemSpacing = new Vector2(1, 1);
        }

        public void Draw()
        {
            FormInputs.SetIndentToParameters();
            if (BeginDialog("Import Operators from another Tooll installation"))
            {
                EditorUi.Instance.ShowMessageBox("This function has been deprecated due to the new project structure.");
                EndDialog();
                return;
                
                ImGui.BeginChild("options", new Vector2(ImGui.GetContentRegionAvail().X - 400, -1));
                {
                    FormInputs.AddHint("This will help you to import Operators and resources from another Tooll installation.");

                    if (string.IsNullOrEmpty(_userNamespace))
                    {
                        //_userNamespace = $"user.{UserSettings.Config.UserName}";
                    }

                    var hasChanged = false;
                    hasChanged |= !_initialized;
                    _initialized = true;

                    hasChanged |= FormInputs.AddStringInput("User Namespace", ref _userNamespace);

                    var fullPath = (_otherToollDir != null && Directory.Exists(_otherToollDir)) ? Path.GetFullPath(_otherToollDir) : "";
                    var startUpPath = Path.GetFullPath(".");

                    string warning = null;
                    if (!Directory.Exists(_otherToollDir))
                    {
                        warning = "Please select a directory with another Tooll installation";
                    }
                    else if (fullPath == startUpPath)
                    {
                        warning = "Can't import from itself";
                    }

                    hasChanged |= FormInputs.AddFilePicker("Tooll directory", ref _otherToollDir, null, warning,
                                                           FileOperations.FilePickerTypes.Folder);

                    if (hasChanged)
                    {
                        ScanFolder();
                    }

                    var isValid = !string.IsNullOrEmpty(_otherToollDir) && !string.IsNullOrEmpty(_userNamespace);
                    FormInputs.AddHint(_scanResultSummary);

                    FormInputs.ApplyIndent();
                    if (CustomComponents.DisablableButton("Import and Restart", isValid))
                    {
                        EditorUi.Instance.ShowMessageBox($"This function has been deprecated as a result of the new project structure.");
                        return;
                        EditorUi.Instance.ShowMessageBox("Tooll now has to restart to complete the import.");
                        EditorUi.Instance.ExitApplication();
                        //Application.Restart();
                        //Environment.Exit(0);

                        //ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndChild();
                ImGui.SameLine();

                // List results
                ImGui.BeginChild("Options");

                {
                    for (var index = 0; index < _scanResults.Count; index++)
                    {
                        ImGui.PushID(index);
                        var item = _scanResults[index];
                        ImGui.Checkbox(item.RemotePathInUserNameSpace + (item.Status == ScanItem.Stati.AlreadyExists ? " (exists)" : ""), ref item.IsSelected);
                        CustomComponents.TooltipForLastItem($"Target: {item.LocalFilePath}\nSource: {item.RemoteFilePath}");

                        ImGui.PopID();
                    }
                }

                EndDialogContent();
            }

            EndDialog();
        }

        private void ScanFolder()
        {
            _scanResults.Clear();
            if (!Directory.Exists(_otherToollDir))
            {
                _scanResultSummary = $"Directory {_otherToollDir} does not exist";
                return;
            }

            var nameSpaceFolder = _userNamespace.Replace(".", @"\");
            _otherOperatorNamespaceDirectory = $@"{_otherToollDir}\Operators\Types\{nameSpaceFolder}";
            _localOperatorNamespaceDirectory = $@"Operators\Types\{nameSpaceFolder}";

            if (!Directory.Exists(_otherOperatorNamespaceDirectory))
            {
                _scanResultSummary = $"Tooll version doesn't have operator directory? {_otherOperatorNamespaceDirectory}";
                return;
            }

            // Scan local files for namespace to allow for moving of files
            var allLocalFilesWithFilePath = new Dictionary<string, string>();
            if (Directory.Exists(_localOperatorNamespaceDirectory))
            {
                foreach (var filePath in Directory.GetFiles(_localOperatorNamespaceDirectory, "", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(filePath);
                    if (!allLocalFilesWithFilePath.TryAdd(fileName, filePath))
                    {
                        Log.Warning($"Skipping double definition of {fileName} in {filePath}");
                    }
                }
            }

            // Scan for ops
            var countNew = 0;
            var countExisting = 0;
            var opFiles = Directory.GetFiles(_otherOperatorNamespaceDirectory, "*.cs", SearchOption.AllDirectories);
            foreach (var remoteFilePath in opFiles)
            {
                var filePathWithinNamespace = remoteFilePath.Replace(_otherOperatorNamespaceDirectory, "");
                var fileName = Path.GetFileName(remoteFilePath);
                var newItem = new ScanItem()
                                  {
                                      RemoteFilePath = remoteFilePath,
                                      Title = Path.GetFileNameWithoutExtension(remoteFilePath),
                                      Status = ScanItem.Stati.Undefined,
                                      IsSelected = false,
                                      LocalFilePath = null,
                                      RemotePathInUserNameSpace = filePathWithinNamespace,
                                  };

                if (allLocalFilesWithFilePath.ContainsKey(fileName))
                {
                    newItem.Status = ScanItem.Stati.AlreadyExists;
                    newItem.LocalFilePath = allLocalFilesWithFilePath[fileName];
                    Log.Debug($"localPath {filePathWithinNamespace} exists");
                    countExisting++;
                }
                else
                {
                    newItem.Status = ScanItem.Stati.New;
                    newItem.LocalFilePath = _localOperatorNamespaceDirectory + filePathWithinNamespace;
                    newItem.IsSelected = true;
                    Log.Debug($"localPath {filePathWithinNamespace} is new");
                    countNew++;
                }

                _scanResults.Add(newItem);
            }

            // Scan for resource files
            var otherResourceDirectory = _otherToollDir + @"\Resources\" + nameSpaceFolder;
            _otherResourceFiles.Clear();
            if (Directory.Exists(otherResourceDirectory))
            {
                _otherResourceFiles.AddRange(Directory.GetFiles(otherResourceDirectory, "", SearchOption.AllDirectories));
            }

            _scanResultSummary = $"Found {opFiles.Length} Operators ({countNew} new / {countExisting} existing)  {_otherResourceFiles.Count} resource files";
        }

        // todo - should this really be here?
        /*private void MigrateSelection()
        {
            string[] allRemoteT3Files;
            string[] allRemoteT3UiFiles;

            try
            {
                const string t3FilePattern = $"*{SymbolPackage.SymbolExtension}";
                const string t3UiFilePattern = $"*{EditorSymbolPackage.SymbolUiExtension}";
                allRemoteT3Files = Directory.GetFiles(_otherOperatorNamespaceDirectory, t3FilePattern, SearchOption.AllDirectories);
                allRemoteT3UiFiles = Directory.GetFiles(_otherOperatorNamespaceDirectory, t3UiFilePattern, SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                Log.Error("Migration failed: " + e.Message);
                return;
            }

            foreach (var item in _scanResults)
            {
                if (!item.IsSelected)
                    continue;

                // Adjust and copy source code file
                var sourceCode = File.ReadAllText(item.RemoteFilePath);
                sourceCode = AddRequiredNameSpaces(sourceCode);
                sourceCode = FixLocalNamespaceUsages(sourceCode);

                var targetDir = Path.GetDirectoryName(item.LocalFilePath);
                try
                {
                    if (!Directory.Exists(targetDir))
                        Directory.CreateDirectory(targetDir);
                }
                catch (Exception e)
                {
                    Log.Warning($"can't create target directory {targetDir}: " + e.Message);
                    continue;
                }

                File.WriteAllText(item.LocalFilePath, sourceCode);

                // Copy tooll-files

                foreach (var fileList in new[] { allRemoteT3Files, allRemoteT3UiFiles })
                {
                    var otherFile =
                        fileList.FirstOrDefault(f =>
                                                    Regex.IsMatch(f,
                                                                  item.Title +
                                                                  @"_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\."));
                    if (otherFile == null)
                    {
                        Log.Warning($"Can't find .t3 or .t3ui file for {item.Title}");
                        continue;
                    }

                    var targetFilePath = otherFile.Replace(_otherOperatorNamespaceDirectory, _localOperatorNamespaceDirectory);
                    try
                    {
                        File.Copy(otherFile, targetFilePath);
                    }
                    catch (Exception e)
                    {
                        Log.Warning($"Can't copy file {otherFile} -> {targetFilePath} " + e.Message);
                    }
                }
            }

            // Copy Resource files
            foreach (var otherResourceFilePath in _otherResourceFiles)
            {
                var targetFileName = otherResourceFilePath.Replace(_otherToollDir, ".");
                try
                {
                    var targetDirectory = Path.GetDirectoryName(targetFileName);
                    if (File.Exists(targetFileName))
                    {
                        Log.Debug($"Overwriting {targetFileName}");
                    }
                    else
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    File.Copy(otherResourceFilePath, targetFileName, overwrite: true);
                }
                catch (Exception e)
                {
                    Log.Warning($"Can't copy resource files {targetFileName}: " + e.Message);
                }
            }

            static string AddRequiredNameSpaces(string sourceCode)
            {
                var requiredNamespaces = new[] { "using T3.Core.DataTypes;" };
                foreach (var requiredNamespace in requiredNamespaces)
                {
                    if (!sourceCode.Contains(requiredNamespace))
                    {
                        sourceCode = requiredNamespace + "\r\n" + sourceCode;
                    }
                }

                return sourceCode;
            }

            static string FixLocalNamespaceUsages(string sourceCode)
            {
                foreach (var (key, value) in StringReplacements)
                {
                    sourceCode = sourceCode.Replace(key, value);
                }

                return sourceCode;
            }
        }*/

        private static readonly Dictionary<string, string> StringReplacements
            = new()
                  {
                      { "<T3.Core.Command>", "<Command>" }
                  };

        private class ScanItem
        {
            public string Title;
            public string RemoteFilePath;
            public string LocalFilePath;
            public string RemotePathInUserNameSpace;
            public bool IsSelected;
            public Stati Status;

            public enum Stati
            {
                Undefined,
                New,
                AlreadyExists,
            }
        }

        private bool _initialized;
        private string _scanResultSummary;
        private readonly List<ScanItem> _scanResults = new();
        private readonly List<string> _otherResourceFiles = new();
        private string _otherToollDir;
        private string _userNamespace;
        private string _otherOperatorNamespaceDirectory;
        private string _localOperatorNamespaceDirectory;
    }
}