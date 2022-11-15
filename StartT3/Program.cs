using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Core.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core;
using T3.Core.Logging;

namespace T3.StartEditor
{
    /// <summary>
    /// Rebuilds Operators.dll 
    /// </summary>
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Log.AddWriter(new ConsoleWriter());
            Log.AddWriter(FileWriter.CreateDefault());
            
            
            Log.Debug("Building operators.dll...");
            Log.Debug(" Collecting sources...");
            var operatorAssemblySources = new List<string>();
            
            foreach (var sourceFile in Directory.GetFiles(Model.OperatorTypesFolder, "*.cs", SearchOption.AllDirectories))
            {
                Log.Debug($"+ {sourceFile}");
                operatorAssemblySources.Add(File.ReadAllText(sourceFile));
            }

            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\AudioAnalysisResult.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\BmFont.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\GpuQuery.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\ICameraPropertiesProvider.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\MidiInConnectionManager.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\OscConnectionManager.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\Interop\SpoutDX.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\Interop\Std.cs"));

            Log.Debug("Compiling...");
            var references = CompileSymbolsFromSource(".", operatorAssemblySources.ToArray());

            Log.Debug("Starting Tooll 3");
            try
            {
                var filepath = "T3.exe";
                Process.Start(new ProcessStartInfo(filepath)
                                  {
                                      UseShellExecute = true,
                                      WorkingDirectory = ".",
                                  });
            }
            catch (Win32Exception e)
            {
                Log.Debug(e.Message);
            }
        }

        private const string ReferenceOperatorAssemblyFilepath = "Operators_Reference.dll";
        private const string FinalOperatorAssemblyFilepath = "Operators.dll";
        
        
        private static List<MetadataReference> CompileSymbolsFromSource(string exportPath, params string[] sources)
        {
            Assembly asm1 = typeof(Program).Assembly;
            
            asm1.ModuleResolve += ModuleResolveEventHandler;
            
            Assembly operatorsAssembly = Assembly.LoadFrom(ReferenceOperatorAssemblyFilepath);

            var referencedAssembliesNames = operatorsAssembly.GetReferencedAssemblies(); // todo: ugly
            var referencedAssemblies = new List<MetadataReference>(referencedAssembliesNames.Length);
            var coreAssembly = typeof(ResourceManager).Assembly;
            referencedAssemblies.Add(MetadataReference.CreateFromFile(coreAssembly.Location));
            referencedAssemblies.Add(MetadataReference.CreateFromFile(operatorsAssembly.Location));
            foreach (var asmName in referencedAssembliesNames)
            {
                try
                {
                    Log.Debug($"@@@ asm: {asmName}");
                    var asm = Assembly.Load(asmName);
                    referencedAssemblies.Add(MetadataReference.CreateFromFile(asm.Location));
                    Log.Debug($"@@@ Location: {asm.Location}");

                    // In order to get dependencies of the used assemblies that are not part of T3 references itself
                    var subAsmNames = asm.GetReferencedAssemblies();
                    foreach (var subAsmName in subAsmNames)
                    {
                        var subAsm = Assembly.Load(subAsmName);
                        referencedAssemblies.Add(MetadataReference.CreateFromFile(subAsm.Location));
                    }
                }
                catch (Exception e)
                {
                    Log.Debug($"Failed: {e} {e.Message} {e.InnerException?.Message}");
                }
            }

            var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s));
            var compilation = CSharpCompilation.Create("Operators",
                                                       syntaxTrees,
                                                       referencedAssemblies.ToArray(),
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                          .WithOptimizationLevel(OptimizationLevel.Release)
                                                          .WithAllowUnsafe(true));

            using (var dllStream = new FileStream(FinalOperatorAssemblyFilepath, FileMode.Create)) 
            // using (var pdbStream = new FileStream(exportPath + Path.DirectorySeparatorChar + "Operators.pdb", FileMode.Create))
            using (var pdbStream = new MemoryStream())
            {
                Log.Debug($" emitting compilation: {compilation}");
                try
                {
                    var emitResult = compilation.Emit(dllStream);
                    
                    Log.Debug($"compilation results of 'export':" );

                    if (!emitResult.Success)
                    {
                        foreach (var entry in emitResult.Diagnostics)
                        {
                            if (entry.WarningLevel == 0)
                                Log.Debug( "ERROR:" + entry.GetMessage());
                            else
                                Log.Debug(entry.GetMessage());
                        }
                    }
                    else
                    {
                        Log.Debug($"Compilation of 'export' successful.");
                    }
                }
                catch (Exception e)
                {
                    Log.Debug("emit Failed: " + e.Message);
                }
            }
            return referencedAssemblies;
        }

        /// <summary>
        /// An attempt to define a fallback assembly folder through the call back event.
        /// Sadly this doesn't work as of yet. 
        /// </summary>
        private static Module ModuleResolveEventHandler(object sender, ResolveEventArgs e)
        {
            Log.Debug($"%%% CALLBACK {e.Name} {e.RequestingAssembly}");
            return null;
        }
    }
}