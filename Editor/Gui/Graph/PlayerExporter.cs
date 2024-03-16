#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SharpDX.Direct3D11;
using T3.Core.Compilation;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.App;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Serialization;

namespace T3.Editor.Gui.Graph
{
    public static partial class PlayerExporter
    {
        public static bool TryExportInstance(GraphCanvas graphCanvas, SymbolChildUi childUi, out string reason, out string exportDir)
        {
            T3Ui.Save(false);

            // Collect all ops and types
            var instance = graphCanvas.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
            var symbol = instance.Symbol;
            Log.Info($"Exporting {symbol.Name}...");

            var output = instance.Outputs.FirstOrDefault();
            if (output == null || output.ValueType != typeof(Texture2D))
            {
                reason = "Can only export ops with 'Texture2D' output";
                exportDir = string.Empty;
                return false;
            }

            
            // traverse starting at output and collect everything
            var exportInfo = new ExportInfo();
            CollectChildSymbols(instance.Symbol, exportInfo);

            exportDir = Path.Combine(UserSettings.Config.DefaultNewProjectDirectory, "Exports", childUi.SymbolChild.ReadableName);

            try
            {
                if(Directory.Exists(exportDir))
                {
                    Directory.Move(exportDir, exportDir + '_' + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                }
            }
            catch (Exception e)
            {
                reason = $"Failed to move export dir: {exportDir}. Exception: {e}";
                return false;
            }

            Directory.CreateDirectory(exportDir);

            // copy assemblies into export dir
            // get symbol packages directly used by the exported symbols
            var primarySymbolPackages = exportInfo.UniqueSymbols
                                                  .Select(symbol => symbol.SymbolPackage)
                                                  .Distinct()
                                                  .ToArray();
            
            // get symbol packages indirectly referenced by the primary symbol packages
            var dependencyReferences = primarySymbolPackages
                                      .SelectMany(package => package.ResourceDependencies)
                                      .Distinct();

            // combine primary and dependency symbol packages
            var symbolPackages = primarySymbolPackages
                                .Concat(dependencyReferences)
                                .Distinct();

            var operatorDir = Path.Combine(exportDir, "Operators");
            Directory.CreateDirectory(operatorDir);
            
            if (!TryExportPackages(out reason, symbolPackages, operatorDir))
                return false;

            // Copy referenced resources
            RecursivelyCollectExportData(output, exportInfo);
            exportInfo.PrintInfo();

            var resourceDir = Path.Combine(exportDir, "Resources");
            
            //var symbolPlaybackSettings = childUi.SymbolChild.Symbol.PlaybackSettings;
            //var audioClipLocation = FindAudioClip(symbolPlaybackSettings, ref errorCount);
            //if (audioClipLocation != null)
            //{
            //    var audioClipPath = Path.Combine(resourceDir, Path.GetFileName(audioClipLocation));
            //    TryCopyFile(audioClipLocation, audioClipPath);
            //}

            if(!TryCopyDirectory(SharedResources.Directory, resourceDir, out reason))
                return false;
            
            var playerDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Player");
            if(!TryCopyDirectory(playerDirectory, exportDir, out reason))
                return false;

            //var copied = TryCopyFiles(exportInfo.UniqueResourcePaths, resourceDir);

            // Update project settings
            var exportSettings = new ExportSettings(OperatorId: symbol.Id, 
                                                    ApplicationTitle: symbol.Name, 
                                                    WindowMode: WindowMode.Fullscreen, 
                                                    ConfigData: ProjectSettings.Config,
                                                    Author: symbol.SymbolPackage.AssemblyInformation.Name); // todo - actual author name
            
            const string exportSettingsFile = "exportSettings.json";
            if(!JsonUtils.TrySaveJson(exportSettings, Path.Combine(exportDir, exportSettingsFile)))
            {
                reason = $"Failed to save export settings to {exportSettingsFile}";
                return false;
            }

            reason = "Exported successfully to " + exportDir;
            return true;
        }

        // todo - can we handle resource references here too?
        private static bool TryExportPackages(out string reason, IEnumerable<SymbolPackage> symbolPackages, string operatorDir)
        {
            string[] excludeSubdirectories = [EditorSymbolPackage.SymbolUiSubFolder, EditorSymbolPackage.SourceCodeSubFolder, ".git"];
            foreach (var package in symbolPackages)
            {
                Log.Debug($"Exporting package {package.AssemblyInformation.Name}");
                var name = package.AssemblyInformation.Name;
                var targetDirectory = Path.Combine(operatorDir, name);
                _ = Directory.CreateDirectory(targetDirectory);
                if (package is EditableSymbolProject project)
                {
                    project.SaveModifiedSymbols();
                    var compiled = project.CsProjectFile.TryCompileRelease(targetDirectory);
                    if (!compiled)
                    {
                        reason = $"Failed to compile project \"{name}\"";
                        return false;
                    }

                    // delete extraneous folders
                    var symbolUiDir = Path.Combine(targetDirectory, EditorSymbolPackage.SymbolUiSubFolder);
                    var sourceCodeDir = Path.Combine(targetDirectory, EditorSymbolPackage.SourceCodeSubFolder);
                    var gitDir = Path.Combine(targetDirectory, ".git");
                    
                    try
                    {
                        Directory.Delete(symbolUiDir, true);
                        Directory.Delete(sourceCodeDir, true);
                        Directory.Delete(gitDir, true);
                    }
                    catch (Exception e)
                    {
                        reason = $"Failed to delete extraneous folders in {targetDirectory}. Exception:\n{e}";
                        return false;
                    }
                }
                else
                {
                    // copy full directory into target directory recursively, maintaining folder layout
                    var directoryToCopy = package.AssemblyInformation.Directory;
                    if (!TryCopyDirectory(directoryToCopy, targetDirectory, out reason, excludeSubdirectories))
                        return false;
                }
            }
            
            reason = string.Empty;
            return true;
        }

        private static bool TryCopyDirectory(string directoryToCopy, string targetDirectory, out string reason, string[]? excludeSubFolders = null, string[]? excludeFiles = null, string[]? excludeFileExtensions = null)
        {
            try
            {
                var rootFiles = Directory.EnumerateFiles(directoryToCopy, "*", SearchOption.TopDirectoryOnly);
                var subfolderFiles = Directory.EnumerateDirectories(directoryToCopy, "*", SearchOption.TopDirectoryOnly)
                                              .Where(subDir =>
                                                     {
                                                         if(excludeSubFolders == null)
                                                             return true;
                                                         
                                                         var dirName = Path.GetRelativePath(directoryToCopy, subDir);
                                                         foreach (var excludeSubFolder in excludeSubFolders)
                                                         {
                                                             if (string.Equals(dirName, excludeSubFolder, StringComparison.OrdinalIgnoreCase))
                                                             {
                                                                 return false;
                                                             }
                                                         }

                                                         return true;
                                                     })
                                              .SelectMany(subDir => Directory.EnumerateFiles(subDir, "*", SearchOption.AllDirectories));
                
                var files = rootFiles.Concat(subfolderFiles);
                var shouldExcludeFiles = excludeFiles != null;
                var shouldExcludeFileExtensions = excludeFileExtensions != null;
                foreach (var file in files)
                {
                    if (shouldExcludeFiles && excludeFiles!.Contains(Path.GetFileName(file)))
                        continue;

                    bool shouldSkipBasedOnExtension = false;
                    if (shouldExcludeFileExtensions)
                    {
                        foreach(var extension in excludeFileExtensions!)
                        {
                            if (file.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                            {
                                shouldSkipBasedOnExtension = true;
                                break;
                            }
                        }
                    }
                    
                    if (shouldSkipBasedOnExtension)
                        continue;
                    
                    var relativePath = Path.GetRelativePath(directoryToCopy, file);
                    var targetPath = Path.Combine(targetDirectory, relativePath);
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (targetDir == null)
                    {
                        reason = $"Failed to get directory for \"{targetPath}\" - is it missing a file extension?";
                        return false;
                    }
                    Directory.CreateDirectory(targetDir!);
                    File.Copy(file, targetPath, true);
                }
            }
            catch (Exception e)
            {
                reason = $"Failed to copy directory {directoryToCopy} to {targetDirectory}. Exception:\n{e}";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private readonly struct ResourcePath(string relativePath, string absolutePath)
        {
            public readonly string RelativePath = relativePath;
            public readonly string AbsolutePath = absolutePath;
        }

        private static bool TryCopyFiles(IEnumerable<ResourcePath> resourcePaths, string targetDir)
        {
            var successInt = Convert.ToInt32(true);
            resourcePaths
               .AsParallel()
               .ForAll(resourcePath =>
                       {
                           var targetPath = Path.Combine(targetDir, resourcePath.RelativePath);
                           var success = TryCopyFile(resourcePath.AbsolutePath, targetPath);

                           // Check for success
                           Interlocked.And(ref successInt, Convert.ToInt32(success));
                           if (!success)
                           {
                               Log.Error($"Failed to copy resource file for export: {resourcePath.AbsolutePath}");
                           }
                       });

            return Convert.ToBoolean(successInt);
        }

        private static bool TryCopyFile(string sourcePath, string targetPath)
        {
            var directory = Path.GetDirectoryName(targetPath);
            try
            {
                Directory.CreateDirectory(directory!);
                File.Copy(sourcePath, targetPath, true);
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to copy resource file for export: {sourcePath}  {e.Message}");
            }

            return false;
        }

        private static void CollectChildSymbols(Symbol symbol, ExportInfo exportInfo)
        {
            if (!exportInfo.TryAddSymbol(symbol))
                return; // already visited

            foreach (var symbolChild in symbol.Children)
            {
                CollectChildSymbols(symbolChild.Symbol, exportInfo);
            }
        }

        private static void RecursivelyCollectExportData(ISlot slot, ExportInfo exportInfo)
        {
            var gotConnection = slot.TryGetFirstConnection(out var firstConnection);
            if (slot is IInputSlot)
            {
                if (gotConnection)
                {
                    RecursivelyCollectExportData(firstConnection, exportInfo);
                }

                CheckInputForResourcePath(slot, exportInfo);
                return;
            }

            if (gotConnection)
            {
                // slot is an output of an composition op
                RecursivelyCollectExportData(firstConnection, exportInfo);
                exportInfo.TryAddInstance(slot.Parent);
                return;
            }

            var parent = slot.Parent;

            if (!exportInfo.TryAddInstance(parent))
                return; // already visited

            foreach (var input in parent.Inputs)
            {
                CheckInputForResourcePath(input, exportInfo);

                if (!input.IsConnected)
                    continue;

                if (input.TryGetAsMultiInput(out var multiInput))
                {
                    foreach (var entry in multiInput.GetCollectedInputs())
                    {
                        RecursivelyCollectExportData(entry, exportInfo);
                    }
                }
                else if (input.TryGetFirstConnection(out var inputsFirstConnection))
                {
                    RecursivelyCollectExportData(inputsFirstConnection, exportInfo);
                }
            }
        }

        private static string? FindAudioClip(PlaybackSettings symbolPlaybackSettings, ref int errorCount)
        {
            var soundtrack = symbolPlaybackSettings?.AudioClips.SingleOrDefault(ac => ac.IsSoundtrack);
            if (soundtrack == null)
            {
                if (PlaybackUtils.TryFindingSoundtrack(out var otherSoundtrack, out var composition))
                {
                    Log.Warning($"You should define soundtracks withing the exported operators. Falling back to {otherSoundtrack.FilePath} set in parent...");
                    errorCount++;

                    if(!otherSoundtrack.TryGetAbsoluteFilePath(out var absolutePath))
                    {
                        Log.Error($"Failed to find absolute path for {otherSoundtrack.FilePath} in [{composition.Symbol.Name}]");
                    }
                    return absolutePath;
                }

                Log.Debug("No soundtrack defined within operator.");
                return null;
            }

            return soundtrack.FilePath;
        }

        private static void CheckInputForResourcePath(ISlot inputSlot, ExportInfo exportInfo)
        {
            var parent = inputSlot.Parent;
            var inputUi = SymbolUiRegistry.Entries[parent.Symbol.Id].InputUis[inputSlot.Id];
            if (inputUi is not StringInputUi stringInputUi)
                return;

            if (stringInputUi.Usage != StringInputUi.UsageType.FilePath && stringInputUi.Usage != StringInputUi.UsageType.DirectoryPath)
                return;

            var compositionSymbol = parent.Parent.Symbol;
            var parentSymbolChild = compositionSymbol.Children.Single(child => child.Id == parent.SymbolChildId);
            var value = parentSymbolChild.Inputs[inputSlot.Id].Value;
            if (value is not InputValue<string> stringValue)
                return;

            switch (stringInputUi.Usage)
            {
                case StringInputUi.UsageType.FilePath:
                {
                    var relativePath = stringValue.Value;
                    exportInfo.TryAddSharedResource(relativePath, parent.AvailableResourceFolders);
                    break;
                }
                case StringInputUi.UsageType.DirectoryPath:
                {
                    var relativeDirectory = stringValue.Value;

                    if (!ResourceManager.TryResolvePath(relativeDirectory, parent.AvailableResourceFolders, out var absoluteDirectory))
                    {
                        Log.Warning($"Directory '{relativeDirectory}' was not found in any resource folder");
                    }

                    Log.Debug($"Export all entries folder {absoluteDirectory}...");
                    var rootDirectory = absoluteDirectory.Replace(relativeDirectory, string.Empty);
                    foreach (var absolutePath in Directory.EnumerateFiles(absoluteDirectory, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = absolutePath.Replace(rootDirectory, string.Empty);
                        exportInfo.TryAddResourcePath(new ResourcePath(relativePath, absolutePath));
                    }

                    break;
                }
                case StringInputUi.UsageType.Default:
                case StringInputUi.UsageType.Multiline:
                case StringInputUi.UsageType.CustomDropdown:
                default:
                    break;
            }
        }
    }
}