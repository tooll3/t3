#nullable enable
using System.IO;
using System.Threading;
using SharpDX.Direct3D11;
using T3.Core.Compilation;
using T3.Core.IO;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.App;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using T3.Serialization;

namespace T3.Editor.Gui.Graph
{
    internal static partial class PlayerExporter
    {
        public const string ExportFolderName = "T3Exports";
        public static bool TryExportInstance(Instance composition, SymbolChildUi childUi, out string reason, out string exportDir)
        {
            T3Ui.Save(false);

            // Collect all ops and types
            var instance = composition.GetChildInstanceWith(childUi.SymbolChild);
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

            exportDir = Path.Combine(UserSettings.Config.DefaultNewProjectDirectory, ExportFolderName, childUi.SymbolChild.ReadableName);

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

            var operatorDir = Path.Combine(exportDir, "Operators");
            Directory.CreateDirectory(operatorDir);

            // copy assemblies into export dir
            // get symbol packages directly used by the exported symbols
            Dictionary<SymbolPackage, List<Symbol>> symbolPackageToSymbols = new();
            foreach(var uniqueSymbol in exportInfo.UniqueSymbols)
            {
                var symbolPackage = uniqueSymbol.SymbolPackage;
                if (!symbolPackageToSymbols.TryGetValue(symbolPackage, out var symbols))
                {
                    symbols = new List<Symbol>();
                    symbolPackageToSymbols[symbolPackage] = symbols;
                }
                symbols.Add(uniqueSymbol);
            }
            
            var symbolPackages = symbolPackageToSymbols.Keys;
            
            if (!TryExportPackages(out reason, symbolPackages, operatorDir))
                return false;

            // Copy referenced resources
            RecursivelyCollectExportData(output, exportInfo);
            exportInfo.PrintInfo();

            var resourceDir = Path.Combine(exportDir, ResourceManager.ResourcesSubfolder);
            Directory.CreateDirectory(resourceDir);
            
            if (!TryFindSoundtrack(symbol, out var absolutePath, out var relativePath))
            {
                reason = $"Failed to find soundTrack for [{symbol.Name}] with relative path \"{relativePath}\"";
                return false;
            }
            
            if (absolutePath != null)
            {
                if (Path.IsPathRooted(relativePath))
                {
                    reason = $"Soundtrack path is not relative: \"{relativePath}\"";
                    return false;
                }
                
                var newPath = Path.Combine(resourceDir, relativePath!);
                if(!TryCopyFile(absolutePath, newPath))
                {
                    reason = $"Failed to copy soundtrack from \"{absolutePath}\" to \"{newPath}\"";
                    return false;
                }
            }

            if(!TryCopyDirectory(SharedResources.Directory, resourceDir, out reason))
                return false;
            
            var playerDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Player");
            if(!TryCopyDirectory(playerDirectory, exportDir, out reason))
                return false;

            if (!TryCopyFiles(exportInfo.UniqueResourcePaths, resourceDir))
            {
                reason = "Failed to copy resource files - see log for details";
                return false;
            }

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
            string[] excludeSubdirectories = [EditorSymbolPackage.SymbolUiSubFolder, EditorSymbolPackage.SourceCodeSubFolder, ".git", ResourceManager.ResourcesSubfolder];
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
            
            // equality operators
            public static bool operator ==(ResourcePath left, ResourcePath right) => left.AbsolutePath == right.AbsolutePath;
            public static bool operator !=(ResourcePath left, ResourcePath right) => left.AbsolutePath != right.AbsolutePath;
            public override int GetHashCode() => AbsolutePath.GetHashCode();
            public override bool Equals(object? obj) => obj is ResourcePath other && other == this;
            
            public override string ToString() => $"\"{RelativePath}\" (\"{AbsolutePath}\")";
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

            foreach (var symbolChild in symbol.Children.Values)
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

        private static bool TryFindSoundtrack(Symbol symbol, out string? absolutePath, out string? relativePath)
        {
            var playbackSettings = symbol.PlaybackSettings;
            var soundtrack = playbackSettings?.AudioClips.SingleOrDefault(ac => ac.IsSoundtrack);
            if (soundtrack == null)
            {
                if (PlaybackUtils.TryFindingSoundtrack(out soundtrack, out _))
                {
                    Log.Warning($"You should define soundtracks withing the exported operators. Falling back to {soundtrack.FilePath} set in parent...");
                }
                else
                {
                    relativePath = null;
                    absolutePath = null;
                    return true;
                }

                Log.Debug("No soundtrack defined within operator.");
            }

            relativePath = soundtrack.FilePath;
            return soundtrack.TryGetAbsoluteFilePath(out absolutePath);
        }

        private static void CheckInputForResourcePath(ISlot inputSlot, ExportInfo exportInfo)
        {
            var parent = inputSlot.Parent;
            var inputUi = parent.GetSymbolUi().InputUis[inputSlot.Id];
            if (inputUi is not StringInputUi stringInputUi)
                return;

            if (stringInputUi.Usage != StringInputUi.UsageType.FilePath && stringInputUi.Usage != StringInputUi.UsageType.DirectoryPath)
                return;

            var compositionSymbol = parent.Parent.Symbol;
            var parentSymbolChild = compositionSymbol.Children[parent.SymbolChildId];
            var value = parentSymbolChild.Inputs[inputSlot.Id].Value;
            if (value is not InputValue<string> stringValue)
                return;

            switch (stringInputUi.Usage)
            {
                case StringInputUi.UsageType.FilePath:
                {
                    var relativePath = stringValue.Value;
                    exportInfo.TryAddSharedResource(relativePath, parent.AvailableResourcePackages);
                    break;
                }
                case StringInputUi.UsageType.DirectoryPath:
                {
                    var relativeDirectory = stringValue.Value;

                    if (!ResourceManager.TryResolvePath(relativeDirectory, parent.AvailableResourcePackages, out var absoluteDirectory, out _))
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