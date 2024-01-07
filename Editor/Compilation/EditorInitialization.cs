using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            NeedsUserProject = !TryCreateProject(name);
        }
        else if (nameArgs.NewName != nameArgs.OldName)
        {
            var oldUserNamespace = $"user.{nameArgs.OldName}";
            var newUserNamespace = $"user.{nameArgs.NewName}";

            Log.Warning($"Have not implemented migration from {oldUserNamespace} to {newUserNamespace}");
        }
    }

    private static bool TryCreateProject(string name)
    {
        var newProject = CsProjectFile.CreateNewProject(name, UserSettings.Config.DefaultNewProjectDirectory);
        if(newProject == null)
        {
            Log.Error("Failed to create new project");
            return false;
        }
            
        var compiled = Compiler.TryCompile(newProject, Compiler.BuildMode.Debug);
            
        if (!compiled)
        {
            Log.Error("Failed to compile new project");
            return false;
        }
            
        if(!newProject.Assembly.HasHome)
        {
            Log.Error("Failed to create project home");
            return false;
        }
            
        var newUiSymbolData = new EditableSymbolProject(newProject);

        AddSymbolPackages(newUiSymbolData);
        if (!newUiSymbolData.TryCreateHome())
        {
            Log.Error("Failed to create project home");
            RemoveSymbolPackage(newUiSymbolData);
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
        UiRegistration.RegisterUiTypes();

        try
        {
            var operatorAssemblies = RuntimeAssemblies.OperatorAssemblies;
            List<EditableSymbolProject> editablePackages = new();
            List<EditorSymbolPackage> staticPackages = new();

            foreach (var assemblyInfo in operatorAssemblies)
            {
                if (TryFindMatchingCSProj(assemblyInfo, out var csprojFile))
                {
                    var editableSymbolData = new EditableSymbolProject(csprojFile);
                    editablePackages.Add(editableSymbolData);
                }
                else
                {
                    var staticSymbolData = new EditorSymbolPackage(assemblyInfo);
                    staticPackages.Add(staticSymbolData);
                }
            }

            var allSymbolPackages = editablePackages
                                .Concat(staticPackages)
                                .ToArray();
            // Load operators
            AddSymbolPackages(allSymbolPackages);

            InitializeCustomUis();

            // Create home
            if (!EditableSymbolProject.ActiveProject.TryCreateHome())
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

    private static void InitializeCustomUis()
    {
        var uiInitializerTypes = RuntimeAssemblies.AllAssemblies
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

    private static bool TryFindMatchingCSProj(AssemblyInformation assembly, out CsProjectFile csprojFile)
    {
        var assemblyNameString = assembly.Name;
        if (assemblyNameString == null)
            throw new ArgumentException("Assembly name is null", nameof(assembly));

        var assemblyDirectory = assembly.Directory;

        // search upwards from the assembly directory to find a matching csproj
        var csprojName = $"{assemblyNameString}.csproj";

        var workingDirectory = Directory.GetParent(assemblyDirectory);
        FileInfo csprojFileInfo = null;

        while (workingDirectory != null)
        {
            var fileFound = Directory.EnumerateFiles(workingDirectory.FullName, csprojName)
                                     .FirstOrDefault();
            if (fileFound != null)
            {
                csprojFileInfo = new FileInfo(fileFound);
                break;
            }

            workingDirectory = workingDirectory.Parent;
        }

        if (csprojFileInfo == null)
        {
            csprojFile = null;
            return false;
        }

        csprojFile = new CsProjectFile(csprojFileInfo, assembly);
        return true;
    }

    private static void AddSymbolPackages(params EditorSymbolPackage[] symbolPackages)
    {
        ConcurrentDictionary<EditorSymbolPackage, List<SymbolPackage.SymbolJsonResult>> loadedSymbols = new();
        symbolPackages.AsParallel().ForAll(symbolPackage => //pull out for non-editable ones too
                                           {
                                               symbolPackage.LoadSymbols(false, out var list);
                                               loadedSymbols.TryAdd(symbolPackage, list);
                                           });
        loadedSymbols.AsParallel().ForAll(pair => pair.Key.ApplySymbolChildren(pair.Value));
        symbolPackages.AsParallel().ForAll(uiSymbolData => uiSymbolData.LoadUiFiles());

        foreach (var symbolPackage in symbolPackages)
        {
            symbolPackage.RegisterUiSymbols(enableLog: false);
        }
    }

    readonly struct AssemblyConstructorInfo(AssemblyInformation assemblyInformation, Type instanceType)
    {
        public readonly AssemblyInformation AssemblyInformation = assemblyInformation;
        public readonly Type InstanceType = instanceType;
    }
}