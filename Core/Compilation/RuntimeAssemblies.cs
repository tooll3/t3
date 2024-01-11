using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using T3.Core.Logging;

namespace T3.Core.Compilation;

public class RuntimeAssemblies
{
    public static readonly AssemblyInformation Core;
    public static IReadOnlyList<AssemblyInformation> AllAssemblies { get; private set; } = Array.Empty<AssemblyInformation>();

    static RuntimeAssemblies()
    {
        var coreAssembly = typeof(RuntimeAssemblies).Assembly;
        var coreAssemblyName = coreAssembly.GetName();
        var path = coreAssembly.Location;
        Core = new AssemblyInformation(path, coreAssemblyName, coreAssembly);
    }
    
    public static bool TryLoadAssemblyInformation(string path, out AssemblyInformation info)
    {
        AssemblyName assemblyName;
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(path);
        }
        catch(Exception e)
        {
            Log.Error($"Failed to get assembly name for {path}\n{e.Message}\n{e.StackTrace}");
            info = null;
            return false;
        }

        var assemblyNameAndPath = new AssemblyNameAndPath()
                                      {
                                          AssemblyName = assemblyName,
                                          Path = path
                                      };
        
        return TryLoadAssemblyInformation(assemblyNameAndPath, out info);
    }

    private static bool TryLoadAssemblyInformation(AssemblyNameAndPath name, out AssemblyInformation info)
    {
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(name.Path);
            Log.Debug($"Loaded assembly {name.AssemblyName.FullName}");
            info = new AssemblyInformation(name.Path, name.AssemblyName, assembly);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load assembly {name.AssemblyName.FullName}\n{name.Path}\n{e.Message}\n{e.StackTrace}");
            info = null;
            return false;
        }
    }

    private struct AssemblyNameAndPath
    {
        public AssemblyName AssemblyName;
        public string Path;
    }
}