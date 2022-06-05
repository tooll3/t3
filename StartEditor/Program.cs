using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core;
using T3.Core.Logging;

namespace StartEditor
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Building operators...");
            Console.WriteLine("Collecting sources...");
            var operatorAssemblySources = new List<string>();
            foreach (var sourceFile in Directory.GetFiles(@"Operators\Types\"))
            {
                if (!sourceFile.EndsWith(".cs"))
                    continue;

                Console.WriteLine($"+ {sourceFile}");
                operatorAssemblySources.Add(File.ReadAllText(sourceFile));
            }

            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\GpuQuery.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\BmFont.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\ICameraPropertiesProvider.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\AudioAnalysisResult.cs"));

            Console.WriteLine("Compiling...");
            var references = CompileSymbolsFromSource(".", operatorAssemblySources.ToArray());

            // Console.WriteLine("Starting Tooll 3");
            // try
            // {
            //     var filepath = "T3.exe";
            //     Process.Start(new ProcessStartInfo(filepath)
            //                       {
            //                           UseShellExecute = true,
            //                           WorkingDirectory = ".",
            //                       });
            // }
            // catch (Win32Exception e)
            // {
            //     Console.WriteLine(e.Message);
            // }
        }

        private static List<MetadataReference> CompileSymbolsFromSource(string exportPath, params string[] sources)
        {
            Assembly operatorsAssembly = Assembly.LoadFrom("Operators.dll");

            var referencedAssembliesNames = operatorsAssembly.GetReferencedAssemblies(); // todo: ugly
            var referencedAssemblies = new List<MetadataReference>(referencedAssembliesNames.Length);
            var coreAssembly = typeof(ResourceManager).Assembly;
            referencedAssemblies.Add(MetadataReference.CreateFromFile(coreAssembly.Location));
            referencedAssemblies.Add(MetadataReference.CreateFromFile(operatorsAssembly.Location));
            foreach (var asmName in referencedAssembliesNames)
            {
                var asm = Assembly.Load(asmName);
                if (asm != null)
                {
                    referencedAssemblies.Add(MetadataReference.CreateFromFile(asm.Location));
                }

                // In order to get dependencies of the used assemblies that are not part of T3 references itself
                var subAsmNames = asm.GetReferencedAssemblies();
                foreach (var subAsmName in subAsmNames)
                {
                    var subAsm = Assembly.Load(subAsmName);
                    referencedAssemblies.Add(MetadataReference.CreateFromFile(subAsm.Location));
                }
            }

            var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s));
            var compilation = CSharpCompilation.Create("Operators",
                                                       syntaxTrees,
                                                       referencedAssemblies.ToArray(),
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                          .WithOptimizationLevel(OptimizationLevel.Release));

            using (var dllStream = new FileStream(exportPath + Path.DirectorySeparatorChar + "Operators2.dll", FileMode.Create)) 
            // using (var pdbStream = new FileStream(exportPath + Path.DirectorySeparatorChar + "Operators.pdb", FileMode.Create))
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                Log.Info($"compilation results of 'export':");

                if (!emitResult.Success)
                {
                    foreach (var entry in emitResult.Diagnostics)
                    {
                        if (entry.WarningLevel == 0)
                            Log.Error(entry.GetMessage());
                        else
                            Log.Warning(entry.GetMessage());
                    }
                }
                else
                {
                    Log.Info($"Compilation of 'export' successful.");
                }
            }

            return referencedAssemblies;
        }
    }
}