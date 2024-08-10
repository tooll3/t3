using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Editor.App;
using T3.Editor.External;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

/// <summary>
/// handles the creation and management of symbol projects
/// </summary>
internal static class ProjectSetup
{
    public static bool TryCreateProject(string name, string nameSpace, bool shareResources, out EditableSymbolProject newProject)
    {
        var newCsProj = CsProjectFile.CreateNewProject(name, nameSpace, shareResources, UserSettings.Config.DefaultNewProjectDirectory);

        if (!newCsProj.TryRecompile())
        {
            Log.Error("Failed to compile new project");
            newProject = null;
            return false;
        }

        if (!newCsProj.TryGetReleaseInfo(out var releaseInfo))
        {
            Log.Error($"Failed to get release info for project {newCsProj.Name}");
            newProject = null;
            return false;
        }

        if (releaseInfo.HomeGuid == Guid.Empty)
        {
            Log.Error("Failed to create project home");
            newProject = null;
            return false;
        }

        newProject = new EditableSymbolProject(newCsProj, releaseInfo);
        newProject.InitializeResources();

        UpdateSymbolPackages(newProject);

        if (releaseInfo.HomeGuid != Guid.Empty)
            return true;

        Log.Error("Failed to find project home");
        RemoveSymbolPackage(newProject);
        return false;
    }

    private static void RemoveSymbolPackage(EditableSymbolProject project)
    {
        project.Dispose();
    }

    private readonly record struct PackageWithReleaseInfo(SymbolPackage Package, ReleaseInfo ReleaseInfo);

    private static readonly Dictionary<string, PackageWithReleaseInfo> LoadedPackages = new();

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    internal static bool TryInitialize(out Exception exception)
    {
        #if DEBUG
        Stopwatch totalStopwatch = new();
        totalStopwatch.Start();
        #endif

        try
        {
            // todo: change to load CsProjs from specific directories and specific nuget packages from a package directory
            List<PackageWithReleaseInfo> readOnlyPackages = new(); // "static" packages, remember to filter by operator vs non-operator assemblies
            ConcurrentBag<AssemblyInformation> nonOperatorAssemblies = new();

            #if !DEBUG
            // Load pre-built built-in packages as read-only
            LoadBuiltInPackages(readOnlyPackages, nonOperatorAssemblies);

            foreach (var package in readOnlyPackages)
                AddToLoadedPackages(package);
            #endif

            // Find project files
            var csProjFiles = FindCsProjFiles();

            // Load projects
            var projects = LoadProjects(csProjFiles, nonOperatorAssemblies);

            // Register UI types
            UiRegistration.RegisterUiTypes();
            InitializeCustomUis(nonOperatorAssemblies);

            var allSymbolPackages = projects
                                   .Concat(readOnlyPackages.Select(x => x.Package))
                                   .Cast<EditorSymbolPackage>()
                                   .ToArray();

            // Initialize resources and shader linting
            InitializePackageResources(allSymbolPackages);

            // Update all symbol packages
            UpdateSymbolPackages(allSymbolPackages);

            foreach (var package in SymbolPackage.AllPackages)
            {
                Log.Debug($"Loaded {package.DisplayName}");
            }

            #if DEBUG
            totalStopwatch.Stop();
            Log.Debug($"Total load time: {totalStopwatch.ElapsedMilliseconds}ms");
            #endif

            exception = null;
            return true;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
        }
    }

    private static void AddToLoadedPackages(PackageWithReleaseInfo package)
    {
        var key = package.ReleaseInfo.RootNamespace;
        if (!LoadedPackages.TryAdd(key, package))
            throw new InvalidOperationException($"Failed to add package {key}: already exists");
    }

    private static void LoadBuiltInPackages(List<PackageWithReleaseInfo> readOnlyPackages, ConcurrentBag<AssemblyInformation> nonOperatorAssemblies)
    {
        var directory = Directory.CreateDirectory(CoreOperatorDirectory);

        directory
           .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
           .Where(folder => !string.Equals(folder.Name, PlayerExporter.ExportFolderName,
                                           StringComparison.OrdinalIgnoreCase)) // ignore "player" project directory
           .ToList()
           .ForEach(directoryInfo =>
                    {
                        // todo - load by releaseInfo json, then load associated assembly
                        var thisDir = directoryInfo.FullName!;
                        var packageInfo = Path.Combine(thisDir, RuntimeAssemblies.PackageInfoFileName);
                        if (!RuntimeAssemblies.TryLoadAssemblyFromPackageInfoFile(packageInfo, out var assembly, out var releaseInfo))
                        {
                            Log.Error($"Failed to load assembly from package info file at {packageInfo}");
                            return;
                        }

                        if (assembly.IsOperatorAssembly)
                        {
                            readOnlyPackages.Add(new PackageWithReleaseInfo(new EditorSymbolPackage(assembly, releaseInfo), releaseInfo));
                        }
                        else
                        {
                            nonOperatorAssemblies.Add(assembly);
                        }
                    });
    }

    private static void InitializePackageResources(IReadOnlyCollection<EditorSymbolPackage> allSymbolPackages)
    {
        foreach (var package in allSymbolPackages)
        {
            package.InitializeResources();
        }

        var sharedShaderPackages = ResourceManager.SharedShaderPackages;
        foreach (var package in allSymbolPackages)
        {
            package.InitializeShaderLinting(sharedShaderPackages);
        }

        ShaderLinter.AddPackage(SharedResources.ResourcePackage, sharedShaderPackages);
    }

    private readonly record struct ProjectWithReleaseInfo(FileInfo ProjectFile, CsProjectFile? CsProject, ReleaseInfo? ReleaseInfo);

    private static IReadOnlyCollection<EditableSymbolProject> LoadProjects(FileInfo[] csProjFiles, ConcurrentBag<AssemblyInformation> nonOperatorAssemblies)
    {
        List<EditableSymbolProject> projects = new();
        ConcurrentBag<CsProjectFile> projectsNeedingCompilation = new();

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

                                  // todo: this will currently break - need to load release info from json before the project has been built.
                                  // this may require some reworking of how that works (MSBuild actions in C#?), or generation at project creation time
                                  if (!csProjFile.TryGetReleaseInfo(out var releaseInfo))
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

        LoadProjects(nonOperatorAssemblies, releases, out var failedProjects, out var unsatisfiedProjects);
        
        foreach(var project in failedProjects)
        {
            Log.Error($"Failed to load {project.CsProject!.Name}");
        }
        
        foreach(var project in unsatisfiedProjects)
        {
            Log.Error($"Unsatisfied dependencies for {project.CsProject!.Name}");
        }

        return projects;
    }

    private static void LoadProjects(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, IReadOnlyCollection<ProjectWithReleaseInfo> releases,
                                     out List<ProjectWithReleaseInfo> failedProjects, out List<ProjectWithReleaseInfo> unsatisfiedProjects)
    {
        List<ProjectWithReleaseInfo> satisfied = new();
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
            for(int i = failed.Count - 1; i >= 0; i--)
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

        unsatisfiedProjects = unsatisfied;
        failedProjects = failed;
        return;

        static bool TryLoad(ConcurrentBag<AssemblyInformation> nonOperatorAssemblies, ProjectWithReleaseInfo release,
                            List<PackageWithReleaseInfo> loadedOperatorPackages)
        {
            if (!TryLoadProject(release, out var operatorPackage))
            {
                return false;
            }

            if (operatorPackage.HasValue) // wont have value if the assembly is a non-operator assembly
            {
                loadedOperatorPackages.Add(operatorPackage.Value);
                AddToLoadedPackages(operatorPackage.Value);
                return true;
            }

            nonOperatorAssemblies.Add(release.CsProject!.Assembly);
            return true;
        }

        void CompileSatisfiedPackages()
        {
            for (var i = satisfied.Count - 1; i >= 0; i--)
            {
                var release = satisfied[i];
                satisfied.RemoveAt(i);
                if (!TryLoad(nonOperatorAssemblies, release, loadedOperatorPackages))
                    failed.Add(release);
            }
        }

        void RetryFailures()
        {
            for (var i = failed.Count - 1; i >= 0; i--)
            {
                var release = failed[i];
                failed.RemoveAt(i);
                if (!TryLoad(nonOperatorAssemblies, release, loadedOperatorPackages))
                    failed.Add(release);
            }
        }

        void SatisfyDependencies()
        {
            for (var i = unsatisfied.Count; i >= 0; i--)
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

    private static bool TryLoadProject(ProjectWithReleaseInfo release, out PackageWithReleaseInfo? operatorPackage)
    {
        var csProj = release.CsProject!;
        if (!csProj.TryLoadLatestAssembly() && !csProj.TryRecompile())
        {
            Log.Error($"Failed to load {csProj.Name}");
            operatorPackage = null;
            return false;
        }

        csProj.RemoveOldBuilds(Compiler.BuildMode.Debug);
        if (csProj.IsOperatorAssembly)
        {
            var project = new EditableSymbolProject(csProj, release.ReleaseInfo!);
            operatorPackage = new PackageWithReleaseInfo(project, release.ReleaseInfo);
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

        var allSatisfied = false;

        foreach (var packageReference in releaseInfo.OperatorPackages)
        {
            if (!LoadedPackages.ContainsKey(packageReference.Identity))
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
        var topDirectories = new[] { CoreOperatorDirectory, UserSettings.Config.DefaultNewProjectDirectory };
        var projectSearchDirectories = topDirectories
                                      .Where(Directory.Exists)
                                      .SelectMany(Directory.EnumerateDirectories)
                                      .Where(dirName => !dirName.Contains(PlayerExporter.ExportFolderName, StringComparison.OrdinalIgnoreCase));

        #if DEBUG // Add Built-in packages as projects
            projectSearchDirectories = projectSearchDirectories.Concat(Directory.EnumerateDirectories(Path.Combine(T3ParentDirectory, "Operators"))
                                                                                .Where(path =>
                                                                                       {
                                                                                           var subDir = Path.GetFileName(path);
                                                                                           return !subDir.StartsWith('.'); // ignore things like .git and .syncthing folders 
                                                                                       }));
        #endif
        return projectSearchDirectories;
    }

    private static readonly string CoreOperatorDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators");
    #if DEBUG
    private static readonly string T3ParentDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "..", "..", "..", "..");
    #endif

    private static void InitializeCustomUis(IReadOnlyCollection<AssemblyInformation> nonOperatorAssemblies)
    {
        var uiInitializerTypes = nonOperatorAssemblies
                                .ToArray()
                                .AsParallel()
                                .SelectMany(assemblyInfo => assemblyInfo.TypesInheritingFrom(typeof(IOperatorUIInitializer))
                                                                        .Select(type => new AssemblyConstructorInfo(assemblyInfo, type)))
                                .ToList();

        foreach (var constructorInfo in uiInitializerTypes)
        {
            //var assembly = Assembly.LoadFile(constructorInfo.AssemblyInformation.Path);
            var assemblyName = constructorInfo.AssemblyInformation.Path;
            var typeName = constructorInfo.InstanceType.FullName;
            try
            {
                var activated = Activator.CreateInstanceFrom(assemblyName, typeName);
                if (activated == null)
                {
                    throw new Exception($"Created null activator handle for {typeName}");
                }

                var initializer = (IOperatorUIInitializer)activated.Unwrap();
                if (initializer == null)
                {
                    throw new Exception($"Casted to null initializer for {typeName}");
                }

                initializer.Initialize();
                Log.Info($"Initialized UI initializer for {constructorInfo.AssemblyInformation.Name}: {typeName}");
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create UI initializer for {constructorInfo.AssemblyInformation.Name}: \"{typeName}\" - does it have a parameterless constructor?\n{e}");
            }
        }
    }

    public static void DisposePackages()
    {
        var allPackages = SymbolPackage.AllPackages.ToArray();
        foreach (var package in allPackages)
            package.Dispose();
    }

    internal static void UpdateSymbolPackage(EditableSymbolProject project) => UpdateSymbolPackages(project);

    private static void UpdateSymbolPackages(params EditorSymbolPackage[] symbolPackages)
    {
        switch (symbolPackages.Length)
        {
            case 0:
                throw new ArgumentException("No symbol packages to update");
            case 1:
            {
                var package = symbolPackages[0];
                package.LoadSymbols(true, out var newlyRead, out var allNewSymbols);
                package.ApplySymbolChildren(newlyRead);
                package.LoadUiFiles(true, allNewSymbols, out var newlyLoadedUis, out var preExistingUis);
                package.LocateSourceCodeFiles();
                package.RegisterUiSymbols(true, newlyLoadedUis, preExistingUis);
                return;
            }
        }

        ConcurrentDictionary<EditorSymbolPackage, List<SymbolJson.SymbolReadResult>> loadedSymbols = new();
        ConcurrentDictionary<EditorSymbolPackage, List<Symbol>> loadedOrCreatedSymbols = new();
        symbolPackages
           .AsParallel()
           .ForAll(package => //pull out for non-editable ones too
                   {
                       package.LoadSymbols(false, out var newlyRead, out var allNewSymbols);
                       loadedSymbols.TryAdd(package, newlyRead);
                       loadedOrCreatedSymbols.TryAdd(package, allNewSymbols);
                   });

        loadedSymbols
           .AsParallel()
           .ForAll(pair => pair.Key.ApplySymbolChildren(pair.Value));

        ConcurrentDictionary<EditorSymbolPackage, SymbolUiLoadInfo> loadedSymbolUis = new();
        symbolPackages
           .AsParallel()
           .ForAll(package =>
                   {
                       package.LoadUiFiles(false, loadedOrCreatedSymbols[package], out var newlyRead, out var preExisting);
                       loadedSymbolUis.TryAdd(package, new SymbolUiLoadInfo(newlyRead, preExisting));
                   });

        loadedSymbolUis
           .AsParallel()
           .ForAll(pair => { pair.Key.LocateSourceCodeFiles(); });

        foreach (var (symbolPackage, symbolUis) in loadedSymbolUis)
        {
            symbolPackage.RegisterUiSymbols(false, symbolUis.NewlyLoaded, symbolUis.PreExisting);
        }
    }

    private readonly record struct SymbolUiLoadInfo(SymbolUi[] NewlyLoaded, SymbolUi[] PreExisting);

    private readonly record struct AssemblyConstructorInfo(AssemblyInformation AssemblyInformation, Type InstanceType);
}