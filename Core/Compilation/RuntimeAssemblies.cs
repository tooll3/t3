using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using T3.Core.Logging;

namespace T3.Core.Compilation;

public class RuntimeAssemblies
{
    public static readonly string CorePath;
    public static readonly string CoreDirectory;
    public const string EnvironmentVariableName = "T3_ASSEMBLY_PATH";

    static RuntimeAssemblies()
    {
        var coreAssembly = typeof(RuntimeAssemblies).Assembly;
        CorePath = coreAssembly.Location;

        CoreDirectory = Path.GetDirectoryName(CorePath);
        SetEnvironmentVariable(EnvironmentVariableName, CoreDirectory);
    }

    private static void SetEnvironmentVariable(string envVar, string envValue)
    {
        Environment.SetEnvironmentVariable(envVar, envValue, EnvironmentVariableTarget.Process);

        // todo - this will not work on linux
        var existing = Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.User);
        if (existing == envValue)
            return;
        
        Environment.SetEnvironmentVariable(envVar, envValue, EnvironmentVariableTarget.User);
    }

    public static bool TryLoadAssemblyInformation(string path, out AssemblyInformation info)
    {
        AssemblyName assemblyName;
        try
        {
            assemblyName = AssemblyName.GetAssemblyName(path);
        }
        catch (Exception e)
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
            var loadContext = new AssemblyLoadContext(name.AssemblyName.FullName, true);
            var assembly = loadContext.LoadFromAssemblyPath(name.Path);
            Log.Debug($"Loaded assembly {name.AssemblyName.FullName}");
            info = new AssemblyInformation(name.Path, name.AssemblyName, assembly, loadContext);
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