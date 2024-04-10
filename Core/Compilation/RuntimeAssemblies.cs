#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
            SystemUi.BlockingWindow.Instance.Show(error);
            throw new Exception(error);
        }

        return success;
    }

    private static bool TryLoadAssemblyInformation(AssemblyNameAndPath name, [NotNullWhen(true)] out AssemblyInformation? info,
                                                   [NotNullWhen(true)] out ReleaseInfo? releaseInfo)
    {
        var packageInfoPath = Path.Combine(Path.GetDirectoryName(name.Path)!, PackageInfoFileName);
        if (!JsonUtils.TryLoadingJson(packageInfoPath, out ReleaseInfoSerialized? json))
        {
            Log.Warning($"Failed to load package info from path {packageInfoPath}");
            releaseInfo = null;
        }
        else
        {
            releaseInfo = json.ToReleaseInfo();
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


    [Serializable]
    private readonly record struct OperatorPackageReferenceSerialized(string Identity, string Version, bool ResourcesOnly);
    [Serializable]
    // Warning: Do not change these structs, as they are used in the serialization of the operator package file and is linked to the csproj json output
    // todo - add package's own Version to release info
    private record ReleaseInfoSerialized(
        Guid HomeGuid,
        string RootNamespace,
        string EditorVersion,
        string Version,
        OperatorPackageReferenceSerialized[] OperatorPackages);
    
    private static ReleaseInfo ToReleaseInfo(this ReleaseInfoSerialized serialized)
    {
        if (!Version.TryParse(serialized.EditorVersion, out var editorVersion))
        {
            editorVersion = new Version(1, 0, 0);
            Log.Warning($"{serialized.RootNamespace}: Failed to parse editor version \"{serialized.EditorVersion}\" from package info. Setting to {editorVersion}");
        }
        
        if (!Version.TryParse(serialized.Version, out var version))
        {
            version = new Version(1, 0, 0);
            Log.Warning($"{serialized.RootNamespace}: Failed to parse package version \"{serialized.Version}\" from package info. Setting to {version}");
        }
        
        return new ReleaseInfo(
            serialized.HomeGuid,
            serialized.RootNamespace,
            editorVersion,
            version,
            serialized.OperatorPackages
                      .Select(x => new OperatorPackageReference(x.Identity, new Version(x.Version), x.ResourcesOnly))
                      .ToArray());
    }
}

public sealed record OperatorPackageReference(string Identity, Version Version, bool ResourcesOnly);
public sealed record ReleaseInfo(Guid HomeGuid, string RootNamespace, Version EditorVersion, Version Version, OperatorPackageReference[] OperatorPackages);