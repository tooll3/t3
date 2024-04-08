#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using T3.Core.Logging;
using T3.Serialization;

namespace T3.Core.Compilation;

public static class RuntimeAssemblies
{
    public static readonly string CorePath;
    public static readonly string CoreDirectory;
    public const string EnvironmentVariableName = "T3_ASSEMBLY_PATH";
    public static readonly Version Version;

    static RuntimeAssemblies()
    {
        var coreAssembly = typeof(RuntimeAssemblies).Assembly;
        CorePath = coreAssembly.Location;
        Version = coreAssembly.GetName().Version;

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

    public static bool TryLoadAssemblyInformation(string path, [NotNullWhen(true)] out AssemblyInformation? info, out ReleaseInfo releaseInfo)
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
            releaseInfo = null;
            return false;
        }

        var assemblyNameAndPath = new AssemblyNameAndPath()
                                      {
                                          AssemblyName = assemblyName,
                                          Path = path
                                      };

        var success = TryLoadAssemblyInformation(assemblyNameAndPath, out info, out releaseInfo);

        if (releaseInfo == null)
        {
            string error = $"Failed to load package info for {assemblyName.FullName} from \"{path}\". Try removing the offending package and restart the application.";
            SystemUi.CoreUi.Instance.ShowMessageBox(error);
            throw new Exception(error);
        }

        return success;
    }

    private static bool TryLoadAssemblyInformation(AssemblyNameAndPath name, [NotNullWhen(true)] out AssemblyInformation? info,
                                                   [NotNullWhen(true)] out ReleaseInfo? releaseInfo)
    {
        var packageInfoPath = Path.Combine(Path.GetDirectoryName(name.Path)!, PackageInfoFileName);
        if (!JsonUtils.TryLoadingJson(packageInfoPath, out releaseInfo))
        {
            Log.Warning($"Failed to load package info from path {packageInfoPath}");
        }
            
        try
        {
            var loadContext = new AssemblyLoadContext(name.AssemblyName.FullName, true);
            var assembly = loadContext.LoadFromAssemblyPath(name.Path);
            Log.Debug($"Loaded assembly {name.AssemblyName.FullName}");

            info = new AssemblyInformation(releaseInfo, name.Path, name.AssemblyName, assembly, loadContext);
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load assembly {name.AssemblyName.FullName}\n{name.Path}\n{e.Message}\n{e.StackTrace}");
            info = null;
            releaseInfo = null;
            return false;
        }
    }

    public static string ToBasicVersionString(this Version versionPrefix)
    {
        return $"{versionPrefix.Major}.{versionPrefix.Minor}.{versionPrefix.Build}";
    }

    private struct AssemblyNameAndPath
    {
        public AssemblyName AssemblyName;
        public string Path;
    }

    public const string PackageInfoFileName = "OperatorPackage.json";
}