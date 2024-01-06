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
using T3.Core.UserData;
using T3.Editor.Gui.Dialog;
using T3.Editor.UiModel;

namespace T3.Editor.Compilation;

internal static class EditorInitialization
{
    private static readonly List<EditorSymbolPackage> EditorSymbolDatasList = new();
    public static IReadOnlyList<EditorSymbolPackage> UiSymbolDatas = EditorSymbolDatasList;
    internal static bool NeedsUserProject;

    internal static void CreateOrMigrateUser(object sender, UserNameDialog.NameChangedEventArgs nameArgs)
    {
        var name = nameArgs.NewName;

        if (NeedsUserProject)
        {
            var newProject = Compiler.CreateNewProject(name);
            if(newProject == null)
            {
                throw new Exception("Failed to create new project");
            }
            
            var compiled = Compiler.TryCompile(newProject, Compiler.BuildMode.Debug);
            
            if (!compiled)
            {
                throw new Exception("Failed to compile new project");
            }
            
            var newUiSymbolData = new EditableSymbolPackage(newProject);

            AddSymbolPackages(newUiSymbolData);
            if (!EditableSymbolPackage.TryCreateHome())
            {
                throw new Exception("Failed to create user home");
            }

            NeedsUserProject = false;
        }
        else if (nameArgs.NewName != nameArgs.OldName)
        {
            var oldUserNamespace = $"user.{nameArgs.OldName}";
            var newUserNamespace = $"user.{nameArgs.NewName}";

            Log.Warning($"Have not implemented migration from {oldUserNamespace} to {newUserNamespace}");
        }
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
    internal static bool TryInitialize(out Exception exception)
    {
        UiRegistration.RegisterUiTypes();

        try
        {
            var operatorAssemblies = RuntimeAssemblies.OperatorAssemblies;
            List<EditableSymbolPackage> editablePackages = new();
            List<EditorSymbolPackage> staticPackages = new();

            foreach (var assemblyInfo in operatorAssemblies)
            {
                if (TryFindMatchingCSProj(assemblyInfo, out var csprojFile))
                {
                    var editableSymbolData = new EditableSymbolPackage(csprojFile);
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

            // Initialize custom UIs
            var uiInitializerTypes = RuntimeAssemblies.AllAssemblies
                                                      .Where(x => x.Name != "Editor")
                                                      .ToArray()
                                                      .AsParallel()
                                                      .SelectMany(assemblyInfo => assemblyInfo.Types
                                                                                              .Where(type =>
                                                                                                         type.IsAssignableTo(typeof(IOperatorUIInitializer)))
                                                                                              .Select(type => new AssemblyConstructorInfo(assemblyInfo, type)));

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

            // Create home
            if (!EditableSymbolPackage.TryCreateHome())
            {
                NeedsUserProject = true;
            }

            Log.Debug($"Loaded {UiSymbolDatas.Count} UI datas.");
            exception = null;
            return true;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
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
            var fileFound = Directory.EnumerateFiles(workingDirectory.FullName, csprojName).FirstOrDefault();
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
        EditorSymbolDatasList.AddRange(symbolPackages);

        ConcurrentDictionary<EditorSymbolPackage, List<SymbolJson.SymbolReadResult>> loadedSymbols = new();
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

    readonly struct AssemblyConstructorInfo
    {
        public readonly AssemblyInformation AssemblyInformation;
        public readonly Type InstanceType;

        public AssemblyConstructorInfo(AssemblyInformation assemblyInformation, Type instanceType)
        {
            AssemblyInformation = assemblyInformation;
            InstanceType = instanceType;
        }
    }
}