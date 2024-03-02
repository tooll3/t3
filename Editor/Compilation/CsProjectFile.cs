using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.UserData;

namespace T3.Editor.Compilation;

internal class CsProjectFile
{
    public string FullPath { get; }
    public string Directory { get; }
    public string FileName { get; }
    public string Name { get; }
    public string Contents { get; }
    public string RootNamespace { get; }
    public string TargetFramework { get; }
    public AssemblyInformation Assembly { get; private set; }
    public IReadOnlyList<DependencyInfo> Dependencies => _dependencies;
    public bool IsOperatorAssembly => Assembly.IsOperatorAssembly;
    public const string ProjectNamePlaceholder = "{{PROJ}}";
    private readonly List<DependencyInfo> _dependencies;
    public event Action<CsProjectFile> Recompiled;

    private uint _buildId = GetNewBuildId();
    private readonly string _releaseRootDirectory;
    private readonly string _debugRootDirectory;
    public readonly string DllName;

    public CsProjectFile(FileInfo file)
    {
        FullPath = file.FullName;
        Contents = File.ReadAllText(file.FullName);
        Directory = Path.GetDirectoryName(FullPath)!;
        FileName = Path.GetFileName(FullPath);
        Name = Path.GetFileNameWithoutExtension(FullPath);

        _dependencies = ParseDependencies(Contents);
        RootNamespace = GetRootNamespace(Contents);
        TargetFramework = GetTargetFramework(Contents);

        _releaseRootDirectory = Path.Combine(Directory, "bin", "Release");
        _debugRootDirectory = Path.Combine(Directory, "bin", "Debug");
        DllName = Name + ".dll";
    }

    private FileInfo GetBuildTargetPath()
    {
        var directory = GetBuildTargetDirectory();
        return new FileInfo(Path.Combine(directory, DllName));
    }

    public string GetBuildTargetDirectory()
    {
        return Path.Combine(GetRootDirectory(), _buildId.ToString(CultureInfo.InvariantCulture), TargetFramework);
    }

    private string GetRootDirectory() => BuildMode == Compiler.BuildMode.Debug ? _debugRootDirectory : _releaseRootDirectory;

    private string GetTargetFramework(string contents)
    {
        const string beginTargetFrameworkTag = "<TargetFramework>";
        const string endTargetFrameworkTag = "</TargetFramework>";
        var start = contents.IndexOf(beginTargetFrameworkTag, StringComparison.Ordinal) + beginTargetFrameworkTag.Length;
        var end = contents.IndexOf(endTargetFrameworkTag, StringComparison.Ordinal);
        if (start == -1 || end == -1 || start > end)
        {
            Log.Error($"Could not find {beginTargetFrameworkTag} in {FullPath}");
            return string.Empty;
        }

        return contents[start..end];
    }

    // int to keep directory name smaller
    private static uint GetNewBuildId() => unchecked((uint)DateTime.UtcNow.Ticks);

    private string GetRootNamespace(string contents)
    {
        const string beginNamespaceTag = "<RootNamespace>";
        const string endNamespaceTag = "</RootNamespace>";
        var start = contents.IndexOf(beginNamespaceTag, StringComparison.Ordinal) + beginNamespaceTag.Length;
        var end = contents.IndexOf(endNamespaceTag, StringComparison.Ordinal);
        if (start == -1 || end == -1)
        {
            Log.Error($"Could not find {beginNamespaceTag} in {FullPath}");
            return string.Empty;
        }

        return contents[start..end];
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

    // todo - rate limit recompiles for when multiple files change
    public bool TryRecompile()
    {
        var previousBuildId = _buildId;
        var previousAssembly = Assembly;
        _buildId = GetNewBuildId();
        var success = Compiler.TryCompile(this);

        if (!success)
        {
            _buildId = previousBuildId;
            return false;
        }

        previousAssembly?.Unload();
        Stopwatch stopwatch = new();
        stopwatch.Start();
        var loaded = TryLoadAssembly(null);
        stopwatch.Stop();
        Log.Info($"{(loaded ? "Loading" : "Failing to load")} assembly took {stopwatch.ElapsedMilliseconds} ms");
        return loaded;
    }

    public bool TryCompileExternal(string externalDirectory)
    {
        return Compiler.TryCompile(this, externalDirectory);
    }

    // todo- use Microsoft.Build.Construction and Microsoft.Build.Evaluation
    public static CsProjectFile CreateNewProject(string projectName, string nameSpace, string parentDirectory)
    {
        var defaultHomeDir = Path.Combine(UserData.SettingsFolderInApplicationDirectory, "default-home");
        var files = System.IO.Directory.EnumerateFiles(defaultHomeDir, "*");
        string destinationDirectory = Path.Combine(parentDirectory, projectName);
        destinationDirectory = Path.GetFullPath(destinationDirectory);
        System.IO.Directory.CreateDirectory(destinationDirectory);

        var dependenciesDirectory = Path.Combine(destinationDirectory, "dependencies");
        System.IO.Directory.CreateDirectory(dependenciesDirectory);

        var resourcesDirectory = Path.Combine(destinationDirectory, "Resources");
        System.IO.Directory.CreateDirectory(resourcesDirectory);

        string placeholderDependencyPath = Path.Combine(dependenciesDirectory, "PlaceNativeDllDependenciesHere.txt");
        File.Create(placeholderDependencyPath).Dispose();

        const string guidPlaceholder = "{{GUID}}";
        const string defaultReferencesPlaceholder = "{{DEFAULT_REFS}}";
        const string nameSpacePlaceholder = "{{NAMESPACE}}";

        var homeGuid = Guid.NewGuid().ToString();
        string csprojPath = null;
        foreach (var file in files)
        {
            var text = File.ReadAllText(file)
                           .Replace(ProjectNamePlaceholder, projectName)
                           .Replace(guidPlaceholder, homeGuid)
                           .Replace(defaultReferencesPlaceholder, CoreReferences)
                           .Replace(nameSpacePlaceholder, nameSpace);

            var destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(file))
                                          .Replace(ProjectNamePlaceholder, projectName)
                                          .Replace(guidPlaceholder, homeGuid);

            File.WriteAllText(destinationFilePath, text);

            if (destinationFilePath.EndsWith(".csproj"))
                csprojPath = destinationFilePath;
        }

        if (csprojPath == null)
        {
            Log.Error($"Could not find .csproj in {defaultHomeDir}");
            return null;
        }

        return new CsProjectFile(new FileInfo(csprojPath));
    }

    public bool TryLoadLatestAssembly()
    {
        if (!TryGetBuildDirectories(out var compatibleDirectories, out var latestDll))
            return false;

        var loaded = TryLoadAssembly(latestDll);

        if (!loaded)
        {
            Log.Error($"Could not load latest assembly at \"{latestDll.FullName}\"");
            return false;
        }

        return true;
    }

    private bool TryGetBuildDirectories(out DirectoryInfo[] compatibleDirectories, out FileInfo latestDll)
    {
        var rootDir = new DirectoryInfo(GetRootDirectory());
        if (!rootDir.Exists)
        {
            compatibleDirectories = Array.Empty<DirectoryInfo>();
            latestDll = null;
            return false;
        }

        compatibleDirectories = rootDir.EnumerateDirectories($"*{TargetFramework}", SearchOption.AllDirectories).ToArray();

        latestDll = compatibleDirectories.SelectMany(x => x.EnumerateFiles(DllName)).MaxBy(x => x.LastWriteTime);
        return latestDll != null;
    }

    public void RemoveOldBuilds()
    {
        if (!TryGetBuildDirectories(out var compatibleDirectories, out var latestDll))
            return;

        var latestDir = Assembly != null ? Assembly.Directory : latestDll.Directory!.FullName;

        // delete all other dll directories
        compatibleDirectories
           .AsParallel()
           .ForAll(directory =>
                   {
                       if (directory.FullName == latestDir)
                           return;

                       try
                       {
                           directory.Delete(recursive: true);
                       }
                       catch (Exception e)
                       {
                           Log.Error($"Could not delete directory \"{directory.FullName}\": {e}");
                       }
                   });
    }

    private bool TryLoadAssembly(FileInfo assemblyFile)
    {
        assemblyFile ??= GetBuildTargetPath();
        if (!assemblyFile.Exists)
        {
            Log.Error($"Could not find assembly at \"{assemblyFile.FullName}\"");
            return false;
        }

        var gotAssembly = RuntimeAssemblies.TryLoadAssemblyInformation(assemblyFile.FullName, out var assembly);
        if (!gotAssembly)
        {
            Log.Error($"Could not load assembly at \"{assemblyFile.FullName}\"");
            return false;
        }

        Assembly = assembly;

        Recompiled?.Invoke(this);

        return true;
    }

    private const string ExpandedAssemblyDirectory = $"$({RuntimeAssemblies.EnvironmentVariableName})";
    private const string Indentation = "\t\t";
    private const string BeginReference = $"<Reference Include=\"{ExpandedAssemblyDirectory}/";
    private const string Ending = "\"/>\n";

    private const string CoreReferences = BeginReference + "Core.dll" + Ending +
                                          Indentation + BeginReference + "Logging.dll" + Ending +
                                          Indentation + BeginReference + "SharpDX.dll" + Ending +
                                          Indentation + BeginReference + "SharpDX.Direct3D11.dll" + Ending +
                                          Indentation + BeginReference + "SharpDX.DXGI.dll" + Ending;

    private const string DllReferenceStart = "<Reference Include=\"";
    private const string ProjectReferenceStart = "<ProjectReference Include=\"";
    private const string PackageReferenceStart = "<PackageReference Include=\"";

    internal const Compiler.BuildMode BuildMode = Compiler.BuildMode.Debug;
    internal const Compiler.BuildMode PlayerBuildMode = Compiler.BuildMode.Release;
}