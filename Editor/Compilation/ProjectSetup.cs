#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;
using T3.Core.Compilation;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

/// <summary>
/// handles the creation, loading, unloading, and general management of projects and packages
/// todo: simplify/refactor as it's pretty confusing
/// </summary>
internal static partial class ProjectSetup
{
    public static bool TryCreateProject(string nameSpace, bool shareResources, [NotNullWhen(true)] out EditableSymbolProject? newProject)
    {
        var name = nameSpace.Split('.').Last();
        var newCsProj = CsProjectFile.CreateNewProject(name, nameSpace, shareResources, UserSettings.Config.DefaultNewProjectDirectory);

        if (!newCsProj.TryRecompile(out var releaseInfo, true))
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

        UpdateSymbolPackages(package);
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

    private static void AddToLoadedPackages(PackageWithReleaseInfo package)
    {
        var key = package.Package.GetKey();
        if (!ActivePackages.TryAdd(key, package))
            throw new InvalidOperationException($"Failed to add package {key}: already exists");
    }

    private static void InitializePackageResources(PackageWithReleaseInfo package)
    {
        var symbolPackage = (EditorSymbolPackage)package.Package;
        symbolPackage.InitializeShaderLinting(ResourceManager.SharedShaderPackages);
    }

    private static bool TryLoadProject(ProjectWithReleaseInfo release, bool forceRecompile, out PackageWithReleaseInfo? operatorPackage)
    {
        var csProj = release.CsProject!;
        csProj.RemoveOldBuilds(Compiler.BuildMode.Debug);

        var releaseInfo = release.ReleaseInfo;
        forceRecompile = forceRecompile 
                         || releaseInfo == null 
                         || !releaseInfo.EditorVersion.Matches(Program.Version) && (!releaseInfo.RootNamespace.StartsWith("Lib") && !releaseInfo.RootNamespace.StartsWith("Types") && !releaseInfo.RootNamespace.StartsWith("Examples")); // force recompile if the project was authored with a different version of the editor
        
        var success = forceRecompile
                          ? csProj.TryRecompile(out _, true) || csProj.TryLoadAssembly() // recompile - if failed, load latest
                          : csProj.TryLoadAssembly() || csProj.TryRecompile(out _, true); // load latest - if failed, recompile
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

        Log.Debug($"Loaded CSProj file {csProj.Name}");
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

    #if DEBUG
    #endif

    private static void InitializeCustomUis(IReadOnlyList<AssemblyInformation> nonOperatorAssemblies)
    {
        if (nonOperatorAssemblies.Count == 0)
            return;

        var uiInitializerTypes = nonOperatorAssemblies
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

    private static void UpdateSymbolPackages(params PackageWithReleaseInfo[] packages)
    {
        var stopWatch = Stopwatch.StartNew();
        
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

            if (!package.ReleaseInfo.EditorVersion.Matches(Program.Version))
            {
                var msg = $"The T3 editor version ({Program.Version}) does not match the editor version" +
                          $" {package.Package.DisplayName} was authored with ({package.ReleaseInfo.EditorVersion})";
                Log.Warning(msg);
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
                const bool parallel = false;
                package.LoadSymbols(parallel, out var newlyRead, out var allNewSymbols);
                package.ApplySymbolChildren(newlyRead);
                package.LoadUiFiles(parallel, allNewSymbols, out var newlyLoadedUis, out var preExistingUis);
                package.LocateSourceCodeFiles();
                package.RegisterUiSymbols(newlyLoadedUis, preExistingUis);

                var count = package.Symbols.Sum(x => x.Value.InstancesOfSelf.Count());
                Log.Debug($"Updated symbol package {package.DisplayName} in {stopWatch.ElapsedMilliseconds}ms with {count} instances of its symbols");
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
        
        Log.Info($"Updated {packages.Length} symbol packages in {stopWatch.ElapsedMilliseconds}ms");
    }

    private static string GetKey(this SymbolPackage package) => package.RootNamespace;

    private static readonly Dictionary<string, PackageWithReleaseInfo> ActivePackages = new();
    internal static readonly IEnumerable<SymbolPackage> AllPackages = ActivePackages.Values.Select(x => x.Package);
    private static readonly List<AssemblyInformation> EditorOnlyPackages = [];

    private static readonly Dictionary<AssemblyInformation, List<IEditorUiExtension>> _uiInitializers = new();

    private readonly record struct ProjectWithReleaseInfo(FileInfo ProjectFile, CsProjectFile? CsProject, ReleaseInfo? ReleaseInfo);
    private readonly record struct SymbolUiLoadInfo(SymbolUi[] NewlyLoaded, SymbolUi[] PreExisting);
    private readonly record struct AssemblyConstructorInfo(AssemblyInformation AssemblyInformation, Type InstanceType);
}