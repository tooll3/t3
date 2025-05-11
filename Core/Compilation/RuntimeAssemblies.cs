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

/// <summary>
/// This is a common entrypoint for package loading and versioning.
/// It also sets an important environment variable for linking external projects with the installed T3 editor assemblies.
/// Many things here can probably be moved elsewhere, but it's a result of a fairly iterative process growing this compilation system.
/// </summary>
public static class RuntimeAssemblies
{
    public const string EnvironmentVariableName = "T3_ASSEMBLY_PATH";
    private static readonly Assembly CoreAssembly = typeof(RuntimeAssemblies).Assembly;
    public static readonly string CorePath = CoreAssembly.Location;
    public static readonly string CoreDirectory = Path.GetDirectoryName(CorePath)!;
    public static readonly Version Version = CoreAssembly.GetName().Version!;

    public const string NetVersion = "9.0";
    
    static RuntimeAssemblies()
    {
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

    public static string ToBasicVersionString(this Version versionPrefix)
    {
        return $"{versionPrefix.Major}.{versionPrefix.Minor}.{versionPrefix.Build}";
    }

    public static bool Matches(this Version version, Version? other)
    {
        if(other == null)
            return false;
        
        // check Major, Minor, and Build
        return version.Major == other.Major 
               && version.Minor == other.Minor 
               && version.Build == other.Build;
    }


    public const string PackageInfoFileName = "OperatorPackage.json";
    
    public static ReleaseInfo ToReleaseInfo(this ReleaseInfoSerialized serialized)
    {
        if (!Version.TryParse(serialized.EditorVersion, out var editorVersion))
        {
            editorVersion = new Version(1, 0, 0);
            Log.Error($"{serialized.RootNamespace}: Failed to parse editor version \"{serialized.EditorVersion}\" from package info. Setting to {editorVersion}");
        }
        
        if (!Version.TryParse(serialized.Version, out var version))
        {
            version = new Version(1, 0, 0);
            Log.Error($"{serialized.RootNamespace}: Failed to parse package version \"{serialized.Version}\" from package info. Setting to {version}");
        }
        
        return new ReleaseInfo(
            serialized.AssemblyFileName,
            serialized.HomeGuid,
            serialized.RootNamespace,
            editorVersion,
            version,
            serialized.IsEditorOnly,
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
    bool IsEditorOnly,
    OperatorPackageReferenceSerialized[] OperatorPackages);

// Identity must equal that package's root namespace
public sealed record OperatorPackageReference(string Identity, Version Version, bool ResourcesOnly);

public sealed record ReleaseInfo(string AssemblyFileName, Guid HomeGuid, string RootNamespace, Version EditorVersion, Version Version, bool IsEditorOnly, OperatorPackageReference[] OperatorPackages);

internal struct AssemblyNameAndPath
{
    public AssemblyName AssemblyName;
    public string Path;
}
