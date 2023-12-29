using System;
using System.Linq;
using System.Reflection;
using T3.Core.Model;

namespace T3.Core.Compilation;

public static class CoreAssembly
{
    public static readonly Assembly Assembly = typeof(CoreAssembly).Assembly;

    public static Assembly[] GetLoadedOperatorAssemblies()
    {
        // todo - this is a hack to get the operator assemblies without referencing the editor
        // there should probably be a better way to do this
        return AppDomain.CurrentDomain.GetAssemblies()
                        .Where(assembly => assembly.Location.Contains(SymbolData.OperatorDirectoryName))
                        .Where(assembly =>
                               {
                                   return !assembly.GetReferencedAssemblies().Any(name => name.FullName.Contains("T3.Editor"));
                               })
                        .ToArray();
    }
}