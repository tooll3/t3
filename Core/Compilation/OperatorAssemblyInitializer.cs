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
        // todo - need to cull out the assemblies that are not operator assemblies (editor-related assemblies)
        return AppDomain.CurrentDomain.GetAssemblies()
                        .Where(x => x.Location.Contains(SymbolData.OperatorDirectoryName))
                        .ToArray();
    }
}