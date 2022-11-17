using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Dialog
{
    public class MigrateOperatorsDialog : ModalDialog
    {
        public MigrateOperatorsDialog()
        {
            DialogSize = new Vector2(1200, 400);
            ItemSpacing = new Vector2(1, 1);
        }

        public void Draw()
        {
            if (BeginDialog("Import Operators from another Tooll installation"))
            {
                ImGui.BeginChild("options", new Vector2(ImGui.GetContentRegionAvail().X - 400, -1));
                {
                    CustomComponents.HelpText("This will help you to import Operators and resources from another Tooll installation.");

                    if (string.IsNullOrEmpty(_userNamespace))
                    {
                        _userNamespace = $"user.{UserSettings.Config.UserName}";
                    }

                    var hasChanged = false;
                    hasChanged |= CustomComponents.DrawStringParameter("User Namespace", ref _userNamespace);

                    var fullPath = (_otherToollDir != null && Directory.Exists(_otherToollDir)) ? Path.GetFullPath(_otherToollDir) : "";
                    var startUpPath = Path.GetFullPath(".");
                    //Log.Debug( $"{fullPath} vs {startUpPath}");

                    string warning = null;
                    if (!Directory.Exists(_otherToollDir))
                    {
                        warning = "Please select a directory with another Tooll installation";
                    }
                    else if (fullPath == startUpPath)
                    {
                        warning = "Can't import from itself";
                    }

                    hasChanged |= CustomComponents.DrawStringParameter("Tooll directory", ref _otherToollDir, null, warning, FileOperations.FilePickerTypes.Folder);

                    if (hasChanged)
                    {
                        ScanFolder();
                    }

                    var isValid = !string.IsNullOrEmpty(_otherToollDir) && !string.IsNullOrEmpty(_userNamespace);
                    CustomComponents.HelpText(_scanResultSummary);

                    if (CustomComponents.DisablableButton("Import", isValid))
                    {
                        //Todo: do something!
                        MigrateSelection();

                        ImGui.CloseCurrentPopup();
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
            var otherOperatorNamespaceDirectory = Path.Combine(_otherToollDir, @"Operators\Types\", nameSpaceFolder);
            
            if (!Directory.Exists(otherOperatorNamespaceDirectory))
            {
                _scanResultSummary = $"Tooll version doesn't have operator directory? {otherOperatorNamespaceDirectory}";
                return;
            }

            var opFiles = Directory.GetFiles(otherOperatorNamespaceDirectory, "*.cs", SearchOption.AllDirectories);
            var countNew = 0;
            var countExisting = 0;

            foreach (var filePath in opFiles)
            {
                var localPath = filePath.Replace(otherOperatorNamespaceDirectory, "");
                var newItem = new ScanItem()
                                  {
                                      FilePath = filePath,
                                      Title = Path.GetFileNameWithoutExtension(filePath),
                                      Status = ScanItem.Stati.Undefined,
                                      IsSelected = false,
                                      RemotePathInUserNameSpace = localPath,
                                  };
                
                if (File.Exists($@"Operators\Types\{_userNamespace.Replace(".", "\\")}\{localPath}"))
                {
                    newItem.Status = ScanItem.Stati.AlreadyExists;
                    Log.Debug($"localPath {localPath} exists");
                    countExisting++;
                }
                else
                {
                    newItem.Status = ScanItem.Stati.New;
                    newItem.IsSelected = true;
                    Log.Debug($" localPath {localPath} is new");
                    countNew++;
                }
                _scanResults.Add(newItem);
            }

            _scanResultSummary = $"Found {opFiles.Length} Operators ({countNew} new   {countExisting} existing";
        }

        private void MigrateSelection()
        {
            var t3NameSpaces = new string[] { "T3.Core.", "T3.Operators." };
            foreach (var item in _scanResults)
            {
                var fileContent = File.ReadAllText(item.FilePath);
                var newContent= Regex.Replace(fileContent, @"^using\s+([^;\s]+);\s*?\n" ,
                                              m =>
                                              {
                                                  var namespaceMatch = m.Groups[1].Value;
                                                  foreach (var wouldNeedFix in t3NameSpaces)
                                                  {
                                                      if (namespaceMatch.StartsWith(wouldNeedFix))
                                                      {
                                                          return "using T3." + namespaceMatch + ";\r\n"; 
                                                      }
                                                  }
                                                  return m.Value;
                                              }, RegexOptions.Multiline );

                foreach (var requiredNamespace in new[] { "using T3.Core.Operator.Resource;" })
                {
                    if(!newContent.Contains(requiredNamespace))
                    {
                        newContent = requiredNamespace + "\r\n" + newContent;
                    }
                }
                Log.Debug(newContent);
            }
        }
        
        private class ScanItem
        {
            public string Title;
            public string FilePath;
            public string RemotePathInUserNameSpace;
            public bool IsSelected ;
            public Stati Status;

            public enum Stati
            {
                Undefined,
                New,
                AlreadyExists,
            }
        }

        private string _scanResultSummary = null;
        private List<ScanItem> _scanResults = new List<ScanItem>();
        private string _otherToollDir;
        private string _userNamespace;
    }
}