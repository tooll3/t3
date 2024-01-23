#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using SharpDX.Direct3D11;
using T3.Core.Compilation;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Compilation;
using T3.Editor.Gui.InputUi.SimpleInputUis;
using T3.Editor.Gui.Interaction.Timing;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

// ReSharper disable StringLiteralTypo

namespace T3.Editor.Gui.Graph
{
    public static partial class PlayerExporter
    {
        public static void ExportInstance(GraphCanvas graphCanvas, SymbolChildUi childUi)
        {
            T3Ui.Save(true);

            // Collect all ops and types
            var instance = graphCanvas.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
            Log.Info($"Exporting {instance.Symbol.Name}...");
            var errorCount = 0;

            var output = instance.Outputs.First();
            if (output == null || output.ValueType != typeof(Texture2D))
            {
                Log.Warning("Can only export ops with 'Texture2D' output");
                return;
            }

            // Update project settings
            ProjectSettings.Config.MainOperatorGuid = instance.Symbol.Id;
            ProjectSettings.Save();

            // traverse starting at output and collect everything
            var exportInfo = new ExportInfo();
            CollectChildSymbols(instance.Symbol, exportInfo);

            var playerCsProjPath = Path.Combine(RuntimeAssemblies.CorePath, "Player", "Player.csproj");
            var playerProject = new CsProjectFile(new FileInfo(playerCsProjPath));

            if (!File.Exists(playerCsProjPath))
                throw new FileNotFoundException("Player project not found", playerCsProjPath);

            var exportDir = Path.Combine(UserSettings.Config.DefaultNewProjectDirectory, "Exports", childUi.SymbolChild.ReadableName);

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

            if (!playerProject.TryCompileExternal(Compiler.BuildMode.Release, exportDir))
            {
                EditorUi.Instance.ShowMessageBox("Failed to compile player project");
                Log.Error("Failed to compile player project");
                return;
            }

            // copy assemblies into export dir
            var successInt = Convert.ToInt32(true);
            exportInfo.UniqueSymbols
                      .Select(x => x.SymbolPackage)
                      .Distinct()
                      .Select(x => x.AssemblyInformation)
                      .SelectMany(x => x.AllAssemblies)
                      .DistinctBy(x => x.FullName)
                      .AsParallel()
                      .SelectMany(assembly => assembly.GetFiles(true))
                      .ForAll(assemblyFileStream =>
                              {
                                  var fileName = Path.GetFileName(assemblyFileStream.Name);
                                  var outputPath = Path.Combine(exportDir, fileName);
                                  bool success;
                                  try
                                  {
                                      using var outputStream = File.Create(outputPath);
                                      assemblyFileStream.CopyTo(outputStream);
                                      success = true;
                                  }
                                  catch (Exception e)
                                  {
                                      Log.Error($"Failed to copy assembly: {assemblyFileStream.Name} to {outputPath}. Exception: {e}");
                                      success = false;
                                  }

                                  Interlocked.And(ref successInt, Convert.ToInt32(success));
                              });

            if (!Convert.ToBoolean(successInt))
            {
                Log.Error("Failed to copy assemblies");
                return;
            }

            var symbolExportDir = Path.Combine(exportDir, "Operators");
            if (Directory.Exists(symbolExportDir))
            {
                Directory.Delete(symbolExportDir, true);
            }

            Directory.CreateDirectory(symbolExportDir);

            exportInfo.UniqueSymbols
                      .AsParallel()
                      .ForAll(symbol =>
                              {
                                  var directory = Path.Combine(symbolExportDir, symbol.SymbolPackage.AssemblyInformation.Name);
                                  var path = Path.Combine(directory, symbol.Name + "_" + symbol.Id + SymbolPackage.SymbolExtension);
                                  Directory.CreateDirectory(directory);
                                  using var sw = new StreamWriter(path);
                                  using var writer = new JsonTextWriter(sw);

                                  writer.Formatting = Formatting.Indented;
                                  SymbolJson.WriteSymbol(symbol, writer);
                              });

            // Copy referenced resources
            RecursivelyCollectExportData(output, exportInfo);
            exportInfo.PrintInfo();

            var resourceDir = Path.Combine(exportDir, "Resources");
            var symbolPlaybackSettings = childUi.SymbolChild.Symbol.PlaybackSettings;
            var audioClipLocation = FindAudioClip(symbolPlaybackSettings, ref errorCount);
            if (audioClipLocation != null)
            {
                var audioClipPath = Path.Combine(resourceDir, Path.GetFileName(audioClipLocation));
                TryCopyFile(audioClipLocation, audioClipPath);
            }

            const string t3IconPath = @"t3-editor\images\t3.ico";
            const string hashMapSettingsShader = @"points\spatial-hash-map\hash-map-settings.hlsl";
            const string fullscreenTextureShader = @"dx11\fullscreen-texture.hlsl";
            const string resolveMultisampledDepthBufferShader = @"img\internal\resolve-multisampled-depth-buffer-cs.hlsl";
            const string brdfLookUp = @"common\images\BRDF-LookUp.png";
            const string studioSmall08Prefiltered = @"common\HDRI\studio_small_08-prefiltered.dds";

            var success = exportInfo.TryAddSharedResource(t3IconPath)
                          && exportInfo.TryAddSharedResource(hashMapSettingsShader)
                          && exportInfo.TryAddSharedResource(fullscreenTextureShader)
                          && exportInfo.TryAddSharedResource(resolveMultisampledDepthBufferShader)
                          && exportInfo.TryAddSharedResource(brdfLookUp)
                          && exportInfo.TryAddSharedResource(studioSmall08Prefiltered);

            if (!success)
            {
                Log.Error("Failed to add shared resources");
                return;
            }

            var copied = TryCopyFiles(exportInfo.UniqueResourcePaths, resourceDir);

            if (errorCount > 0 || !copied)
            {
                Log.Error("Error exporting. See log for details.");
                return;
            }

            Log.Debug($"Exported successfully to {exportDir}");
        }

        private readonly struct ResourcePath(string relativePath, string absolutePath)
        {
            public readonly string RelativePath = ResourceManager.CleanRelativePath(relativePath);
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

        private static string? FindAudioClip(PlaybackSettings symbolPlaybackSettings, ref int errorCount)
        {
            var soundtrack = symbolPlaybackSettings?.AudioClips.SingleOrDefault(ac => ac.IsSoundtrack);
            if (soundtrack == null)
            {
                if (PlaybackUtils.TryFindingSoundtrack(out var otherSoundtrack))
                {
                    Log.Warning($"You should define soundtracks withing the exported operators. Falling back to {otherSoundtrack.FilePath} set in parent...");
                    errorCount++;
                    return otherSoundtrack.FilePath;
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
                    exportInfo.TryAddSharedResource(relativePath, parent.ResourceFolders);
                    break;
                }
                case StringInputUi.UsageType.DirectoryPath:
                {
                    var relativeDirectory = stringValue.Value;

                    if (!ResourceManager.TryResolveDirectory(relativeDirectory, out var absoluteDirectory, parent.ResourceFolders))
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