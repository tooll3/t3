using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;
using SharpDX.Direct3D11;
using T3.Compilation;
using T3.Core;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using t3.Gui.Audio;
using T3.Gui.InputUi;
using t3.Gui.InputUi.SimpleInputUis;
using T3.Operators.Types.Id_92b18d2b_1022_488f_ab8e_a4dcca346a23;

namespace T3.Gui.Graph
{
    public class PlayerExporter
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

                string exportDir = "Export";
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
                var operatorAssemblySources = exportInfo.UniqueSymbols.Select(symbol =>
                                                                              {
                                                                                  var source = File.ReadAllText(Model.BuildFilepathForSymbol(symbol, Model.SourceExtension));
                                                                                  return source;
                                                                              }).ToList();
                
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\GpuQuery.cs"));
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\BmFont.cs"));
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\ICameraPropertiesProvider.cs"));
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\AudioAnalysisResult.cs"));
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\MidiInConnectionManager.cs"));

                // Copy player and dependent assemblies to export dir
                var currentDir = Directory.GetCurrentDirectory();

                var playerBuildPath = currentDir + @"\Player\bin\Release\net6.0-windows\";
                var operatorDependenciesPath = Program.IsStandAlone
                                                   ? @".\"
                                                   : @"T3\bin\Release\net6.0-windows\";

                if (!File.Exists(currentDir + @"\Player\bin\Release\net6.0-windows\Player.exe"))
                {
                    Log.Error($"Can't find valid build in player release folder: (${playerBuildPath})");
                    Log.Error("Please use your IDE to rebuild solution in release mode.");
                    return;
                }

                Log.Debug("Copy player resources...");
                CopyFiles(new[]
                              {
                                  playerBuildPath + "bass.dll",
                                  playerBuildPath + "basswasapi.dll",
                                  playerBuildPath + "CommandLine.dll",
                                  playerBuildPath + "Player.dll",
                                  playerBuildPath + "Player.exe",
                                  playerBuildPath + "Player.deps.json",
                                  playerBuildPath + "Player.runtimeconfig.json",
                                  playerBuildPath + "Player.runtimeconfig.dev.json",
                                  
                                  // FIXME: These dlls should be references as Operators dependencies but aren't found there
                                  playerBuildPath + "SharpDX.Desktop.dll", 
                              },
                          exportDir);

                // NOTE: This is fallback because the Operators.dll compiled for stand alone runner
                // does not contain all assembly references. So we add these here manually
                if (Program.IsStandAlone)
                {
                    Log.Debug("Copy operator dependencies");
                    CopyFiles(new[]
                                  {
                                      playerBuildPath + "Rug.OSC.dll",
                                      operatorDependenciesPath + "Core.dll",
                                      operatorDependenciesPath + "DdsImport.dll",
                                      operatorDependenciesPath + "ManagedBass.Wasapi.dll",
                                      operatorDependenciesPath + "ManagedBass.dll",
                                      operatorDependenciesPath + "Newtonsoft.Json.dll",
                                      operatorDependenciesPath + "Unsplasharp.dll",
                                      operatorDependenciesPath + "SharpDX.Mathematics.dll",
                                      operatorDependenciesPath + "SharpDX.Direct3D11.dll",
                                      operatorDependenciesPath + "SharpDX.Direct2D1.dll",
                                      operatorDependenciesPath + "SharpDX.DXGI.dll",
                                      operatorDependenciesPath + "SharpDX.D3DCompiler.dll",
                                      operatorDependenciesPath + "SharpDX.dll",
                                      operatorDependenciesPath + "NAudio.Midi.dll",
                                      operatorDependenciesPath + "NAudio.Core.dll",
                                      operatorDependenciesPath + "Svg.dll",
                                      operatorDependenciesPath + "Fizzler.dll",
                                      operatorDependenciesPath + "SharpDX.MediaFoundation.dll",
                                  },
                              exportDir);
                }
                
                Log.Debug("Compiling Operators.dll...");
                var references = CompileSymbolsFromSource(exportDir, operatorAssemblySources.ToArray());
                
                if(!Program.IsStandAlone)
                {
                    Log.Debug("Copy dependencies referenced in Operators.dll...");
                    var referencedAssemblies = references.Where(r => r.Display.Contains(currentDir))
                                                         .Select(r => r.Display)
                                                         .Distinct()
                                                         .ToArray();
                    CopyFiles(referencedAssemblies,
                              exportDir);
                }

                // Generate exported .t3 files
                var json = new SymbolJson();
                
                var symbolExportDir = Path.Combine(exportDir, Model.OperatorTypesFolder);
                if (Directory.Exists(symbolExportDir))
                    Directory.Delete(symbolExportDir, true);

                Directory.CreateDirectory(symbolExportDir);
                foreach (var symbol in exportInfo.UniqueSymbols)
                {
                    using (var sw = new StreamWriter(symbolExportDir + symbol.Name + "_" + symbol.Id + ".t3"))
                    using (var writer = new JsonTextWriter(sw))
                    {
                        json.Writer = writer;
                        json.Writer.Formatting = Formatting.Indented;
                        json.WriteSymbol(symbol);
                    }
                }

                // Copy referenced resources
                Traverse(instance.Outputs.First(), exportInfo);
                exportInfo.PrintInfo();
                var resourcePaths = exportInfo.UniqueResourcePaths;

                {
                    var soundtrack = childUi.SymbolChild.Symbol.AudioClips.SingleOrDefault(ac => ac.IsSoundtrack);
                    if (soundtrack == null)
                    {
                        if (SoundtrackUtils.TryFindingSoundtrack(instance, out var otherSoundtrack))
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

                resourcePaths.Add(@"Resources\lib\shared\bias.hlsl");
                resourcePaths.Add(@"Resources\lib\shared\hash-functions.hlsl");
                resourcePaths.Add(@"Resources\lib\points\spatial-hash-map\hash-map-settings.hlsl");

                resourcePaths.Add(@"Resources\lib\shared\noise-functions.hlsl");
                resourcePaths.Add(@"Resources\lib\shared\particle.hlsl");
                resourcePaths.Add(@"Resources\lib\shared\pbr.hlsl");
                resourcePaths.Add(@"Resources\lib\shared\point.hlsl");
                resourcePaths.Add(@"Resources\lib\shared\point-light.hlsl");

                resourcePaths.Add(@"Resources\lib\dx11\fullscreen-texture.hlsl");
                resourcePaths.Add(@"Resources\lib\img\internal\resolve-multisampled-depth-buffer-cs.hlsl");

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

                        var targetDir = new DirectoryInfo(targetPath).Parent.FullName;
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
            private HashSet<Instance> CollectedInstances { get; } = new HashSet<Instance>();
            public HashSet<Symbol> UniqueSymbols { get; } = new HashSet<Symbol>();
            public HashSet<string> UniqueResourcePaths { get; } = new HashSet<string>();

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

        private static void Traverse(ISlot slot, ExportInfo exportInfo)
        {
            if (slot is IInputSlot)
            {
                if (slot.IsConnected)
                {
                    Traverse(slot.GetConnection(0), exportInfo);
                }

                CheckInputForResourcePath(slot, exportInfo);
            }
            else if (slot.IsConnected)
            {
                // slot is an output of an composition op
                Traverse(slot.GetConnection(0), exportInfo);
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

                    if (input.IsConnected)
                    {
                        if (input.IsMultiInput)
                        {
                            var multiInput = (IMultiInputSlot)input;
                            foreach (var entry in multiInput.GetCollectedInputs())
                            {
                                Traverse(entry, exportInfo);
                            }
                        }
                        else
                        {
                            Traverse(input.GetConnection(0), exportInfo);
                        }
                    }
                }
            }
        }

        private static void CheckInputForResourcePath(ISlot inputSlot, ExportInfo exportInfo)
        {
            var parent = inputSlot.Parent;
            var inputUi = SymbolUiRegistry.Entries[parent.Symbol.Id].InputUis[inputSlot.Id];
            if (inputUi is StringInputUi stringInputUi && stringInputUi.Usage == StringInputUi.UsageType.FilePath)
            {
                var compositionSymbol = parent.Parent.Symbol;
                var parentSymbolChild = compositionSymbol.Children.Single(child => child.Id == parent.SymbolChildId);
                var value = parentSymbolChild.InputValues[inputSlot.Id].Value;
                if (value is InputValue<string> stringValue)
                {
                    var resourcePath = stringValue.Value;
                    exportInfo.AddResourcePath(resourcePath);
                    if (resourcePath.EndsWith(".fnt"))
                    {
                        exportInfo.AddResourcePath(resourcePath.Replace(".fnt", ".png"));
                    }
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
            // referencedAssemblies.Add(MetadataReference.CreateFromFile(operatorsAssembly.Location));
            foreach (var asmName in referencedAssembliesNames)
            {
                var asm = Assembly.Load(asmName);
                referencedAssemblies.Add(MetadataReference.CreateFromFile(asm.Location));
                Log.Debug($"Loaded from {asm} {asm.Location}");

                // in order to get dependencies of the used assemblies that are not part of T3 references itself
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
                                                          .WithOptimizationLevel(OptimizationLevel.Release));

            using var dllStream = new FileStream( Path.Combine(exportPath, "Operators.dll"), FileMode.Create);
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