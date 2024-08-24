#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.Build.Construction;
using T3.Core.Compilation;
using T3.Core.Resource;
using T3.Core.UserData;
using Encoding = System.Text.Encoding;
// ReSharper disable SuggestBaseTypeForParameterInConstructor

namespace T3.Editor.Compilation;

internal sealed partial class CsProjectFile
{
    public string FullPath => _projectRootElement.FullPath;
    public string Directory => _projectRootElement.DirectoryPath;
    public string Name => Path.GetFileNameWithoutExtension(FullPath);
    public readonly string? RootNamespace;
    public AssemblyInformation? Assembly { get; private set; }
    
    // default to true bc uncompiled projects are normally operator assemblies. todo: this should be more robust
    public bool IsOperatorAssembly => Assembly?.IsOperatorAssembly ?? true; 
    
    public event Action<CsProjectFile>? Recompiled;

    private uint _buildId = GetNewBuildId();
    private readonly string _targetFramework;
    private readonly string _releaseRootDirectory;
    private readonly string _debugRootDirectory;
    private readonly ProjectRootElement _projectRootElement;

    private CsProjectFile(ProjectRootElement projectRootElement)
    {
        _projectRootElement = projectRootElement;

        RootNamespace = GetProperty(PropertyType.RootNamespace, _projectRootElement, Name);
        _targetFramework = GetProperty(PropertyType.TargetFramework, _projectRootElement, DefaultProperties[PropertyType.TargetFramework]);

        var dir = Directory;
        _releaseRootDirectory = Path.Combine(dir, "bin", "Release");
        _debugRootDirectory = Path.Combine(dir, "bin", "Debug");
        
        // clear the release info on recompilation
        Recompiled += file => _cachedReleaseInfo = null;
    }

    public static bool TryLoad(string filePath, [NotNullWhen(true)] out CsProjectFile? csProjFile, [NotNullWhen(false)] out string? error)
    {
        try
        {
            var root = ProjectRootElement.Open(filePath);
            if (root == null)
            {
                csProjFile = null;
                error = $"Failed to open project file at \"{filePath}\"";
                return false;
            }
            
            csProjFile = new CsProjectFile(root);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            error = $"Failed to open project file at \"{filePath}\":\n{e}";
            csProjFile = null;
            return false;
        }
    }

    private FileInfo GetBuildTargetPath()
    {
        var directory = GetBuildTargetDirectory();
        return new FileInfo(Path.Combine(directory, DllName));
    }

    private string DllName
    {
        get
        {
            var defaultAssemblyName = UnevaluatedVariable(GetNameOf(PropertyType.RootNamespace));
            var property = GetProperty(PropertyType.AssemblyName, _projectRootElement, defaultAssemblyName);
            if(property != defaultAssemblyName)
                return property + ".dll";
            
            return RootNamespace + ".dll";
        }
    }

    public string GetBuildTargetDirectory()
    {
        return Path.Combine(GetRootDirectory(EditorBuildMode), _buildId.ToString(CultureInfo.InvariantCulture), _targetFramework);
    }

    private string GetRootDirectory(Compiler.BuildMode buildMode) => buildMode == Compiler.BuildMode.Debug ? _debugRootDirectory : _releaseRootDirectory;

    // int to keep directory name smaller
    private static uint GetNewBuildId() => unchecked((uint)DateTime.UtcNow.Ticks);


    // todo - rate limit recompiles for when multiple files change
    public bool TryRecompile([NotNullWhen(true)] out ReleaseInfo? releaseInfo)
    {
        var previousBuildId = _buildId;
        var previousAssembly = Assembly;
        _buildId = GetNewBuildId();
        var success = Compiler.TryCompile(this, EditorBuildMode);

        if (!success)
        {
            _buildId = previousBuildId;
            releaseInfo = null;
            return false;
        }

        previousAssembly?.Unload();
        Stopwatch stopwatch = new();
        stopwatch.Start();
        var loaded = TryLoadAssembly(null);
        stopwatch.Stop();
        Log.Info($"{(loaded ? "Loading" : "Failing to load")} assembly took {stopwatch.ElapsedMilliseconds} ms");

        if (loaded)
        {
            if (TryGetReleaseInfo(out releaseInfo))
            {
                UpdateVersionForNextBuild();
            }
            else
            {
                loaded = false;
                Log.Error($"{Name} successfully compiled but failed to find release info");
            }
        }
        else
        {
            releaseInfo = null;
        }
        
        return loaded;
    }

    // when inputs, outputs, or the existence of operators changes, we want to use the version number to mark this as a breaking change
    public void UpdateVersionForIOChange()
    {
        var version = GetCurrentVersion();
        var newVersion = new Version(version.Major, version.Minor + 1, 0);
        SetOrAddProperty(PropertyType.VersionPrefix, newVersion.ToBasicVersionString(), _projectRootElement);
        _projectRootElement.Save();
    }

    private void UpdateVersionForNextBuild()
    {
        SetOrAddProperty(PropertyType.EditorVersion, Program.Version.ToBasicVersionString(), _projectRootElement);
        
        var version = GetCurrentVersion();
        var newVersion = new Version(version.Major, version.Minor, version.Build + 1);
        SetOrAddProperty(PropertyType.VersionPrefix, newVersion.ToBasicVersionString(), _projectRootElement);
         
        _projectRootElement.Save();
    }

    private Version GetCurrentVersion()
    {
        var versionString = GetProperty(PropertyType.VersionPrefix, _projectRootElement, "1.0.0");
        var actualVersion = new Version(versionString!);
        return actualVersion;
    }

    public bool TryCompileRelease(string externalDirectory)
    {
        return Compiler.TryCompile(this, PlayerBuildMode, externalDirectory);
    }

    // todo- use Microsoft.Build.Construction and Microsoft.Build.Evaluation
    public static CsProjectFile CreateNewProject(string projectName, string nameSpace, bool shareResources, string parentDirectory)
    {
        var defaultHomeDir = Path.Combine(UserData.ReadOnlySettingsFolder, "default-home");
        var files = System.IO.Directory.EnumerateFiles(defaultHomeDir, "*");
        string destinationDirectory = Path.Combine(parentDirectory, projectName);
        destinationDirectory = Path.GetFullPath(destinationDirectory);
        System.IO.Directory.CreateDirectory(destinationDirectory);

        var dependenciesDirectory = Path.Combine(destinationDirectory, DependenciesFolder);
        System.IO.Directory.CreateDirectory(dependenciesDirectory);

        var resourcesDirectory = Path.Combine(destinationDirectory, ResourceManager.ResourcesSubfolder);
        System.IO.Directory.CreateDirectory(resourcesDirectory);

        string placeholderDependencyPath = Path.Combine(dependenciesDirectory, "PlaceNativeDllDependenciesHere.txt");
        File.Create(placeholderDependencyPath).Dispose();

        // todo - use source generation and direct type references instead of this copy and replace strategy
        const string guidPlaceholder = "{{GUID}}";
        const string nameSpacePlaceholder = "{{NAMESPACE}}";
        const string usernamePlaceholder = "{{USER}}";
        const string shareResourcesPlaceholder = "{{SHARE_RESOURCES}}";
        const string projectNamePlaceholder = "{{PROJ}}";

        var shouldShareResources = shareResources ? "true" : "false";
        var username = nameSpace.Split('.').First();
        
        var homeGuid = Guid.NewGuid();
        var homeGuidString = homeGuid.ToString();

        var projRoot = CreateNewProjectRootElement(nameSpace, homeGuid);

        foreach (var file in files)
        {
            var text = File.ReadAllText(file)
                           .Replace(projectNamePlaceholder, projectName)
                           .Replace(guidPlaceholder, homeGuidString)
                           .Replace(nameSpacePlaceholder, nameSpace)
                           .Replace(usernamePlaceholder, username)
                           .Replace(shareResourcesPlaceholder, shouldShareResources);

            var destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(file))
                                          .Replace(projectNamePlaceholder, projectName)
                                          .Replace(guidPlaceholder, homeGuidString);

            File.WriteAllText(destinationFilePath, text);
        }

        projRoot.FullPath = Path.Combine(destinationDirectory, $"{projectName}.csproj");
        projRoot.Save(Encoding.UTF8);
        return new CsProjectFile(projRoot);
    }

    public bool TryLoadLatestAssembly()
    {
        if (!TryGetBuildDirectories(EditorBuildMode, out _, out var latestDll))
        {
            return false;
        }

        if (!TryLoadAssembly(latestDll))
        {
            Log.Error($"Could not load latest assembly at \"{latestDll.FullName}\"");
            return false;
        }

        return true;
    }

    private bool TryGetBuildDirectories(Compiler.BuildMode buildMode, out DirectoryInfo[] compatibleDirectories, [NotNullWhen(true)]out FileInfo? latestDll)
    {
        var rootDir = new DirectoryInfo(GetRootDirectory(buildMode));
        if (!rootDir.Exists)
        {
            compatibleDirectories = Array.Empty<DirectoryInfo>();
            latestDll = null;
            return false;
        }

        compatibleDirectories = rootDir.EnumerateDirectories($"*{_targetFramework}", SearchOption.AllDirectories).ToArray();

        var dllName = DllName;
        latestDll = compatibleDirectories
                   .Select(x => new FileInfo(Path.Combine(x.FullName, dllName)))
                   .Where(file => file.Exists)
                   .MaxBy(x => x.LastWriteTime);
        return latestDll != null;
    }

    public void RemoveOldBuilds(Compiler.BuildMode buildMode)
    {
        if (!TryGetBuildDirectories(buildMode, out var compatibleDirectories, out var latestDll))
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

    private bool TryLoadAssembly(FileInfo? assemblyFile)
    {
        assemblyFile ??= GetBuildTargetPath();
        if (!assemblyFile.Exists)
        {
            Log.Error($"Could not find assembly at \"{assemblyFile.FullName}\"");
            return false;
        }

        if (!RuntimeAssemblies.TryLoadAssemblyFromDirectory(assemblyFile.Directory.FullName, out var assembly))
        {
            Log.Error($"Could not load assembly at \"{assemblyFile.FullName}\"");
            return false;
        }

        Assembly = assembly;

        Recompiled?.Invoke(this);
        return true;
    }

    private const Compiler.BuildMode EditorBuildMode = Compiler.BuildMode.Debug;
    private const Compiler.BuildMode PlayerBuildMode = Compiler.BuildMode.Release;
    private ReleaseInfo? _cachedReleaseInfo;

    public bool TryGetReleaseInfo([NotNullWhen(true)] out ReleaseInfo? releaseInfo)
    {
        if (_cachedReleaseInfo != null)
        {
            releaseInfo = _cachedReleaseInfo;
            return true;
        }

        if (Assembly == null)
        {
            releaseInfo = null;
            return false;
        }
        
        var success = Assembly.TryGetReleaseInfo(out _cachedReleaseInfo);
        releaseInfo = _cachedReleaseInfo;
        return success;
    }
}