using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core.Compilation;

public class RuntimeAssemblies
{
    public static readonly Assembly Core = typeof(RuntimeAssemblies).Assembly;
    public static readonly IReadOnlyList<AssemblyInformation> AllAssemblies;
    public static readonly IReadOnlyList<AssemblyInformation> OperatorAssemblies;

    static RuntimeAssemblies()
    {
        AllAssemblies = LoadAssemblies();
        OperatorAssemblies = AllAssemblies
                            .Where(assemblyInformation =>
                                   {
                                       try
                                       {
                                           return assemblyInformation.Assembly.DefinedTypes
                                                                     .Any(typeInfo => typeInfo.IsAssignableTo(typeof(Instance)));
                                       }
                                       catch (Exception e)
                                       {
                                           Log.Debug($"Failed to check if assembly '{assemblyInformation.Name}' ({assemblyInformation.Path}) " +
                                                     $"contains operator types.\n{e.Message}\n{e.StackTrace}");
                                           return false;
                                       }
                                   })
                            .ToList();
        
        Log.Debug($"Loaded {AllAssemblies.Count} assemblies.");
        Log.Debug($"Loaded {OperatorAssemblies.Count} operator assemblies.");
    }

    static List<AssemblyInformation> LoadAssemblies()
    {
        Log.Debug($"Attempting to load operator assemblies...");
        var currentAssemblyFullNames = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetName().FullName).ToList();
        var coreAssemblyName = Core.GetName();

        var workingDirectory = Directory.GetCurrentDirectory();

        try
        {
            return Directory.GetFiles(workingDirectory, "*.dll", SearchOption.AllDirectories)
                             //.Where(path => path.Contains(BinaryDirectory))
                            .Where(path => !path.Contains("NDI") && !path.Contains("Spout"))
                            .Select(path =>
                                    {
                                        AssemblyName assemblyName = null;
                                        try
                                        {
                                            assemblyName = AssemblyName.GetAssemblyName(path);
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Debug($"Failed to get assembly name for {path}\n{e.Message}\n{e.StackTrace}");
                                        }

                                        return new AssemblyNameAndPath()
                                                   {
                                                       AssemblyName = assemblyName,
                                                       Path = path
                                                   };
                                    })
                            .Where(nameAndPath =>
                                   {
                                       var assemblyName = nameAndPath.AssemblyName;
                                       return assemblyName != null
                                              && assemblyName.ProcessorArchitecture == coreAssemblyName.ProcessorArchitecture
                                              && !currentAssemblyFullNames.Contains(assemblyName.FullName);
                                   })
                            .Select(name =>
                                    {
                                        Log.Debug($"Attempting to load assembly at {name.Path}");
                                        try
                                        {
                                            var assembly = Assembly.LoadFile(name.Path);
                                            currentAssemblyFullNames.Add(name.AssemblyName.FullName);
                                            Log.Debug($"Loaded assembly {name.AssemblyName.FullName}");
                                            return new AssemblyInformation(name.Path, name.AssemblyName, assembly);
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Error($"Failed to load assembly {name.AssemblyName.FullName}\n{name.Path}\n{e.Message}\n{e.StackTrace}");
                                            return null;
                                        }
                                    })
                            .Where(info => info != null)
                            .ToList();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load assemblies\n{e.Message}\n{e.StackTrace}");
            return new List<AssemblyInformation>();
        }
    }

    private struct AssemblyNameAndPath
    {
        public AssemblyName AssemblyName;
        public string Path;
    }
}

public class AssemblyInformation
{
    public readonly string Name;
    public readonly string Path;
    public readonly AssemblyName AssemblyName;
    public readonly Assembly Assembly;
    
    public bool TryGetType(string typeName, out Type type) => _types.TryGetValue(typeName, out type);

    private readonly Dictionary<string, Type> _types;
    
    public IReadOnlyCollection<Type> Types => _types.Values;

    public AssemblyInformation(string path, AssemblyName assemblyName, Assembly assembly)
    {
        Name = assemblyName.Name;
        Path = path;
        AssemblyName = assemblyName;
        Assembly = assembly;
        _types = assembly.GetExportedTypes().ToDictionary(type => type.FullName, type => type);
    }

    public void UpdateType(Type updated)
    {
        _types[updated.FullName!] = updated;
    }
}