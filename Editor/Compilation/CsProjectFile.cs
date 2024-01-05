using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Logging;

namespace T3.Editor.Compilation;

public class CsProjectFile
{
    public string FullPath { get; }
    public string Directory { get; }
    public string FileName { get; }
    public string Name { get; }
    public string Contents { get; }
    public string RootNamespace { get; }
    public AssemblyInformation Assembly { get; }
    public IReadOnlyList<DependencyInfo> Dependencies => _dependencies;
    private readonly List<DependencyInfo> _dependencies;

    public CsProjectFile(FileInfo file, AssemblyInformation assembly)
    {
        FullPath = file.FullName;
        Contents = File.ReadAllText(file.FullName);
        Assembly = assembly;

        Directory = Path.GetDirectoryName(FullPath)!;
        FileName = Path.GetFileName(FullPath);
        Name = Path.GetFileNameWithoutExtension(FullPath);

        _dependencies = ParseDependencies(Contents);
        RootNamespace = GetRootNamespace(Contents);
    }

    private string GetRootNamespace(string contents)
    {
        const string beginNamespaceTag = "<RootNamespace>";
        const string endNamespaceTag = "</RootNamespace>";
        var start = contents.IndexOf(beginNamespaceTag, StringComparison.Ordinal) + beginNamespaceTag.Length;
        var end = contents.IndexOf(endNamespaceTag, StringComparison.Ordinal);
        if(start == -1 || end == -1)
        {
            throw new Exception($"Could not find {beginNamespaceTag} in {FullPath}");
        }
        
        return contents[start..end];
    }

    public void MoveTo(string newPath)
    {
        throw new NotImplementedException();
    }

    private static List<DependencyInfo> ParseDependencies(string csproj)
    {
        var dependencies = new List<DependencyInfo>();
        var lines = csproj.Split('\n');

        foreach (var line in lines)
        {
            var gotRefType = TryGetDependencyType(line, out var refType);
            if (!gotRefType) continue;

            var gotName = TryExtractDependencyName(line, refType, out var name);
            if (!gotName) continue;

            if (refType == DependencyType.PackageReference)
            {
                var gotVersion = TryExtractVersion(line, name, out var version);
                if (!gotVersion)
                {
                    Log.Warning($"Could not extract version for package {name}");
                    // should we throw here?
                }

                dependencies.Add(new DependencyInfo(name, version, refType));
            }
            else
            {
                dependencies.Add(new DependencyInfo(name, refType));

                // todo: parse HintPath
            }
        }

        return dependencies;
    }

    private static bool TryGetDependencyType(string line, out DependencyType type)
    {
        if (line.Contains(ProjectReferenceStart))
        {
            type = DependencyType.ProjectReference;
            return true;
        }

        if (line.Contains(DllReferenceStart))
        {
            type = DependencyType.DllReference;
            return true;
        }

        if (line.Contains(PackageReferenceStart))
        {
            type = DependencyType.PackageReference;
            return true;
        }

        type = default;
        return false;
    }

    private static bool TryExtractDependencyName(string line, DependencyType type, out string name)
    {
        var refStart = type switch
                           {
                               DependencyType.ProjectReference => ProjectReferenceStart,
                               DependencyType.DllReference     => DllReferenceStart,
                               DependencyType.PackageReference => PackageReferenceStart,
                               _                               => string.Empty
                           };

        if (refStart == string.Empty)
        {
            name = string.Empty;
            return false;
        }

        var startIndex = line.IndexOf(refStart, StringComparison.Ordinal) + refStart.Length;
        var endIndex = line.IndexOf('"', startIndex);
        name = line[startIndex..endIndex];
        return true;
    }

    private static bool TryExtractVersion(string line, string dependencyName, out string version)
    {
        const string versionStart = "\" Version=\"";
        var startIndex = line.IndexOf(dependencyName, StringComparison.Ordinal) + dependencyName.Length;
        var versionIndex = line.IndexOf(versionStart, startIndex, StringComparison.Ordinal);
        if (versionIndex == -1)
        {
            version = string.Empty;
            return false;
        }

        versionIndex += versionStart.Length;
        var endIndex = line.IndexOf('"', versionIndex);

        if (endIndex == -1)
        {
            version = string.Empty;
            return false;
        }

        version = line[versionIndex..endIndex];
        return true;
    }

    private const string DllReferenceStart = "<Reference Include=\"";
    private const string ProjectReferenceStart = "<ProjectReference Include=\"";
    private const string PackageReferenceStart = "<PackageReference Include=\"";

    public bool TryRecompile() // todo - rate limit recompiles for when multiple files change. should change operator resources to projects or something
    {
        return Compiler.TryCompile(this);
    }
}