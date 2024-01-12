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
using T3.Editor.Gui.Dialog;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

internal static class EditorInitialization
{
    private static readonly List<EditableSymbolProject> EditableSymbolDatasList = new();
    public static readonly IReadOnlyList<EditableSymbolProject> EditableSymbolPackages = EditableSymbolDatasList;
    internal static bool NeedsUserProject;

    internal static void CreateOrMigrateProject(object sender, UserNameDialog.NameChangedEventArgs nameArgs)
    {
        var name = nameArgs.NewName;

        if (NeedsUserProject)
        {
            var success = !TryCreateProject(name, out var project);
            NeedsUserProject = success;
            if (success)
            {
                EditableSymbolDatasList.Add(project);
                EditableSymbolProject.ActiveProjectRw = project;
            }
        }
        else if (nameArgs.NewName != nameArgs.OldName)
        {
            var oldUserNamespace = $"user.{nameArgs.OldName}";
            var newUserNamespace = $"user.{nameArgs.NewName}";

            Log.Warning($"Have not implemented migration from {oldUserNamespace} to {newUserNamespace}");
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

        var compiled = Compiler.TryCompile(newCsProj, Compiler.BuildMode.Debug);

        if (!compiled)
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

        try
        {
            // todo: change to load CsProjs from specific directories and specific nuget packages from a package directory
            //var operatorAssemblies = RuntimeAssemblies.OperatorAssemblies;
            List<EditorSymbolPackage> operatorNugetPackages = new(); // "static" packages, remember to filter by operator vs non-operator assemblies

            stopwatch.Start();
            var coreAssemblyDirectory = Path.Combine(RuntimeAssemblies.CoreDirectory, "Operators"); // theoretically where the core libs assemblies will be
            var rootDirectories = new[] { coreAssemblyDirectory, UserSettings.Config.DefaultNewProjectDirectory };

            stopwatch.Stop();
            Log.Debug($"Core directories initialized in {stopwatch.ElapsedMilliseconds}ms");
            
            
            #if IDE
            // add lib projects
            stopwatch.Restart();
            string t3ParentDirectory = GetT3ParentDirectory();
            CreateSymlinks(t3ParentDirectory);
            
            stopwatch.Stop();
            Log.Debug($"Created symlinks in {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();

            var operatorFolder = Path.Combine(t3ParentDirectory, "Operators");
            rootDirectories = Directory.EnumerateDirectories(operatorFolder)
                                       .Where(path => !path.EndsWith("user"))
                                       .Concat(rootDirectories)
                                       .ToArray();

            stopwatch.Stop();
            Log.Debug($"Found {rootDirectories.Length} root directories in {stopwatch.ElapsedMilliseconds}ms");
            #endif


            stopwatch.Restart();
            var csProjFiles = rootDirectories
                             .Where(Directory.Exists)
                             .SelectMany(x => Directory.EnumerateFiles(x, "*.csproj", SearchOption.AllDirectories))
                             .ToArray();

            stopwatch.Stop();
            Log.Debug($"Found {csProjFiles.Length} csproj files in {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            ConcurrentBag<EditableSymbolProject> projects = new();
            ConcurrentBag<AssemblyInformation> nonOperatorAssemblies = new();
            csProjFiles
               .ToList()
               .ForEach(path =>
                        {
                            stopwatch.Restart();
                            var csProjFile = new CsProjectFile(new FileInfo(path));
                            var loaded = csProjFile.TryLoadAssembly(Compiler.BuildMode.Debug);
                            if (!loaded)
                            {
                                loaded = csProjFile.TryRecompile(Compiler.BuildMode.Debug);
                                if (!loaded)
                                {
                                    stopwatch.Stop();
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

                            stopwatch.Stop();
                            Log.Info($"Loaded {csProjFile.Name} in {stopwatch.ElapsedMilliseconds}ms");
                        });

            stopwatch.Stop();
            Log.Debug($"Loaded {projects.Count} projects and {nonOperatorAssemblies.Count} non-operator assemblies in {stopwatch.ElapsedMilliseconds}ms");
            
            var allSymbolPackages = projects
                                   .Concat(operatorNugetPackages)
                                   .ToArray();
            // Load operators
            stopwatch.Restart();
            InitializeCustomUis(nonOperatorAssemblies);
            stopwatch.Stop();
            Log.Debug($"Initialized custom uis in {stopwatch.ElapsedMilliseconds}ms");
            
            stopwatch.Restart();
            UpdateSymbolPackages(allSymbolPackages);
            stopwatch.Stop();
            Log.Debug($"Updated symbol packages in {stopwatch.ElapsedMilliseconds}ms");

            var activeProject = EditableSymbolProject.ActiveProject;
            // Create home
            if (activeProject == null || !activeProject.TryCreateHome())
            {
                NeedsUserProject = true;
            }

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
    private static void CreateSymlinks(string t3ParentDirectory)
    {
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
                                .Where(x => x.Name != "Editor")
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

    internal static void UpdateSymbolPackage(EditableSymbolProject project)
    {
        UpdateSymbolPackages(project);
    }

    private static void UpdateSymbolPackages(params EditorSymbolPackage[] symbolPackages)
    {
        ConcurrentDictionary<EditorSymbolPackage, List<SymbolJson.SymbolReadResult>> loadedSymbols = new();
        symbolPackages.AsParallel().ForAll(package => //pull out for non-editable ones too
                                           {
                                               package.LoadSymbols(false, out var newlyRead);
                                               loadedSymbols.TryAdd(package, newlyRead);
                                           });
        loadedSymbols.AsParallel().ForAll(pair => pair.Key.ApplySymbolChildren(pair.Value));

        ConcurrentDictionary<EditorSymbolPackage, IReadOnlyCollection<SymbolUi>> loadedSymbolUis = new();
        symbolPackages.AsParallel().ForAll(package =>
                                           {
                                               package.LoadUiFiles(loadedSymbols[package], out var newlyRead);
                                               loadedSymbolUis.TryAdd(package, newlyRead);
                                           });

        foreach (var (symbolPackage, symbolUis) in loadedSymbolUis)
        {
            symbolPackage.RegisterUiSymbols(enableLog: false, symbolUis);
            if (symbolPackage is EditableSymbolProject project)
                project.LocateSourceCodeFiles();
        }
    }

    readonly struct AssemblyConstructorInfo(AssemblyInformation assemblyInformation, Type instanceType)
    {
        public readonly AssemblyInformation AssemblyInformation = assemblyInformation;
        public readonly Type InstanceType = instanceType;
    }
}