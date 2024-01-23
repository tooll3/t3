using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

internal static class ProjectSetup
{
    private static readonly List<EditableSymbolProject> EditableSymbolProjectsRw = new();
    public static readonly IReadOnlyList<EditableSymbolProject> EditableSymbolPackages = EditableSymbolProjectsRw;
    internal static bool NeedsUserProject;

    internal static void CreateOrMigrateProject(object sender, string nameArgs)
    {
        var name = nameArgs;

        if (NeedsUserProject)
        {
            var success = !TryCreateProject(name, out var project);
            NeedsUserProject = success;
            if (success)
            {
                EditableSymbolProjectsRw.Add(project);
            }
        }
        else 
        {
            //var oldUserNamespace = $"user.{nameArgs.OldName}";
            //var newUserNamespace = $"user.{nameArgs.NewName}";

            Log.Warning($"Have not implemented project rename yet: {name}");
        }
    }

    private static bool TryCreateProject(string name, out EditableSymbolProject newProject)
    {
        var newCsProj = CsProjectFile.CreateNewProject(name, UserSettings.Config.DefaultNewProjectDirectory);
        if (newCsProj == null)
        {
            Log.Error("Failed to create new project");
            newProject = null;
            return false;
        }

        if (!newCsProj.TryRecompile(Compiler.BuildMode.Debug))
        {
            Log.Error("Failed to compile new project");
            newProject = null;
            return false;
        }

        if (!newCsProj.Assembly.HasHome)
        {
            Log.Error("Failed to create project home");
            newProject = null;
            return false;
        }

        newProject = new EditableSymbolProject(newCsProj);

        UpdateSymbolPackages(newProject);
        if (!newProject.TryCreateHome())
        {
            Log.Error("Failed to create project home");
            RemoveSymbolPackage(newProject);
            return false;
        }

        return true;
    }

    private static void RemoveSymbolPackage(EditableSymbolProject newUiSymbolData)
    {
        throw new NotImplementedException();
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    internal static bool TryInitialize(out Exception exception)
    {
        Stopwatch stopwatch = new();
        UiRegistration.RegisterUiTypes();
        #if DEBUG
        Stopwatch totalStopwatch = new();
        totalStopwatch.Start();
        #endif

        try
        {
            // todo: change to load CsProjs from specific directories and specific nuget packages from a package directory
            ConcurrentBag<EditorSymbolPackage> readOnlyPackages = new(); // "static" packages, remember to filter by operator vs non-operator assemblies
            ConcurrentBag<AssemblyInformation> nonOperatorAssemblies = new();

            stopwatch.Start();
            var coreAssemblyDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators"); // theoretically where the core libs assemblies will be
            var projectSearchDirectories = new[] { coreAssemblyDirectory, UserSettings.Config.DefaultNewProjectDirectory };

            Log.Debug($"Core directories initialized in {stopwatch.ElapsedMilliseconds}ms");

            #if IDE
            stopwatch.Restart();

            var operatorFolder = Path.Combine(GetT3ParentDirectory(), "Operators");
            projectSearchDirectories = Directory.EnumerateDirectories(operatorFolder)
                                       .Where(path => !path.EndsWith("user"))
                                       .Concat(projectSearchDirectories)
                                       .ToArray();

            stopwatch.Stop();
            Log.Debug($"Found {projectSearchDirectories.Length} root directories in {stopwatch.ElapsedMilliseconds}ms");
            #else
            stopwatch.Restart();
            var readOnlyRootDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators");
            var directory = Directory.CreateDirectory(readOnlyRootDirectory);
            directory
               .EnumerateDirectories("*", SearchOption.TopDirectoryOnly)
               .ToList()
               .ForEach(package =>
                        {
                            foreach (var file in package.EnumerateFiles($"{package.Name}.dll", SearchOption.TopDirectoryOnly))
                            {
                                var loaded = RuntimeAssemblies.TryLoadAssemblyInformation(file.FullName, out var assembly);
                                if (!loaded)
                                {
                                    Log.Error($"Could not load assembly at \"{file.FullName}\"");
                                    continue;
                                }

                                if (assembly.IsOperatorAssembly)
                                    readOnlyPackages.Add(new EditorSymbolPackage(assembly, true));
                                else
                                    nonOperatorAssemblies.Add(assembly);
                            }
                        });
            Log.Debug($"Found built-in operator assemblies in {stopwatch.ElapsedMilliseconds}ms");
            #endif

            stopwatch.Restart();
            var csProjFiles = projectSearchDirectories
                             .Where(Directory.Exists)
                             .SelectMany(dir => Directory.EnumerateFiles(dir, "*.csproj", SearchOption.AllDirectories))
                             .Where(filePath => !filePath.Contains(CsProjectFile.ProjectNamePlaceholder))
                             .ToArray();

            Log.Debug($"Found {csProjFiles.Length} csproj files in {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            ConcurrentBag<EditableSymbolProject> projects = new();
            csProjFiles
               .ToList()
               .ForEach(path =>
                        {
                            stopwatch.Restart();
                            var csProjFile = new CsProjectFile(new FileInfo(path));
                            if (!csProjFile.TryLoadLatestAssembly(Compiler.BuildMode.Debug))
                            {
                                if (!csProjFile.TryRecompile(Compiler.BuildMode.Debug))
                                {
                                    Log.Info($"Failed to load {csProjFile.Name} in {stopwatch.ElapsedMilliseconds}ms");
                                    return;
                                }
                            }

                            if (csProjFile.IsOperatorAssembly)
                            {
                                var project = new EditableSymbolProject(csProjFile);
                                projects.Add(project);
                            }
                            else
                            {
                                nonOperatorAssemblies.Add(csProjFile.Assembly);
                            }

                            Log.Info($"Loaded {csProjFile.Name} in {stopwatch.ElapsedMilliseconds}ms");
                        });

            #if DEBUG
            Log.Debug($"Loaded {projects.Count} projects and {nonOperatorAssemblies.Count} non-operator assemblies in {totalStopwatch.ElapsedMilliseconds}ms");
            #endif

            var projectList = projects.ToArray();
            var allSymbolPackages = projectList
                                   .Concat(readOnlyPackages)
                                   .ToArray();

            EditableSymbolProjectsRw.AddRange(projectList);

            // Load operators
            stopwatch.Restart();
            InitializeCustomUis(nonOperatorAssemblies);
            Log.Debug($"Initialized custom uis in {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            UpdateSymbolPackages(allSymbolPackages);
            Log.Debug($"Updated symbol packages in {stopwatch.ElapsedMilliseconds}ms");

            #if DEBUG
            totalStopwatch.Stop();
            Log.Debug($"Total load time pre-home: {totalStopwatch.ElapsedMilliseconds}ms");
            #endif

            stopwatch.Restart();
            
            var exampleLib = allSymbolPackages.Single(x => x.AssemblyInformation.Name == "examples");
            EditorSymbolPackage.InitializeRoot(exampleLib);
            
            Log.Debug($"Created root symbol in {stopwatch.ElapsedMilliseconds}ms");
            
            var createdHome = false;
            foreach (var project in projectList)
            {
                createdHome |= project.TryCreateHome();
            }

            if (!createdHome)
            {
                NeedsUserProject = true;
            }

            stopwatch.Stop();
            if (!NeedsUserProject)
                Log.Debug($"Created home in {stopwatch.ElapsedMilliseconds}ms");

            exception = null;
            return true;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
        }
    }

    #if IDE
    internal static void CreateSymlinks()
    {
        var t3ParentDirectory = GetT3ParentDirectory();
        Log.Debug($"Creating symlinks for t3 project in {t3ParentDirectory}");
        var projectParentDirectory = Path.Combine(t3ParentDirectory, "Operators", "user");
        var directoryInfo = new DirectoryInfo(projectParentDirectory);
        if (!directoryInfo.Exists)
            throw new Exception($"Could not find project parent directory {projectParentDirectory}");

        Log.Debug($"Continuing creating symlinks for t3 project in {projectParentDirectory}");
        var targetDirectory = UserSettings.Config.DefaultNewProjectDirectory;
        Directory.CreateDirectory(targetDirectory);

        Log.Debug($"Beginning enumerating subdirectories of {directoryInfo}");
        foreach (var subDirectory in directoryInfo.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
        {
            //symlink to user project directory
            var linkName = Path.Combine(targetDirectory, subDirectory.Name);
            Log.Debug($"Target: {linkName} <- {subDirectory.FullName}");
            if (Directory.Exists(linkName))
                continue;

            Log.Debug($"Creating symlink: {linkName} <- {subDirectory.FullName}");
            Directory.CreateSymbolicLink(linkName, subDirectory.FullName);
        }
    }

    private static string GetT3ParentDirectory()
    {
        return Path.Combine(RuntimeAssemblies.CoreDirectory, "..", "..", "..", "..");
    }
    #endif

    private static void InitializeCustomUis(IReadOnlyCollection<AssemblyInformation> nonOperatorAssemblies)
    {
        var uiInitializerTypes = nonOperatorAssemblies
                                .ToArray()
                                .AsParallel()
                                .SelectMany(assemblyInfo => assemblyInfo.Types
                                                                        .Where(type =>
                                                                                   type.IsAssignableTo(typeof(IOperatorUIInitializer)))
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

    internal static void UpdateSymbolPackage(EditableSymbolProject project) => UpdateSymbolPackages(project);

    private static void UpdateSymbolPackages(params EditorSymbolPackage[] symbolPackages)
    {
        ConcurrentDictionary<EditorSymbolPackage, List<SymbolJson.SymbolReadResult>> loadedSymbols = new();
        ConcurrentDictionary<EditorSymbolPackage, IReadOnlyCollection<Symbol>> loadedOrCreatedSymbols = new();
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
                       package.LoadUiFiles(loadedOrCreatedSymbols[package], out var newlyRead, out var preExisting);
                       loadedSymbolUis.TryAdd(package, new SymbolUiLoadInfo(newlyRead, preExisting));
                   });

        loadedSymbolUis
           .AsParallel()
           .ForAll(pair =>
                   {
                       if (pair.Key is EditableSymbolProject project)
                           project.LocateSourceCodeFiles();
                   });

        foreach (var (symbolPackage, symbolUis) in loadedSymbolUis)
        {
            symbolPackage.RegisterUiSymbols(enableLog: false, symbolUis.NewlyLoaded, symbolUis.PreExisting);
        }
    }
    
    readonly struct SymbolUiLoadInfo(IReadOnlyCollection<SymbolUi> newlyLoaded, IReadOnlyCollection<SymbolUi> preExisting)
    {
        public readonly IReadOnlyCollection<SymbolUi> NewlyLoaded = newlyLoaded;
        public readonly IReadOnlyCollection<SymbolUi> PreExisting = preExisting;
    }

    readonly struct AssemblyConstructorInfo(AssemblyInformation assemblyInformation, Type instanceType)
    {
        public readonly AssemblyInformation AssemblyInformation = assemblyInformation;
        public readonly Type InstanceType = instanceType;
    }
}