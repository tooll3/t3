using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SharpDX.Direct3D11;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

// ReSharper disable StringLiteralTypo

namespace T3.Editor.Gui.Graph
{
    public static class PlayerExporter
    {
        public static void ExportInstance(GraphCanvas graphCanvas, SymbolChildUi childUi)
        {
            T3Ui.Save(true);
            
            var homePackage = graphCanvas.CompositionOp.Symbol.SymbolPackage;
            if (!homePackage.IsModifiable)
            {
                EditorUi.Instance.ShowMessageBox("Cannot export symbols from non-modifiable packages.");
                return;
            }

            var exportDir = Path.Combine(homePackage.Folder, "bin", childUi.SymbolChild.ReadableName);

            // Collect all ops and types
            var instance = graphCanvas.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
            Log.Info($"Exporting {instance.Symbol.Name}...");
            var errorCount = 0;

            if (instance.Outputs.Count < 1 || instance.Outputs.First().ValueType != typeof(Texture2D))
            {
                Log.Warning("Can only export ops with 'Texture2D' output");
                return;
            }

            // Update project settings
            ProjectSettings.Config.MainOperatorName = instance.Symbol.Name;
            ProjectSettings.Save();

            // traverse starting at output and collect everything
            var exportInfo = new ExportInfo();
            CollectChildSymbols(instance.Symbol, exportInfo);

            try
            {
                Directory.Delete(exportDir, true);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to delete export dir: {exportDir}. Exception: {e}");
                exportDir = exportDir + '_' + DateTime.Now;
            }

            Directory.CreateDirectory(exportDir);

            var playerProjectPath = GetPlayerProjectPath();

            // Copy player and dependent assemblies to export dir

            var playerPublishPath = Path.Combine(playerProjectPath, "bin", "Release", "net8.0-windows", "publish");
            
            var playerBuildPath = Path.Combine(playerProjectPath, "bin", "Release", "net8.0-windows");
            
            

            var playerExecutablePath = Path.Combine(playerBuildPath, "Player.exe");
            if (!File.Exists(playerExecutablePath))
            {
                Log.Error($"Can't find valid build in player release folder: (${playerPublishPath})");
                Log.Error("Please use your IDE to rebuild solution in release mode.");
                return;
            }

            Log.Debug("Copy core resources...");
            CopyFiles(new[]
                          {
                              playerPublishPath + "Svg.dll",
                              playerPublishPath + "Player.exe",

                              playerBuildPath + "SpoutDX.dll",
                              playerBuildPath + "Spout.dll",
                              playerBuildPath + "Processing.NDI.Lib.x64.dll",
                              playerBuildPath + "basswasapi.dll",
                              playerBuildPath + "bass.dll",
                          },
                      exportDir);

            var packages = exportInfo.UniqueSymbols.Select(x => x.SymbolPackage).Distinct();

            // Generate exported .t3 files

            var symbolExportDir = Path.Combine(exportDir, "Operators");
            if (Directory.Exists(symbolExportDir))
                Directory.Delete(symbolExportDir, true);

            Directory.CreateDirectory(symbolExportDir);

            foreach (var package in packages)
            {
                var assemblyPath = package.AssemblyInformation.Directory;
                // copy full assembly directory in the build output directly and much of below can probably be skipped
            }
            
            exportInfo.UniqueSymbols
                      .AsParallel()
                      .ForAll(symbol =>
                              {
                                  using var sw = new StreamWriter(symbolExportDir + symbol.Name + "_" + symbol.Id + SymbolPackage.SymbolExtension);
                                  using var writer = new JsonTextWriter(sw);

                                  writer.Formatting = Formatting.Indented;
                                  SymbolJson.WriteSymbol(symbol, writer);
                              });

            // Copy referenced resources
            RecursivelyCollectExportData(instance.Outputs.First(), exportInfo);
            exportInfo.PrintInfo();

            var symbolPlaybackSettings = childUi.SymbolChild.Symbol.PlaybackSettings;
            FindAudioClip(exportInfo.UniqueResourcePaths, symbolPlaybackSettings, ref errorCount);

            const string t3IconPath = @"t3-editor\images\t3.ico";
            const string hashMapSettingsShader = @"points\spatial-hash-map\hash-map-settings.hlsl";
            const string fullscreenTextureShader = @"dx11\fullscreen-texture.hlsl";
            const string resolveMultisampledDepthBufferShader = @"img\internal\resolve-multisampled-depth-buffer-cs.hlsl";
            const string brdfLookUp = @"common\images\BRDF-LookUp.png";
            const string studioSmall08Prefiltered = @"common\HDRI\studio_small_08-prefiltered.dds";

            var success = TryAddSharedResource(t3IconPath)
                          && TryAddSharedResource(hashMapSettingsShader)
                          && TryAddSharedResource(fullscreenTextureShader)
                          && TryAddSharedResource(resolveMultisampledDepthBufferShader)
                          && TryAddSharedResource(brdfLookUp)
                          && TryAddSharedResource(studioSmall08Prefiltered);

            if (!success)
            {
                Log.Error("Failed to add shared resources");
                return;
            }

            foreach (var resourcePath in exportInfo.UniqueResourcePaths)
            {
                try
                {
                    var targetPath = Path.Combine(exportDir, resourcePath);
                    var directory = Path.GetDirectoryName(targetPath);
                    Directory.CreateDirectory(directory!);
                    File.Copy(resourcePath, targetPath);
                }
                catch (Exception e)
                {
                    Log.Error($"Error exporting resource '{resourcePath}': '{e.Message}'");
                    errorCount++;
                }
            }

            if (errorCount > 0)
            {
            }
            else
            {
                Log.Debug($"Exported successfully to {exportDir}");
            }

            return;

            bool TryAddSharedResource(string sharedFile)
            {
                if (!ResourceManager.TryResolvePath(sharedFile, out var iconPath))
                {
                    Log.Error($"Can't find icon: {sharedFile}");
                    return false;
                }

                exportInfo.AddResourcePath(iconPath);
                return true;
            }
        }

        private static string GetPlayerProjectPath()
        {
            throw new NotImplementedException();
        }

        private class ExportInfo
        {
            private HashSet<Instance> CollectedInstances { get; } = new();
            public HashSet<Symbol> UniqueSymbols { get; } = new();
            public HashSet<string> UniqueResourcePaths { get; } = new();

            public bool AddInstance(Instance instance)
            {
                return CollectedInstances.Add(instance);
            }

            public void AddResourcePath(string path)
            {
                UniqueResourcePaths.Add(path);
            }

            public bool TryAddSymbol(Symbol symbol)
            {
                return UniqueSymbols.Add(symbol);
            }

            public void PrintInfo()
            {
                Log.Info($"Collected {CollectedInstances.Count} instances for export in {UniqueSymbols.Count} different symbols:");
                foreach (var resourcePath in UniqueResourcePaths)
                {
                    Log.Info($"  {resourcePath}");
                }
            }
        }

        private static void CopyFiles(IEnumerable<string> sourcePaths, string targetDir)
        {
            foreach (var path in sourcePaths)
            {
                CopyFile(path, targetDir);
            }
        }

        private static void CopyFile(string sourcePath, string targetDir)
        {
            var fi = new FileInfo(sourcePath);
            var targetPath = targetDir + Path.DirectorySeparatorChar + fi.Name;
            try
            {
                File.Copy(sourcePath, targetPath);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to copy resource file for export: {sourcePath}  {e.Message}");
            }
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
            if (slot is IInputSlot)
            {
                if (slot.IsConnected)
                {
                    RecursivelyCollectExportData(slot.GetConnection(0), exportInfo);
                }

                CheckInputForResourcePath(slot, exportInfo);
                return;
            }

            if (slot.IsConnected)
            {
                // slot is an output of an composition op
                RecursivelyCollectExportData(slot.GetConnection(0), exportInfo);
                exportInfo.AddInstance(slot.Parent);
                return;
            }

            var parent = slot.Parent;

            if (!exportInfo.AddInstance(parent))
                return; // already visited

            foreach (var input in parent.Inputs)
            {
                CheckInputForResourcePath(input, exportInfo);

                if (!input.IsConnected)
                    continue;

                if (input.IsMultiInput)
                {
                    var multiInput = (IMultiInputSlot)input;
                    foreach (var entry in multiInput.GetCollectedInputs())
                    {
                        RecursivelyCollectExportData(entry, exportInfo);
                    }
                }
                else
                {
                    RecursivelyCollectExportData(input.GetConnection(0), exportInfo);
                }
            }
        }

        private static void FindAudioClip(ICollection<string> resourcePaths, PlaybackSettings symbolPlaybackSettings, ref int errorCount)
        {
            var soundtrack = symbolPlaybackSettings?.AudioClips.SingleOrDefault(ac => ac.IsSoundtrack);
            if (soundtrack == null)
            {
                if (PlaybackUtils.TryFindingSoundtrack(out var otherSoundtrack))
                {
                    Log.Warning($"You should define soundtracks withing the exported operators. Falling back to {otherSoundtrack.FilePath} set in parent...");
                    resourcePaths.Add(otherSoundtrack.FilePath);
                    errorCount++;
                }
                else
                {
                    Log.Debug("No soundtrack defined within operator.");
                }
            }
            else
            {
                resourcePaths.Add(soundtrack.FilePath);
            }
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
                    var originalPath = stringValue.Value;

                    if (!ResourceManager.TryResolvePath(originalPath, out var resourcePath, parent.ResourceFolders))
                    {
                        Log.Warning($"File '{originalPath}' was not found in any resource folder");
                        return;
                    }

                    exportInfo.AddResourcePath(resourcePath);

                    // Copy related font textures
                    if (resourcePath.EndsWith(".fnt"))
                    {
                        exportInfo.AddResourcePath(resourcePath.Replace(".fnt", ".png"));
                    }

                    break;
                }
                case StringInputUi.UsageType.DirectoryPath:
                {
                    var originalDirectory = stringValue.Value;

                    if (!ResourceManager.TryResolveDirectory(originalDirectory, out var absoluteDirectory, parent.ResourceFolders))
                    {
                        Log.Warning($"Directory '{originalDirectory}' was not found in any resource folder");
                    }

                    Log.Debug($"Export all entries folder {absoluteDirectory}...");
                    foreach (var resourcePath in Directory.EnumerateFiles(absoluteDirectory, "*", SearchOption.AllDirectories))
                    {
                        exportInfo.AddResourcePath(resourcePath);
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