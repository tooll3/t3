using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SharpDX.Direct3D11;
using T3.Compilation;
using T3.Core;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.InputUi;

namespace T3.Gui.Graph
{
    public class PlayerExporter
    {
        public static void ExportInstance(GraphCanvas graphCanvas, SymbolChildUi childUi)
        {
            Log.Info("export");
            // collect all ops and types
            var instance = graphCanvas.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
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

                // generate Operators assembly
                var operatorAssemblySources = exportInfo.UniqueSymbols.Select(symbol =>
                                                                              {
                                                                                  var source = File.ReadAllText(symbol.SourcePath);
                                                                                  return source;
                                                                              }).ToList();
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\GpuQuery.cs"));
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\BmFont.cs"));
                operatorAssemblySources.Add(File.ReadAllText(@"Operators\Types\LFO.cs"));
                var references = OperatorUpdating.CompileSymbolsFromSource(exportDir, operatorAssemblySources.ToArray());
                
                // copy player and dependent assemblies to export dir
                var currentDir = Directory.GetCurrentDirectory();
                
                
                var buildFolder = currentDir + @"\Player\bin\Release\net5.0-windows\";
                if (!File.Exists(currentDir + @"\Player\bin\Release\net5.0-windows\Player.exe"))
                {
                    Log.Error($"Can't find valid build in player release folder: (${buildFolder})");
                    Log.Error("Please use your IDE to rebuild solution in release mode.");
                    return;
                }
                    
                var playerFileNames = new List<string>
                                          {
                                              buildFolder + "bass.dll",
                                              buildFolder + "basswasapi.dll",
                                              buildFolder + "CommandLine.dll",
                                              buildFolder + "Player.dll",
                                              buildFolder + "Player.exe",
                                              buildFolder + "SharpDX.Desktop.dll",
                                              buildFolder + "Player.deps.json",
                                              buildFolder + "Player.runtimeconfig.json",
                                              buildFolder + "Player.runtimeconfig.dev.json",
                                          };
                playerFileNames.ForEach(s => CopyFile(s, exportDir));
                
                var referencedAssemblies = references.Where(r => r.Display.Contains(currentDir))
                                                     .Select(r => r.Display)
                                                     .Distinct()
                                                     .ToArray();
                foreach (var asmPath in referencedAssemblies)
                {
                    CopyFile(asmPath, exportDir);
                }

                // generate exported .t3 files
                Json json = new Json();
                string symbolExportDir = exportDir + Path.DirectorySeparatorChar + @"Operators\Types\";
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
                
                // copy referenced resources
                Traverse(instance.Outputs.First(), exportInfo);
                exportInfo.PrintInfo();
                var resourcePaths = exportInfo.UniqueResourcePaths;
                resourcePaths.Add(ProjectSettings.Config.SoundtrackFilepath);
                resourcePaths.Add(@"projectSettings.json");
                resourcePaths.Add(@"Resources\hash-functions.hlsl");
                resourcePaths.Add(@"Resources\noise-functions.hlsl");
                resourcePaths.Add(@"Resources\particle.hlsl");
                resourcePaths.Add(@"Resources\pbr.hlsl");
                resourcePaths.Add(@"Resources\point.hlsl");
                resourcePaths.Add(@"Resources\point-light.hlsl");
                resourcePaths.Add(@"Resources\utils.hlsl");
                resourcePaths.Add(@"Resources\lib\dx11\fullscreen-texture.hlsl");
                resourcePaths.Add(@"Resources\lib\img\internal\resolve-multisampled-depth-buffer-cs.hlsl");
                resourcePaths.Add(@"Resources\lib\particles\particle-dead-list-init.hlsl");
                resourcePaths.Add(@"Resources\t3\t3.ico");
                foreach (var resourcePath in resourcePaths)
                {
                    try
                    {
                        var targetPath = exportDir + Path.DirectorySeparatorChar + resourcePath;
                    
                        var targetDir = new DirectoryInfo(targetPath).Parent.FullName;
                        if (!Directory.Exists(targetDir))
                            Directory.CreateDirectory(targetDir);
                        
                        File.Copy(resourcePath, targetPath);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error exporting resource '{resourcePath}': '{e.Message}'");
                    }
                }
            } 
            else
            {
                Log.Warning("Can only export ops with 'Texture2D' output");
            }
        }
        
        public class ExportInfo 
        {
            public HashSet<Instance> CollectedInstances { get; } = new HashSet<Instance>();
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
                Log.Info($"Collected {CollectedInstances.Count} instances for export in {UniqueSymbols.Count} different symbols");
                foreach (var resourcePath in UniqueResourcePaths)
                {
                    Log.Info(resourcePath);
                }
            }
        }

        public static void CopyFile(string sourcePath, string targetDir)
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

        public static void CollectChildSymbols(Symbol symbol, ExportInfo exportInfo)
        {
            if (!exportInfo.AddSymbol(symbol))
                return; // already visited

            foreach (var symbolChild in symbol.Children)
            {
                CollectChildSymbols(symbolChild.Symbol, exportInfo);
            }
        }

        public static void Traverse(ISlot slot, ExportInfo exportInfo)
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
        
    }
}