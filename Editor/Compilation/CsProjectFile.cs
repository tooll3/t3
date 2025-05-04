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

/// <summary>
/// A class that assists in the creation, modification, and compilation of a csproj file.
/// Each editable project is currently represented by a csproj file, which is compiled and loaded at runtime.
/// There are several unusual properties in the csproj file that are used to accommodate T3's feature set - see <see cref="ProjectXml"/>
/// for more details. This can be considered a higher-level utility class that handles versioning, provides simple compilation methods, and provides
/// straightforward access to properties within the csproj file.
/// </summary>
internal sealed class CsProjectFile
{
    /// <summary>
    /// The path to the csproj file.
    /// </summary>
    public string FullPath => _projectRootElement.FullPath;
    
    /// <summary>
    /// The directory containing the csproj file.
    /// </summary>
    public string Directory => _projectRootElement.DirectoryPath;
    
    /// <summary>
    /// The name of the csproj file.
    /// </summary>
    public string Name => Path.GetFileNameWithoutExtension(FullPath);
    
    /// <summary>
    /// The root namespace of the project, as defined in the csproj file.
    /// </summary>
    public string RootNamespace => _projectRootElement.GetOrAddProperty(PropertyType.RootNamespace, Name);
    
    /// <summary>
    /// The version string of the project, as defined in the csproj file.
    /// </summary>
    public string VersionString => _projectRootElement.GetOrAddProperty(PropertyType.VersionPrefix, "1.0.0");
    
    /// <summary>
    /// The version of the project, as defined in the csproj file.
    /// </summary>
    public Version Version => new(VersionString!);
    
    /// <summary>
    /// The assembly information for the project, provided the assembly has been loaded.
    /// </summary>
    public AssemblyInformation? Assembly { get; private set; }
    
    /// <summary>
    /// An event that is triggered when the assembly for this project is loaded. Currently this is largely unused.
    /// </summary>
    public event Action<CsProjectFile>? AssemblyLoaded;

    /// <summary>
    /// Returns the target dotnet framework for the project, or adds the default framework if none is found and returns that.
    /// </summary>
    private string TargetFramework => _projectRootElement.GetOrAddProperty(PropertyType.TargetFramework, ProjectXml.TargetFramework);

    private CsProjectFile(ProjectRootElement projectRootElement)
    {
        _projectRootElement = projectRootElement;

        var targetFramework = TargetFramework;
        
        // check if the project needs its dotnet version upgraded. If so, update the project file accordingly.
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

    /// <summary>
    /// Details about a loaded csproj file, including any errors that occurred during loading.
    /// </summary>
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

    /// <summary>
    /// Loads the csproj file at the given path, returning a <see cref="CsProjectLoadInfo"/> struct that contains the loaded file and any errors.
    /// Does not actually handle any assemblies or type loading - it's just a way to load the xml file.
    /// </summary>
    /// <returns>True if successful</returns>
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

    /// <summary>
    /// Returns the file info for this project's primary dll. This file may or may not exist, as this is simply a "functional" way to generate the file path.
    /// </summary>
    private FileInfo GetBuildTargetFileInfo()
    {
        var directory = GetBuildTargetDirectory();
        var defaultAssemblyName = ProjectXml.UnevaluatedVariable(PropertyType.RootNamespace.GetItemName());
        var property = _projectRootElement.GetOrAddProperty(PropertyType.AssemblyName, defaultAssemblyName);
        var dllName = property != defaultAssemblyName ? property + ".dll" : RootNamespace + ".dll";
        return new FileInfo(Path.Combine(directory, dllName));
    }

    /// <summary>
    /// Returns the directory where the primary dll for this project is built. This directory may or may not exist, as this is simply a "functional"
    /// way to generate the directory path.
    /// </summary>
    public string GetBuildTargetDirectory()
    {
        return Path.Combine(GetRootDirectory(EditorBuildMode), VersionSubfolder, TargetFramework);
    }
    
    /// <summary>
    /// We separate out builds by version so we can attempt to reload a new version before the old one is fully unloaded since we cannot guarantee
    /// when the old version will be unloaded. This is a simple property to ensure the way we generate these directories is consistent.
    /// </summary>
    private string VersionSubfolder => Version.ToBasicVersionString();

    /// <summary>
    /// Returns the debug & release build directories for this project.
    /// </summary>
    /// <param name="buildMode"></param>
    /// <returns></returns>
    private string GetRootDirectory(Compiler.BuildMode buildMode) => buildMode == Compiler.BuildMode.Debug ? _debugRootDirectory : _releaseRootDirectory;

    // todo - rate limit recompiles for when multiple files change
    /// <summary>
    /// Compiles/recompiles this project in debug mode for runtime use in the Editor.
    /// </summary>
    /// <param name="releaseInfo">The resulting release info if successful</param>
    /// <param name="nugetRestore">True if NuGet packages should be restored</param>
    /// <returns>True if successful</returns>
    public bool TryRecompile([NotNullWhen(true)] out ReleaseInfo? releaseInfo, bool nugetRestore)
    {
        var previousAssembly = Assembly;
        if(_needsIncrementBuildVersion)
            ModifyBuildVersion(0, 0, 1);
        var success = Compiler.TryCompile(this, EditorBuildMode, nugetRestore);

        if (!success)
        {
            if(_needsIncrementBuildVersion)
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
        _needsIncrementBuildVersion = false;
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

    /// <summary>
    /// For building release-mode assemblies for use in the Player. All other runtime-compilation is done in debug mode.
    /// </summary>
    /// <param name="externalDirectory">Output directory</param>
    /// <param name="nugetRestore">True if NuGet packages should be restored</param>
    /// <returns>True if successful</returns>
    public bool TryCompileRelease(string externalDirectory, bool nugetRestore)
    {
        return Compiler.TryCompile(this, PlayerBuildMode, nugetRestore, targetDirectory: externalDirectory);
    }

    // todo- use Microsoft.Build.Construction and Microsoft.Build.Evaluation
    /// <summary>
    /// Creates a new .csproj file and populates all required files for a new T3 project.
    /// </summary>
    /// <param name="projectName">The name of the new project</param>
    /// <param name="nameSpace">The root namespace of the new project</param>
    /// <param name="shareResources">True if the project should share its resources with other packages</param>
    /// <param name="parentDirectory">The directory inside which the new project should be created. The project will actually reside inside
    /// "parentDirectory/projectName"</param>
    /// <returns></returns>
    /// <remarks>
    /// todo - find a better home for this
    /// todo: - files copied into the new project should be generated at runtime where possible -
    /// for example, the default home canvas symbol/symbolui
    /// should probably be copied from the Examples package wholesale, with replaced guids.
    /// </remarks>
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

    /// <summary>
    /// Deletes all build directories that are not the current version of the specified <see cref="Compiler.BuildMode"/>
    /// </summary>
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

    /// <summary>
    /// Tries to load the assembly file provided - if none are provided, it will attempt to load the assembly from the default build directory.
    /// </summary>
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

    /// <summary>
    /// Attempts to retrieve the release info for this project. If the release info has already been loaded, it will return that.
    /// Note that this method is only valid if the assembly has been loaded, or can be loaded. Therefore, the project will need to have been compiled at least
    /// once prior to calling this method.
    ///
    /// The reason it must be compiled is that the release info is generated from this csproj file at build time.
    /// </summary>
    /// <param name="releaseInfo"></param>
    /// <returns>True if successful</returns>
    /// <remarks>
    /// Todo: investigate the use of 
    /// <a href="https://learn.microsoft.com/en-us/dotnet/standard/assembly/set-attributes-project-file">custom assembly metadata</a> to store this information
    /// in the assembly itself
    /// </remarks>
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
    

    private const Compiler.BuildMode EditorBuildMode = Compiler.BuildMode.Debug;
    private const Compiler.BuildMode PlayerBuildMode = Compiler.BuildMode.Release;
    private ReleaseInfo? _cachedReleaseInfo;

    private readonly string _releaseRootDirectory;
    private readonly string _debugRootDirectory;
    private readonly ProjectRootElement _projectRootElement;
    private bool _needsIncrementBuildVersion;

    public void MarkCodeChanged()
    {
        _needsIncrementBuildVersion = true;
    }
}