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
    
    public static bool TryLoadAssemblyFromPackageInfoFile(string filePath, [NotNullWhen(true)] out AssemblyInformation? assembly)
    {
        if (!JsonUtils.TryLoadingJson<ReleaseInfoSerialized>(filePath, out var releaseInfoSerialized))
        {
            Log.Warning($"Could not load package info from path {filePath}");
            assembly = null;
            return false;
        }

        var releaseInfo = releaseInfoSerialized.ToReleaseInfo();
        var directory = Path.GetDirectoryName(filePath);
        var assemblyFilePath = Path.Combine(directory!, releaseInfo.AssemblyFileName + ".dll");
        if (TryLoadAssemblyInformation(assemblyFilePath, out assembly, releaseInfo)) 
            return true;
        
        Log.Error($"Could not load assembly at \"{filePath}\"");
        return false;
    }
    
    public static bool TryLoadAssemblyFromDirectory(string directory, [NotNullWhen(true)] out AssemblyInformation? assembly)
    {
        var releaseInfoPath = Path.Combine(directory, PackageInfoFileName);
        if (!File.Exists(releaseInfoPath))
        {
            assembly = null;
            Log.Warning($"Could not find package info at \"{releaseInfoPath}\"");
            return false;
        }
        
        if (!JsonUtils.TryLoadingJson<ReleaseInfoSerialized>(releaseInfoPath, out var releaseInfoSerialized))
        {
            Log.Warning($"Could not load package info from path {releaseInfoPath}");
            assembly = null;
            return false;
        }
        
        var releaseInfo = releaseInfoSerialized.ToReleaseInfo();
        var assemblyFilePath = Path.Combine(directory, releaseInfo.AssemblyFileName + ".dll");
        if (!TryLoadAssemblyInformation(assemblyFilePath, out assembly, releaseInfo))
        {
            Log.Error($"Could not load assembly at \"{directory}\"");
            return false;
        }
        
        return true;
    }

    public static bool TryLoadAssemblyInformation(string path, [NotNullWhen(true)] out AssemblyInformation? info, ReleaseInfo releaseInfo)
    {
        if(!File.Exists(path))
        {
            Log.Error($"Assembly file does not exist at \"{path}\"\n{Environment.StackTrace}");
            info = null;
            return false;
        }
        
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

        var success = TryLoadAssemblyInformation(assemblyNameAndPath, out info, releaseInfo);

        if (!success)
        {
            string error = $"Failed to load package {assemblyName.FullName} from \"{path}\". Try removing the offending package and restart the application.";
            SystemUi.BlockingWindow.Instance.ShowMessageBox(error);
        }

        return success;
    }
    
    private static bool TryLoadAssemblyInformation(AssemblyNameAndPath name, [NotNullWhen(true)] out AssemblyInformation? info, ReleaseInfo releaseInfo)
    {
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
    
    public static ReleaseInfo ToReleaseInfo(this ReleaseInfoSerialized serialized)
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
            serialized.AssemblyFileName,
            serialized.HomeGuid,
            serialized.RootNamespace,
            editorVersion,
            version,
            serialized.OperatorPackages
                      .Select(x => new OperatorPackageReference(x.Identity, new Version(x.Version), x.ResourcesOnly))
                      .ToArray());
    }
}

[Serializable]
public readonly record struct OperatorPackageReferenceSerialized(string Identity, string Version, bool ResourcesOnly);

[Serializable]
// Warning: Do not change these structs, as they are used in the serialization of the operator package file and is linked to the csproj json output
// todo - add package's own Version to release info
public record ReleaseInfoSerialized(
    string AssemblyFileName,
    Guid HomeGuid,
    string RootNamespace,
    string EditorVersion,
    string Version,
    OperatorPackageReferenceSerialized[] OperatorPackages);

public sealed record OperatorPackageReference(string Identity, Version Version, bool ResourcesOnly);

public sealed record ReleaseInfo(string AssemblyFileName, Guid HomeGuid, string RootNamespace, Version EditorVersion, Version Version, OperatorPackageReference[] OperatorPackages);