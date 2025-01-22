#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Build.Construction;
using T3.Core.Compilation;
using T3.Core.Resource;
using T3.Core.UserData;
using Encoding = System.Text.Encoding;

// ReSharper disable SuggestBaseTypeForParameterInConstructor

namespace T3.Editor.Compilation;

internal sealed class CsProjectFile
{
    public string FullPath => _projectRootElement.FullPath;
    public string Directory => _projectRootElement.DirectoryPath;
    public string Name => Path.GetFileNameWithoutExtension(FullPath);
    public string RootNamespace => _projectRootElement.GetOrAddProperty(PropertyType.RootNamespace, Name);
    public string VersionString => _projectRootElement.GetOrAddProperty(PropertyType.VersionPrefix, "1.0.0");
    public Version Version => new(VersionString!);
    public AssemblyInformation? Assembly { get; private set; }
    public event Action<CsProjectFile>? AssemblyLoaded;

    private readonly string _releaseRootDirectory;
    private readonly string _debugRootDirectory;
    private readonly ProjectRootElement _projectRootElement;

    private string TargetFramework => _projectRootElement.GetOrAddProperty(PropertyType.TargetFramework, ProjectXml.TargetFramework);

    private CsProjectFile(ProjectRootElement projectRootElement)
    {
        _projectRootElement = projectRootElement;

        var targetFramework = TargetFramework;
        if (!ProjectXml.FrameworkIsCurrent(targetFramework))
        {
            var newFramework = ProjectXml.UpdateFramework(targetFramework);
            _projectRootElement.SetOrAddProperty(PropertyType.TargetFramework, newFramework);
        }

        var dir = Directory;
        _releaseRootDirectory = Path.Combine(dir, "bin", "Release");
        _debugRootDirectory = Path.Combine(dir, "bin", "Debug");

        // clear the release info on recompilation
        AssemblyLoaded += file => _cachedReleaseInfo = null;
    }

    public readonly struct CsProjectLoadInfo
    {
        public readonly string? Error;
        public readonly CsProjectFile? CsProjectFile;
        public readonly bool NeedsUpgrade;
        public readonly bool NeedsRecompile;

        internal CsProjectLoadInfo(CsProjectFile? file, string? error)
        {
            Error = error;
            CsProjectFile = file;

            if (file == null)
            {
                NeedsUpgrade = false;
                NeedsRecompile = false;
                return;
            }

            var targetFramework = file.TargetFramework;
            var currentFramework = RuntimeInformation.FrameworkDescription;

            // todo - additional version checks
            var needsUpgrade = !targetFramework.Contains(currentFramework);
            NeedsUpgrade = needsUpgrade;
            NeedsRecompile = needsUpgrade;
        }
    }

    public static bool TryLoad(string filePath, out CsProjectLoadInfo loadInfo)
    {
        try
        {
            var root = ProjectRootElement.Open(filePath);
            if (root == null)
            {
                loadInfo = new CsProjectLoadInfo(null, $"Failed to open project file at \"{filePath}\"");
                return false;
            }

            loadInfo = new CsProjectLoadInfo(new CsProjectFile(root), null);
            return true;
        }
        catch (Exception e)
        {
            var error = $"Failed to open project file at \"{filePath}\":\n{e}";
            loadInfo = new CsProjectLoadInfo(null, error);
            return false;
        }
    }

    private FileInfo GetBuildTargetFileInfo()
    {
        var directory = GetBuildTargetDirectory();
        var defaultAssemblyName = ProjectXml.UnevaluatedVariable(PropertyType.RootNamespace.GetItemName());
        var property = _projectRootElement.GetOrAddProperty(PropertyType.AssemblyName, defaultAssemblyName);
        var dllName = property != defaultAssemblyName ? property + ".dll" : RootNamespace + ".dll";
        return new FileInfo(Path.Combine(directory, dllName));
    }

    public string GetBuildTargetDirectory()
    {
        return Path.Combine(GetRootDirectory(EditorBuildMode), VersionSubfolder, TargetFramework);
    }
    
    private string VersionSubfolder => Version.ToBasicVersionString();

    private string GetRootDirectory(Compiler.BuildMode buildMode) => buildMode == Compiler.BuildMode.Debug ? _debugRootDirectory : _releaseRootDirectory;

    // todo - rate limit recompiles for when multiple files change
    public bool TryRecompile([NotNullWhen(true)] out ReleaseInfo? releaseInfo, bool nugetRestore)
    {
        var previousAssembly = Assembly;
        ModifyBuildVersion(0, 0, 1);
        var success = Compiler.TryCompile(this, EditorBuildMode, nugetRestore);

        if (!success)
        {
            ModifyBuildVersion(0, 0, -1);
            releaseInfo = null;
            return false;
        }

        previousAssembly?.Unload();
        Stopwatch stopwatch = new();
        stopwatch.Start();
        var loaded = TryLoadAssembly();
        stopwatch.Stop();
        Log.Info($"{(loaded ? "Loading" : "Failing to load")} assembly took {stopwatch.ElapsedMilliseconds} ms");

        if (loaded)
        {
            if (!TryGetReleaseInfo(out releaseInfo))
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

    public void UpdateVersionForIOChange(int modifyAmount)
    {
        ModifyBuildVersion(0, Math.Clamp(modifyAmount, -1, 1), 0);
    }

    private void ModifyBuildVersion(int majorModify, int minorModify, int buildModify)
    {
        _projectRootElement.SetOrAddProperty(PropertyType.EditorVersion, Program.Version.ToBasicVersionString());

        var version = Version;
        var newVersion = new Version(version.Major + majorModify, version.Minor + minorModify, version.Build + buildModify);
        _projectRootElement.SetOrAddProperty(PropertyType.VersionPrefix, newVersion.ToBasicVersionString());

        _projectRootElement.Save();
    }

    public bool TryCompileRelease(string externalDirectory, bool nugetRestore)
    {
        return Compiler.TryCompile(this, PlayerBuildMode, nugetRestore, targetDirectory: externalDirectory);
    }

    // todo- use Microsoft.Build.Construction and Microsoft.Build.Evaluation
    public static CsProjectFile CreateNewProject(string projectName, string nameSpace, bool shareResources, string parentDirectory)
    {
        var defaultHomeDir = Path.Combine(UserData.ReadOnlySettingsFolder, "default-home");
        var files = System.IO.Directory.EnumerateFiles(defaultHomeDir, "*");
        string destinationDirectory = Path.Combine(parentDirectory, projectName);
        destinationDirectory = Path.GetFullPath(destinationDirectory);
        System.IO.Directory.CreateDirectory(destinationDirectory);

        var dependenciesDirectory = Path.Combine(destinationDirectory, ProjectXml.DependenciesFolder);
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

        var projRoot = ProjectXml.CreateNewProjectRootElement(nameSpace, homeGuid);

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

    public void RemoveOldBuilds(Compiler.BuildMode buildMode)
    {
        var versionSubfolder = VersionSubfolder;
        var rootDir = new DirectoryInfo(GetRootDirectory(buildMode));

        rootDir.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
               .AsParallel()
               .Where(x => !x.FullName.Contains(versionSubfolder)) // ignore our current version
               .ForAll(directory =>
                       {
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

    public bool TryLoadAssembly(FileInfo? assemblyFile = null)
    {
        assemblyFile ??= GetBuildTargetFileInfo();
        if (!assemblyFile.Exists)
        {
            Log.Error($"Could not find assembly at \"{assemblyFile.FullName}\"");
            return false;
        }

        if (!RuntimeAssemblies.TryLoadAssemblyFromDirectory(assemblyFile.Directory!.FullName, out var assembly))
        {
            Log.Error($"Could not load assembly at \"{assemblyFile.FullName}\"");
            return false;
        }

        Assembly = assembly;

        AssemblyLoaded?.Invoke(this);
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
            if (!TryLoadAssembly())
            {
                releaseInfo = null;
                return false;
            }
        }

        var success = Assembly!.TryGetReleaseInfo(out _cachedReleaseInfo);
        releaseInfo = _cachedReleaseInfo;
        return success;
    }
}