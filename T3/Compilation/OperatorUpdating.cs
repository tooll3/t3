using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using T3.Core;
using T3.Core.Logging;

namespace T3.Compilation
{
    public static class OperatorUpdating
    {
        public static bool Update(OperatorResource resource, string path)
        {
            Log.Info($"Operator source '{path}' changed.");
            Log.Info($"Actual thread Id {Thread.CurrentThread.ManagedThreadId}");
        
            string source;
            try
            {
                source = File.ReadAllText(path);
            }
            catch (Exception e)
            {
                Log.Error($"Error opening file '{path}");
                Log.Error(e.Message);
                return false;
            }
        
            if (string.IsNullOrEmpty(source))
            {
                Log.Info("Source was empty, skip compilation.");
                return false;
            }
        
            var referencedAssembliesNames = ResourceManager.Instance().OperatorsAssembly.GetReferencedAssemblies(); // todo: ugly
            var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var referencedAssemblies = new List<MetadataReference>(referencedAssembliesNames.Length);
            var coreAssembly = typeof(ResourceManager).Assembly;
            referencedAssemblies.Add(MetadataReference.CreateFromFile(coreAssembly.Location));
            foreach (var asmName in referencedAssembliesNames)
            {
                var asm = appDomainAssemblies.SingleOrDefault(assembly => assembly.GetName().Name == asmName.Name);
                if (asm != null)
                {
                    referencedAssemblies.Add(MetadataReference.CreateFromFile(asm.Location));
                }
            }
        
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CSharpCompilation.Create("Operators",
                                                       new[] { syntaxTree },
                                                       referencedAssemblies.ToArray(),
                                                       new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);
                Log.Info($"compilation results of '{path}':");
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
                    Log.Info("successful");
                    var newAssembly = Assembly.Load(dllStream.GetBuffer());
                    if (newAssembly.ExportedTypes.Any())
                    {
                        resource.OperatorAssembly = newAssembly;
                        resource.Updated = true;
                    }
                    else
                    {
                        Log.Error("New compiled Assembly had no exported type.");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}