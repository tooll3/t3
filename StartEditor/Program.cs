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

            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\AudioAnalysisResult.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\BmFont.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\GpuQuery.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\ICameraPropertiesProvider.cs"));
            operatorAssemblySources.Add(File.ReadAllText(@"Operators\Utils\MidiConnectionManager.cs"));

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
            Assembly asm1 = typeof(Program).Assembly;
            
            asm1.ModuleResolve += ModuleResolveEventHandler;
            
            Assembly operatorsAssembly = Assembly.LoadFrom("Operators_Sources.dll");

            var referencedAssembliesNames = operatorsAssembly.GetReferencedAssemblies(); // todo: ugly
            var referencedAssemblies = new List<MetadataReference>(referencedAssembliesNames.Length);
            var coreAssembly = typeof(ResourceManager).Assembly;
            referencedAssemblies.Add(MetadataReference.CreateFromFile(coreAssembly.Location));
            referencedAssemblies.Add(MetadataReference.CreateFromFile(operatorsAssembly.Location));
            foreach (var asmName in referencedAssembliesNames)
            {
                try
                {
                    Console.WriteLine($"@@@ asm: {asmName}");
                    var asm = Assembly.Load(asmName);
                    referencedAssemblies.Add(MetadataReference.CreateFromFile(asm.Location));
                    Console.WriteLine($"@@@ Location: {asm.Location}");

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
                    Console.WriteLine($"Failed: {e} {e.Message} {e.InnerException?.Message}");
                }
            }

            var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s));
            var compilation = CSharpCompilation.Create("Operators",
                                                       syntaxTrees,
                                                       referencedAssemblies.ToArray(),
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                                          .WithOptimizationLevel(OptimizationLevel.Release));

            using (var dllStream = new FileStream("Operators_Build.dll", FileMode.Create)) 
            // using (var pdbStream = new FileStream(exportPath + Path.DirectorySeparatorChar + "Operators.pdb", FileMode.Create))
            using (var pdbStream = new MemoryStream())
            {
                Console.WriteLine($" emitting compilation: {compilation}");
                try
                {
                    var emitResult = compilation.Emit(dllStream);
                    
                    Console.WriteLine($"compilation results of 'export':" );

                    if (!emitResult.Success)
                    {
                        foreach (var entry in emitResult.Diagnostics)
                        {
                            if (entry.WarningLevel == 0)
                                Console.WriteLine( "ERROR:" + entry.GetMessage());
                            else
                                Console.WriteLine(entry.GetMessage());
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Compilation of 'export' successful.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("emit Failed: " + e.Message);
                }
            }

            return referencedAssemblies;
        }

        private static Module ModuleResolveEventHandler(object sender, ResolveEventArgs e)
        {
            Console.WriteLine($"%%% CALLBACK {e.Name} {e.RequestingAssembly}");
            return null;
        }
    }
}