using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
using T3.Editor.UiModel;

// ReSharper disable StringLiteralTypo

namespace T3.Editor.Gui.Graph
{
    public static class PlayerExporter
    {
        public static void ExportInstance(GraphCanvas graphCanvas, SymbolChildUi childUi)
        {
            // Collect all ops and types
            var instance = graphCanvas.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
            Log.Info($"Exporting {instance.Symbol.Name}...");
            var errorCount = 0;

            if (instance.Outputs.Count >= 1 && instance.Outputs.First().ValueType == typeof(Texture2D))
            {
                // Update project settings
                ProjectSettings.Config.MainOperatorName = instance.Symbol.Name;
                ProjectSettings.Save();

                // traverse starting at output and collect everything
                var exportInfo = new ExportInfo();
                CollectChildSymbols(instance.Symbol, exportInfo);

                const string exportDir = "Export";
                try
                {
                    Directory.Delete(exportDir, true);
                }
                catch (Exception)
                {
                    // ignored
                }

                Directory.CreateDirectory(exportDir);

                // Generate Operators assembly
                var operatorAssemblySources
                    = exportInfo
                     .UniqueSymbols
                     .Select(symbol =>
                             {
                                 var filePathForSymbol = SymbolData.BuildFilepathForSymbol(symbol,
                                                                                           SymbolData.SourceExtension);

                                 if (!File.Exists(filePathForSymbol))
                                 {
                                     Log.Warning($"Can't find source file {filePathForSymbol}");
                                     return string.Empty;
                                 }

                                 var source = File.ReadAllText(filePathForSymbol);
                                 return source;
                             }).ToList();

                foreach (var file in Directory.GetFiles(@"Operators\Utils\", "*.cs", SearchOption.AllDirectories))
                {
                    operatorAssemblySources.Add(File.ReadAllText(file));
                }

                // Copy player and dependent assemblies to export dir
                var currentDir = Directory.GetCurrentDirectory();

                var playerPublishPath = currentDir + @"\Player\bin\Release\net6.0-windows\publish\";
                var playerBuildPath = currentDir + @"\Player\bin\Release\net6.0-windows\";

                if (!File.Exists(currentDir + @"\Player\bin\Release\net6.0-windows\publish\Player.exe"))
                {
                    Log.Error($"Can't find valid build in player release folder: (${playerPublishPath})");
                    Log.Error("Please use your IDE to rebuild solution in release mode.");
                    return;
                }

                Log.Debug("Copy player resources...");
                CopyFiles(new[]
                              {
                                  playerPublishPath + "Svg.dll",
                                  playerPublishPath + "Player.exe",

                                  playerBuildPath + "SpoutDX.dll",
                                  playerBuildPath + "Spout.dll",
                                  playerBuildPath + "Processing.NDI.Lib.x64.dll",
                                  playerBuildPath + "basswasapi.dll",
                                  playerBuildPath + "bass.dll",
                                  playerBuildPath + "AbletonLinkDLL.dll",
                                  playerBuildPath + "AbletonLink.dll",
                                  playerBuildPath + "AbletonLink.deps.json",
                              },
                          exportDir);

                Log.Debug("Compiling Operators.dll...");
                var references = CompileSymbolsFromSource(exportDir, operatorAssemblySources.ToArray());

                if (!Program.IsStandAlone)
                {
                    Log.Debug("Copy dependencies referenced in Operators.dll...");
                    var referencedAssemblies = references.Where(assembly => assembly.Display != null && assembly.Display.Contains(currentDir))
                                                         .Select(r => r.Display)
                                                         .Distinct()
                                                         .ToArray();
                    CopyFiles(referencedAssemblies,
                              exportDir);
                }

                // Generate exported .t3 files

                var symbolExportDir = Path.Combine(exportDir, SymbolData.OperatorTypesFolder);
                if (Directory.Exists(symbolExportDir))
                    Directory.Delete(symbolExportDir, true);

                Directory.CreateDirectory(symbolExportDir);
                foreach (var symbol in exportInfo.UniqueSymbols)
                {
                    using var sw = new StreamWriter(symbolExportDir + symbol.Name + "_" + symbol.Id + ".t3");
                    using var writer = new JsonTextWriter(sw);
                    
                    writer.Formatting = Formatting.Indented;
                    SymbolJson.WriteSymbol(symbol, writer);
                }

                // Copy referenced resources
                RecursivelyCollectExportData(instance.Outputs.First(), exportInfo);
                exportInfo.PrintInfo();
                var resourcePaths = exportInfo.UniqueResourcePaths;

                {
                    var symbolPlaybackSettings = childUi.SymbolChild.Symbol.PlaybackSettings;

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

                resourcePaths.Add(@"projectSettings.json");

                resourcePaths.UnionWith(Directory.GetFiles(@"Resources\lib\shared\"));

                resourcePaths.Add(@"Resources\lib\points\spatial-hash-map\hash-map-settings.hlsl");

                resourcePaths.Add(@"Resources\lib\dx11\fullscreen-texture.hlsl");
                resourcePaths.Add(@"Resources\lib\img\internal\resolve-multisampled-depth-buffer-cs.hlsl");
                resourcePaths.Add(@"Resources\lib\cs\CombineGltfChannels-cs.hlsl");

                resourcePaths.Add(@"Resources\common\images\BRDF-LookUp.png");
                resourcePaths.Add(@"Resources\common\images\BRDF-LookUp.dds");
                resourcePaths.Add(@"Resources\common\HDRI\studio_small_08-prefiltered.dds");

                resourcePaths.Add(@"Resources\t3-editor\images\t3.ico");
                foreach (var resourcePath in resourcePaths)
                {
                    try
                    {
                        if (!resourcePath.Contains("\\") && resourcePath.StartsWith("Resources"))
                        {
                            Log.Warning($" {resourcePath} can't be used for exporting because it's not located in Resources/ folder.");
                            errorCount++;
                            continue;
                        }

                        var targetPath = exportDir + Path.DirectorySeparatorChar + resourcePath;

                        var targetDirectoryInfo = new DirectoryInfo(targetPath);
                        if (targetDirectoryInfo.Parent == null)
                        {
                            Log.Warning($"Can't access parent of {targetPath}");
                            continue;
                        }
                        
                        var targetDir = targetDirectoryInfo.Parent.FullName;
                        if (!Directory.Exists(targetDir))
                            Directory.CreateDirectory(targetDir);

                        File.Copy(resourcePath, targetPath);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error exporting resource '{resourcePath}': '{e.Message}'");
                        errorCount++;
                    }
                }
            }
            else
            {
                Log.Warning("Can only export ops with 'Texture2D' output");
                errorCount++;
            }

            if (errorCount > 0)
            {
                Log.Error($"{errorCount} problem{(errorCount > 1 ? "s" : string.Empty)}. Export might be broken.");
            }
            else
            {
                Log.Debug("Done. Please check Export/ directory.");
            }
        }

        private class ExportInfo
        {
            private HashSet<Instance> CollectedInstances { get; } = new();
            public HashSet<Symbol> UniqueSymbols { get; } = new();
            public HashSet<string> UniqueResourcePaths { get; } = new();

            public bool AddInstance(Instance instance)
            {
                if (CollectedInstances.Contains(instance))
                    return false;

                CollectedInstances.Add(instance);
                return true;
            }

            public void AddResourcePath(string path)
            {
                UniqueResourcePaths.Add(path);
            }

            public bool AddSymbol(Symbol symbol)
            {
                if (UniqueSymbols.Contains(symbol))
                    return false;

                UniqueSymbols.Add(symbol);
                return true;
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
            if (!exportInfo.AddSymbol(symbol))
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
            }
            else if (slot.IsConnected)
            {
                // slot is an output of an composition op
                RecursivelyCollectExportData(slot.GetConnection(0), exportInfo);
                exportInfo.AddInstance(slot.Parent);
            }
            else
            {
                Instance parent = slot.Parent;
                // Log.Info(parent.Symbol.Name);
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
                    var resourcePath = stringValue.Value;
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
                    var resourceDirectory = stringValue.Value;
                    if (!Directory.Exists(resourceDirectory))
                        break;

                    if (!resourceDirectory.StartsWith("Resources"))
                    {
                        Log.Debug($"skipping folder {resourceDirectory} because not in Resources/ folder...");
                        break;
                    }

                    Log.Debug($"Export all entries folder {resourceDirectory}...");
                    foreach (var resourcePath in Directory.GetFiles(resourceDirectory))
                    {
                        exportInfo.AddResourcePath(resourcePath);
                    }

                    break;
                }
            }
        }

        private static List<MetadataReference> CompileSymbolsFromSource(string exportPath, params string[] sources)
        {
            var operatorsAssembly = ResourceManager.Instance().OperatorsAssembly;
            var referencedAssembliesNames = operatorsAssembly.GetReferencedAssemblies(); // todo: ugly
            var referencedAssemblies = new List<MetadataReference>(referencedAssembliesNames.Length);
            var coreAssembly = typeof(ResourceManager).Assembly;
            referencedAssemblies.Add(MetadataReference.CreateFromFile(coreAssembly.Location));
            foreach (var asmName in referencedAssembliesNames)
            {
                var asm = Assembly.Load(asmName);
                referencedAssemblies.Add(MetadataReference.CreateFromFile(asm.Location));
                Log.Debug($"Loaded from {asm} {asm.Location}");

                // In order to get dependencies of the used assemblies that are not part of T3 references itself
                var subAsmNames = asm.GetReferencedAssemblies();
                foreach (var subAsmName in subAsmNames)
                {
                    var subAsm = Assembly.Load(subAsmName);
                    Log.Debug($"  Loaded SUB from {subAsm} {subAsm.Location}");

                    referencedAssemblies.Add(MetadataReference.CreateFromFile(subAsm.Location));
                }
            }

            var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s));
            var compilation = CSharpCompilation.Create("Operators",
                                                       syntaxTrees,
                                                       referencedAssemblies.ToArray(),
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                          .WithOptimizationLevel(OptimizationLevel.Release)
                                                          .WithAllowUnsafe(true));

            using var dllStream = new FileStream(Path.Combine(exportPath, "Operators.dll"), FileMode.Create);
            using var pdbStream = new MemoryStream();

            var emitResult = compilation.Emit(dllStream, pdbStream);
            Log.Info($"compilation results of 'export':");

            if (!emitResult.Success)
            {
                Log.Debug("Failed!");

                Log.Debug("Source codes:");
                foreach (var source in sources)
                {
                    Log.Debug(source);
                    Log.Debug("~~~~~~~~~~~~~~~~~~");
                }

                Log.Debug("Messages");
                foreach (var entry in emitResult.Diagnostics)
                {
                    if (entry.WarningLevel == 0)
                        Log.Error("ERROR:" + entry.GetMessage());
                    else
                        Log.Warning(entry.GetMessage());
                }
            }
            else
            {
                Log.Info($"Compilation of 'export' successful.");
            }

            return referencedAssemblies;
        }
    }
}