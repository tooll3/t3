#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.External;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

/// <summary>
/// handles the creation, loading, unloading, and general management of projects and packages
/// todo: simplify/refactor as it's pretty confusing
/// </summary>
internal static class ProjectSetup
{
    public static bool TryCreateProject(string name, string nameSpace, bool shareResources, [NotNullWhen(true)] out EditableSymbolProject? newProject)
    {
        var newCsProj = CsProjectFile.CreateNewProject(name, nameSpace, shareResources, UserSettings.Config.DefaultNewProjectDirectory);

        if (!newCsProj.TryRecompile(out var releaseInfo))
        {
            Log.Error("Failed to compile new project");
            newProject = null;
            return false;
        }

        if (releaseInfo.HomeGuid == Guid.Empty)
        {
            Log.Error($"No project home found for project {name}");
            newProject = null;
            return false;
        }

        newProject = new EditableSymbolProject(newCsProj);
        var package = new PackageWithReleaseInfo(newProject, releaseInfo);
        ActivePackages.Add(newProject.GetKey(), package);

        UpdateSymbolPackage(package);
        InitializePackageResources(package);
        return true;
    }

    internal static void RemoveSymbolPackage(SymbolPackage package, bool needsDispose)
    {
        var key = package.GetKey();
        if (!ActivePackages.Remove(key, out _))
            throw new InvalidOperationException($"Failed to remove package {key}: does not exist");

        if (needsDispose)
            package.Dispose();
    }

    private static string GetKey(this SymbolPackage package) => package.RootNamespace;

    private readonly record struct ProjectWithReleaseInfo(FileInfo ProjectFile, CsProjectFile? CsProject, ReleaseInfo? ReleaseInfo);

    private static readonly Dictionary<string, PackageWithReleaseInfo> ActivePackages = new();
    internal static readonly IEnumerable<SymbolPackage> AllPackages = ActivePackages.Values.Select(x => x.Package);
    private static readonly List<AssemblyInformation> EditorOnlyPackages = [];

    internal static bool TryLoadAll(bool forceRecompile, [NotNullWhen(false)] out Exception? exception)
    {
        try
        {
            LoadAll(forceRecompile);

            exception = null;
            return true;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
        }
    }

    /// <summary>
    /// Loads all projects and packages - for use only at the start of the editor
    /// </summary>
    /// <param name="forceRecompile">all symbol projects will be recompiled if possible</param>
    /// <exception cref="Exception">Unknown exceptions may be raised - if you want to handle them, wrap this in a try/catch</exception>
    private static void LoadAll(bool forceRecompile)
    {
        #if DEBUG
        Stopwatch totalStopwatch = new();
        totalStopwatch.Start();
        #endif

        // todo: change to load CsProj files from specific directories and specific nuget packages from a package directory
        ConcurrentBag<AssemblyInformation> nonOperatorAssemblies = [];

        #if !DEBUG
        // Load pre-built built-in packages as read-only
        LoadBuiltInPackages(nonOperatorAssemblies);
        #endif

        // Find project files
        var csProjFiles = FindCsProjFiles();

        // Load projects
        LoadProjects(csProjFiles, nonOperatorAssemblies, forceRecompile, unsatisfiedProjects: out _, failedProjects: out _);

        // Register UI types
        var allPackages = ActivePackages.Values
                                        .ToArray();

        foreach (var assembly in nonOperatorAssemblies)
        {
            if (assembly.IsEditorOnly)
            {
                EditorOnlyPackages.Add(assembly);
            }
        }

        // Update all symbol packages
        UpdateSymbolPackages(allPackages);

        // Initialize resources and shader linting
        InitializePackageResources(allPackages);

        // Initialize custom UIs
        UiRegistration.RegisterUiTypes();
        InitializeCustomUis(EditorOnlyPackages);

        foreach (var package in SymbolPackage.AllPackages)
        {
            Log.Debug($"Loaded {package.DisplayName}");
        }

        #if DEBUG
            totalStopwatch.Stop();
            Log.Debug($"Total load time: {totalStopwatch.ElapsedMilliseconds}ms");
        #endif
    }

    private static void AddToLoadedPackages(PackageWithReleaseInfo package)
    {
        var key = package.Package.GetKey();
        if (!ActivePackages.TryAdd(key, package))
            throw new InvalidOperationException($"Failed to add package {key}: already exists");
    }

    private static void LoadBuiltInPackages(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies)
    {
        var directory = Directory.CreateDirectory(CoreOperatorDirectory);

        directory
           .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
           .Where(folder => !folder.Name.EndsWith(PlayerExporter.ExportFolderName, StringComparison.OrdinalIgnoreCase)) // ignore "player" project directory
           .ToList()
           .ForEach(directoryInfo =>
                    {
                        var thisDir = directoryInfo.FullName;
                        var packageInfo = Path.Combine(thisDir, RuntimeAssemblies.PackageInfoFileName);
                        if (!RuntimeAssemblies.TryLoadAssemblyFromPackageInfoFile(packageInfo, out var assembly, out var releaseInfo))
                        {
                            Log.Error($"Failed to load assembly from package info file at {packageInfo}");
                            return;
                        }

                        if (!assembly.IsEditorOnly)
                        {
                            AddToLoadedPackages(new PackageWithReleaseInfo(new EditorSymbolPackage(assembly), releaseInfo));
                        }
                        else
                        {
                            nonOperatorAssemblies.Add(assembly);
                        }
                    });
    }

    private static void InitializePackageResources(IReadOnlyCollection<PackageWithReleaseInfo> allSymbolPackages)
    {
        foreach (var package in allSymbolPackages)
        {
            InitializePackageResources(package);
        }

        ShaderLinter.AddPackage(SharedResources.ResourcePackage, ResourceManager.SharedShaderPackages);
    }

    private static void InitializePackageResources(PackageWithReleaseInfo package)
    {
        var symbolPackage = (EditorSymbolPackage)package.Package;
        symbolPackage.InitializeShaderLinting(ResourceManager.SharedShaderPackages);
    }

    [SuppressMessage("ReSharper", "OutParameterValueIsAlwaysDiscarded.Local")]
    private static void LoadProjects(FileInfo[] csProjFiles, ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, bool forceRecompile,
                                     out List<ProjectWithReleaseInfo> unsatisfiedProjects, out List<ProjectWithReleaseInfo> failedProjects)
    {
        // Load each project file and its associated assembly
        var releases = csProjFiles
                      .AsParallel()
                      .Select(fileInfo =>
                              {
                                  if (!CsProjectFile.TryLoad(fileInfo.FullName, out var csProjFile, out var error))
                                  {
                                      Log.Error($"Failed to load project at \"{fileInfo.FullName}\":\n{error}");
                                      return new ProjectWithReleaseInfo(fileInfo, null, null);
                                  }

                                  // this may call for some reworking of how that works (MSBuild actions in C#?), or generation at project creation time
                                  if (!csProjFile.TryGetReleaseInfo(out var releaseInfo) && !csProjFile.TryRecompile(out releaseInfo))
                                  {
                                      Log.Error($"Failed to load release info for {csProjFile.Name}");
                                      return new ProjectWithReleaseInfo(fileInfo, csProjFile, null);
                                  }

                                  return new ProjectWithReleaseInfo(fileInfo, csProjFile, releaseInfo);
                              })
                      .Where(x =>
                             {
                                 if (x.ReleaseInfo == null)
                                 {
                                     Log.Error($"Failed to load release info for {x.ProjectFile.FullName}");
                                     return false;
                                 }

                                 if (x.CsProject != null) return true;

                                 Log.Error($"Failed to load project at \"{x.ProjectFile.FullName}\"");
                                 return false;
                             })
                      .ToArray();

        LoadProjects(nonOperatorAssemblies, releases, forceRecompile, out failedProjects, out unsatisfiedProjects);

        foreach (var project in failedProjects)
        {
            Log.Error($"Failed to load {project.CsProject!.Name}");
        }

        foreach (var project in unsatisfiedProjects)
        {
            Log.Error($"Unsatisfied dependencies for {project.CsProject!.Name}");
        }
    }

    private static void LoadProjects(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, IReadOnlyCollection<ProjectWithReleaseInfo> releases,
                                     bool forceRecompile,
                                     out List<ProjectWithReleaseInfo> failedProjects, out List<ProjectWithReleaseInfo> unsatisfiedProjects)
    {
        List<ProjectWithReleaseInfo> satisfied = [];
        var unsatisfied = new List<ProjectWithReleaseInfo>();
        foreach (var release in releases)
        {
            if (AllDependenciesAreSatisfied(release))
                satisfied.Add(release);
            else
                unsatisfied.Add(release);
        }

        var failed = new List<ProjectWithReleaseInfo>();
        List<PackageWithReleaseInfo> loadedOperatorPackages = [];

        var shouldTryAgain = true;

        while (shouldTryAgain)
        {
            // empty failures into satisfied
            for (int i = failed.Count - 1; i >= 0; i--)
            {
                var release = failed[i];
                failed.RemoveAt(i);
                satisfied.Add(release);
            }

            // compile all projects that have satisfied dependencies
            CompileSatisfiedPackages();
            // retry failures after any successes
            RetryFailures();

            var unsatisfiedCount = unsatisfied.Count;

            SatisfyDependencies();

            // if no further dependencies were satisfied, we're done
            // whether it's because of success or failure
            shouldTryAgain = unsatisfied.Count < unsatisfiedCount;
        }

        // try compiling/loading the unsatisfied projects anyway. should we do this?
        // todo - this compilation process is not as robust as the previous loop
        for (var index = unsatisfied.Count - 1; index >= 0; index--)
        {
            var project = unsatisfied[index];
            if (!TryLoad(nonOperatorAssemblies, project, loadedOperatorPackages, forceRecompile))
            {
                failed.Add(project);
                unsatisfied.RemoveAt(index);
            }
        }

        unsatisfiedProjects = unsatisfied;
        failedProjects = failed;
        return;

        static bool TryLoad(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, ProjectWithReleaseInfo release,
                            List<PackageWithReleaseInfo> loadedOperatorPackages, bool forceRecompile)
        {
            if (!TryLoadProject(release, forceRecompile, out var operatorPackage))
            {
                return false;
            }

            if (operatorPackage.HasValue) // won't have value if the assembly is a non-operator assembly
            {
                loadedOperatorPackages.Add(operatorPackage.Value);
                AddToLoadedPackages(operatorPackage.Value);
                return true;
            }

            var assembly = release.CsProject!.Assembly!;
            nonOperatorAssemblies.Add(assembly);
            return true;
        }

        void CompileSatisfiedPackages()
        {
            for (var i = satisfied.Count - 1; i >= 0; i--)
            {
                var release = satisfied[i];
                satisfied.RemoveAt(i);
                if (!TryLoad(nonOperatorAssemblies, release, loadedOperatorPackages, forceRecompile))
                    failed.Add(release);
            }
        }

        void RetryFailures()
        {
            for (var i = failed.Count - 1; i >= 0; i--)
            {
                var release = failed[i];
                failed.RemoveAt(i);
                if (!TryLoad(nonOperatorAssemblies, release, loadedOperatorPackages, forceRecompile))
                    failed.Add(release);
            }
        }

        void SatisfyDependencies()
        {
            for (var i = unsatisfied.Count - 1; i >= 0; i--)
            {
                var release = unsatisfied[i];
                if (AllDependenciesAreSatisfied(release))
                {
                    satisfied.Add(release);
                    unsatisfied.RemoveAt(i);
                }
            }
        }
    }

    private static bool TryLoadProject(ProjectWithReleaseInfo release, bool forceRecompile, out PackageWithReleaseInfo? operatorPackage)
    {
        var csProj = release.CsProject!;
        csProj.RemoveOldBuilds(Compiler.BuildMode.Debug);

        var success = forceRecompile
                          ? csProj.TryRecompile(out _) || csProj.TryLoadLatestAssembly() // recompile - if failed, load latest
                          : csProj.TryLoadLatestAssembly() || csProj.TryRecompile(out _); // load latest - if failed, recompile
        if (!success)
        {
            Log.Error($"Failed to load {csProj.Name}");
            operatorPackage = null;
            return false;
        }

        if (!csProj.Assembly!.IsEditorOnly)
        {
            var project = new EditableSymbolProject(csProj);
            operatorPackage = new PackageWithReleaseInfo(project, release.ReleaseInfo!);
        }
        else
        {
            operatorPackage = null;
        }

        Log.Info($"Loaded {csProj.Name}");
        return true;
    }

    private static bool AllDependenciesAreSatisfied(ProjectWithReleaseInfo projectWithReleaseInfo)
    {
        var releaseInfo = projectWithReleaseInfo.ReleaseInfo!;
        Debug.Assert(releaseInfo != null);

        foreach (var packageReference in releaseInfo.OperatorPackages)
        {
            if (!ActivePackages.ContainsKey(packageReference.Identity))
            {
                return false;
            }
        }

        return true;
    }

    private static FileInfo[] FindCsProjFiles()
    {
        return GetProjectDirectories()
              .SelectMany(dir => Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories))
              .Select(x => new FileInfo(x))
              .ToArray();
    }

    private static IEnumerable<string> GetProjectDirectories()
    {
        // ReSharper disable once JoinDeclarationAndInitializer
        string[] topDirectories = [UserSettings.Config.DefaultNewProjectDirectory];

        var projectSearchDirectories = topDirectories
                                      .Where(Directory.Exists)
                                      .SelectMany(Directory.EnumerateDirectories)
                                      .Where(dirName => !dirName.Contains(PlayerExporter.ExportFolderName, StringComparison.OrdinalIgnoreCase));

        #if DEBUG // Add Built-in packages as projects
            projectSearchDirectories = projectSearchDirectories.Concat(Directory.EnumerateDirectories(Path.Combine(T3ParentDirectory, "Operators"))
                                                                                .Where(path =>
                                                                                       {
                                                                                           var subDir = Path.GetFileName(path);
                                                                                           return !subDir.StartsWith('.'); // ignore things like .git and file sync folders 
                                                                                       }));
        #endif
        return projectSearchDirectories;
    }

    private static readonly string CoreOperatorDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators");
    #if DEBUG
    private static readonly string T3ParentDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "..", "..", "..", "..");
    #endif

    private static void InitializeCustomUis(IReadOnlyList<AssemblyInformation> nonOperatorAssemblies)
    {
        if (nonOperatorAssemblies.Count == 0)
            return;

        var uiInitializerTypes = nonOperatorAssemblies
                                .ToArray()
                                .AsParallel()
                                .SelectMany(assemblyInfo => assemblyInfo.TypesInheritingFrom(typeof(IEditorUiExtension))
                                                                        .Select(type => new AssemblyConstructorInfo(assemblyInfo, type)))
                                .ToList();

        foreach (var constructorInfo in uiInitializerTypes)
        {
            //var assembly = Assembly.LoadFile(constructorInfo.AssemblyInformation.Path);
            var assemblyInfo = constructorInfo.AssemblyInformation;
            var instanceType = constructorInfo.InstanceType;
            try
            {
                var activated = assemblyInfo.CreateInstance(instanceType);
                if (activated == null)
                {
                    Log.Error($"Created null object for {instanceType.Name}");
                    continue;
                }

                var initializer = (IEditorUiExtension)activated;
                initializer.Initialize();

                if (_uiInitializers.TryGetValue(assemblyInfo, out var initializers))
                    initializers.Add(initializer);
                else
                    _uiInitializers[assemblyInfo] = [initializer];

                Log.Info($"Initialized UI initializer for {assemblyInfo.Name}: {instanceType.Name}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create UI initializer for {assemblyInfo.Name}: \"{instanceType}\" - does it have a parameterless constructor?\n{e.Message}");
                if (e is FileNotFoundException fileNotFoundException)
                {
                    Log.Error($"File not found: {fileNotFoundException.FileName}");
                }
            }
        }
    }

    public static void DisposePackages()
    {
        var allPackages = SymbolPackage.AllPackages.ToArray();
        foreach (var package in allPackages)
            package.Dispose();
    }

    internal static void UpdateSymbolPackage<T>(T project) where T : EditorSymbolPackage
    {
        UpdateSymbolPackages(ActivePackages[project.GetKey()]);
    }

    private static void UpdateSymbolPackage(PackageWithReleaseInfo package)
    {
        UpdateSymbolPackages(package);
    }

    private static void UpdateSymbolPackages(params PackageWithReleaseInfo[] packages)
    {
        // update all the editor ui packages in concert with the operator packages
        var uiPackagesNeedingReload = new List<AssemblyInformation>();
        foreach (var package in packages)
        {
            var assembly = package.Package.AssemblyInformation;
            assembly.Unload();

            foreach (var nonOperatorAssembly in EditorOnlyPackages)
            {
                if (!nonOperatorAssembly.DependsOn(package))
                    continue;

                uiPackagesNeedingReload.Add(nonOperatorAssembly);
            }
        }

        foreach (var uiAssembly in uiPackagesNeedingReload)
        {
            if (_uiInitializers.TryGetValue(uiAssembly, out var initializers))
            {
                for (var index = initializers.Count - 1; index >= 0; index--)
                {
                    var initializer = initializers[index];
                    initializer.Uninitialize();
                    initializers.RemoveAt(index);
                }
            }

            uiAssembly.Unload();
            uiAssembly.ReplaceResolversOf(packages);
        }

        InitializeCustomUis(uiPackagesNeedingReload);

        // actually update the symbol packages

        // this switch statement exists to avoid the overhead of parallelization for a single package, e.g. when compiling changes to a single project
        switch (packages.Length)
        {
            case 0:
                Log.Warning($"Tried to update symbol packages but none were provided");
                return;
            case 1:
            {
                var package = (EditorSymbolPackage)packages[0].Package;
                package.LoadSymbols(true, out var newlyRead, out var allNewSymbols);
                package.ApplySymbolChildren(newlyRead);
                package.LoadUiFiles(true, allNewSymbols, out var newlyLoadedUis, out var preExistingUis);
                package.LocateSourceCodeFiles();
                package.RegisterUiSymbols(newlyLoadedUis, preExistingUis);
                return;
            }
        }

        // do the same as above, just in several steps so we can do them in parallel
        ConcurrentDictionary<EditorSymbolPackage, List<SymbolJson.SymbolReadResult>> loadedSymbols = new();
        ConcurrentDictionary<EditorSymbolPackage, List<Symbol>> loadedOrCreatedSymbols = new();
        packages
           .AsParallel()
           .ForAll(package => //pull out for non-editable ones too
                   {
                       var symbolPackage = (EditorSymbolPackage)package.Package;
                       symbolPackage.LoadSymbols(false, out var newlyRead, out var allNewSymbols);
                       loadedSymbols.TryAdd(symbolPackage, newlyRead);
                       loadedOrCreatedSymbols.TryAdd(symbolPackage, allNewSymbols);
                   });

        loadedSymbols
           .AsParallel()
           .ForAll(pair => pair.Key.ApplySymbolChildren(pair.Value));

        ConcurrentDictionary<EditorSymbolPackage, SymbolUiLoadInfo> loadedSymbolUis = new();
        packages
           .AsParallel()
           .ForAll(package =>
                   {
                       var symbolPackage = (EditorSymbolPackage)package.Package;
                       var newlyRead = loadedOrCreatedSymbols[symbolPackage];
                       symbolPackage.LoadUiFiles(false, newlyRead, out var newlyReadUis, out var preExisting);
                       loadedSymbolUis.TryAdd(symbolPackage, new SymbolUiLoadInfo(newlyReadUis, preExisting));
                   });

        loadedSymbolUis
           .AsParallel()
           .ForAll(pair => { pair.Key.LocateSourceCodeFiles(); });

        foreach (var (symbolPackage, symbolUis) in loadedSymbolUis)
        {
            symbolPackage.RegisterUiSymbols(symbolUis.NewlyLoaded, symbolUis.PreExisting);
        }
    }

    private static readonly Dictionary<AssemblyInformation, List<IEditorUiExtension>> _uiInitializers = new();

    private readonly record struct SymbolUiLoadInfo(SymbolUi[] NewlyLoaded, SymbolUi[] PreExisting);

    private readonly record struct AssemblyConstructorInfo(AssemblyInformation AssemblyInformation, Type InstanceType);
}