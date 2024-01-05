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

public static class EditorInitialization
{
    private static readonly List<UiSymbolData> UiSymbolDatasEditable = new();
    public static IReadOnlyList<UiSymbolData> UiSymbolDatas = UiSymbolDatasEditable;
    internal static bool NeedsUserProject;

    internal static void CreateOrMigrateUser(object sender, UserNameDialog.NameChangedEventArgs nameArgs)
    {
        var name = nameArgs.NewName;
        string destinationDirectory = Path.Combine(SymbolData.OperatorDirectoryName, "user", name);

        if (NeedsUserProject)
        {
            var defaultHomeDir = Path.Combine(UserData.RootFolder, "default-home");
            var files = Directory.EnumerateFiles(defaultHomeDir, "*");
            destinationDirectory = Path.GetFullPath(destinationDirectory);
            Directory.CreateDirectory(destinationDirectory);

            var dependenciesDirectory = Path.Combine(destinationDirectory, "dependencies");
            Directory.CreateDirectory(dependenciesDirectory);

            string placeholderDependencyPath = Path.Combine(dependenciesDirectory, "PlaceNativeDllDependenciesHere.txt");
            File.Create(placeholderDependencyPath).Dispose();

            const string namePlaceholder = "{{USER}}";
            const string guidPlaceholder = "{{GUID}}";
            string homeGuid = UiSymbolData.HomeSymbolId.ToString();
            foreach (var file in files)
            {
                string text = File.ReadAllText(file);
                text = text.Replace(namePlaceholder, name)
                           .Replace(guidPlaceholder, homeGuid);

                var destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(file));
                destinationFilePath = destinationFilePath.Replace(namePlaceholder, name)
                                                         .Replace(guidPlaceholder, homeGuid);

                File.WriteAllText(destinationFilePath, text);
            }

            // Todo: add solution reference to project

            // build destinationDirectory/name.csproj with dotnet build
            var process = new Process
                              {
                                  StartInfo = new ProcessStartInfo
                                                  {
                                                      FileName = "dotnet",
                                                      Arguments = $"build {name}.csproj",
                                                      WorkingDirectory = destinationDirectory,
                                                      UseShellExecute = true
                                                  }
                              };

            process.Start();
            process.WaitForExit();
            
            //todo - check dotnet output for success - exit codes? parse output?

            var newOperatorAssemblies = RuntimeAssemblies.LoadNewOperatorAssemblies(destinationDirectory);

            List<UiSymbolData> newUiSymbolDatas = new();
            foreach (var assembly in newOperatorAssemblies)
            {
                var newUiSymbolData = new UiSymbolData(assembly);
                newUiSymbolDatas.Add(newUiSymbolData);
            }

            AddUiSymbolData(newUiSymbolDatas);
            if (!UiSymbolData.TryCreateHome())
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
        try
        {
            var operatorAssemblies = RuntimeAssemblies.OperatorAssemblies;

            // Select only Operator assemblies
            var uiSymbolData = operatorAssemblies
                              .Select(a => new UiSymbolData(a))
                              .ToList();

            // Load operators
            AddUiSymbolData(uiSymbolData);

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
                    var activated =Activator.CreateInstanceFrom(assemblyName, typeName);
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
            if (!UiSymbolData.TryCreateHome())
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

    private static void AddUiSymbolData(List<UiSymbolData> uiSymbolDatas)
    {
        UiSymbolDatasEditable.AddRange(uiSymbolDatas);

        ConcurrentDictionary<UiSymbolData, List<SymbolJson.SymbolReadResult>> loadedSymbols = new();
        uiSymbolDatas.AsParallel().ForAll(uiSymbolData =>
                                          {
                                              uiSymbolData.LoadSymbols(false, out var list);
                                              loadedSymbols.TryAdd(uiSymbolData, list);
                                          });
        loadedSymbols.AsParallel().ForAll(pair => pair.Key.ApplySymbolChildren(pair.Value));
        uiSymbolDatas.AsParallel().ForAll(uiSymbolData => uiSymbolData.LoadUiFiles());

        foreach (var uiSymbolData in uiSymbolDatas)
        {
            uiSymbolData.RegisterUiSymbols(enableLog: false);
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