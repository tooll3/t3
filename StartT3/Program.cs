using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Resource;

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

            var sourceFilePaths = new HashSet<string>();

            sourceFilePaths.UnionWith(Directory.GetFiles(SymbolData.OperatorTypesFolder, "*.cs", SearchOption.AllDirectories).ToArray().ToArray());
            sourceFilePaths.UnionWith(Directory.GetFiles(@"Operators\Utils\", "*.cs", SearchOption.AllDirectories));
            sourceFilePaths.Add(@"Operators\Types\lib\dx11\draw\PickBlendMode.cs");

            var operatorAssemblySources = new List<string>();
            foreach (var filepath in sourceFilePaths)
            {
                Log.Debug($"+ {filepath}");
                operatorAssemblySources.Add(File.ReadAllText(filepath));
            }

            Log.Debug("Compiling...");
            var references = CompileSymbolsFromSource(".", operatorAssemblySources.ToArray());

            Log.Debug("Starting Tooll 3");
            try
            {
                var filepath = "T3Editor.exe";
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
            AppDomain.CurrentDomain.AssemblyResolve += ModuleResolveEventHandler;

            var operatorsAssembly = Assembly.LoadFrom(ReferenceOperatorAssemblyFilepath);

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

                    Log.Debug($"compilation results of 'export':");

                    if (!emitResult.Success)
                    {
                        foreach (var entry in emitResult.Diagnostics)
                        {
                            if (entry.WarningLevel == 0)
                                Log.Debug("ERROR:" + entry.GetMessage());
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

        
        private static Assembly ModuleResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Log.Debug($"AssemblyResolveCallback for {args.Name}");
            if (args.Name.StartsWith("NDI", StringComparison.InvariantCultureIgnoreCase))
            {
                var xxx = new AssemblyName(args.Name);
                
                var filePath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), xxx.Name + ".dll");
                Log.Debug($"> Try loading {filePath}");
                var loadNdiAssemblyResult = Assembly.LoadFrom(filePath);
                Log.Debug("> result " + loadNdiAssemblyResult);
                return loadNdiAssemblyResult;
            }

            return null;
        }
    }
}