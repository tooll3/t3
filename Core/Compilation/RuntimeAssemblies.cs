using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic.ApplicationServices;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;

namespace T3.Core.Compilation;

public class RuntimeAssemblies
{
    public static readonly AssemblyInformation Core;
    public static readonly IReadOnlyList<AssemblyInformation> AllAssemblies;
    public static readonly IReadOnlyList<AssemblyInformation> DynamicallyLoadedAssemblies;
    public static readonly IReadOnlyList<AssemblyInformation> OperatorAssemblies;

    static RuntimeAssemblies()
    {
        var coreAssembly = typeof(RuntimeAssemblies).Assembly;
        var coreAssemblyName = coreAssembly.GetName();
        var path = coreAssembly.Location;
        Core = new AssemblyInformation(path, coreAssemblyName, coreAssembly);
        var baseAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                                      .Where(x => !x.IsDynamic)
                                      .Select(x => new AssemblyInformation(x.Location, x.GetName(), x))
                                      .ToArray();

        DynamicallyLoadedAssemblies = LoadAssemblies(baseAssemblies);

        OperatorAssemblies = DynamicallyLoadedAssemblies
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

        Log.Debug($"Loaded {DynamicallyLoadedAssemblies.Count} assemblies.");
        Log.Debug($"Loaded {OperatorAssemblies.Count} operator assemblies.");

        AllAssemblies = DynamicallyLoadedAssemblies
                       .Concat(baseAssemblies)
                       .ToArray();
    }

    static List<AssemblyInformation> LoadAssemblies(IEnumerable<AssemblyInformation> baseAssemblies)
    {
        Log.Debug($"Attempting to load operator assemblies...");
        var currentAssemblyFullNames = baseAssemblies.Select(x => x.AssemblyName.FullName).ToList();

        var workingDirectory = Directory.GetCurrentDirectory();

        try
        {
            return Directory.GetFiles(workingDirectory, "*.dll", SearchOption.AllDirectories)
                             //.Where(path => path.Contains(BinaryDirectory))
                             //.Where(path => !path.Contains("NDI") && !path.Contains("Spout"))
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
                                              && assemblyName.ProcessorArchitecture == Core.AssemblyName.ProcessorArchitecture
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

    public bool TryGetType(Guid typeId, out Type type) => _operatorTypes.TryGetValue(typeId, out type);

    private readonly Dictionary<Guid, Type> _operatorTypes;
    private readonly Dictionary<string, Type> _types;
    public IReadOnlyCollection<Type> Types => _types.Values;

    public AssemblyInformation(string path, AssemblyName assemblyName, Assembly assembly)
    {
        Name = assemblyName.Name;
        Path = path;
        AssemblyName = assemblyName;
        Assembly = assembly;
        try
        {
            _types = assembly.GetExportedTypes().ToDictionary(type => type.FullName, type => type);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load types from assembly {assembly.FullName}\n{e.Message}\n{e.StackTrace}");
            _types = new Dictionary<string, Type>();
            _operatorTypes = new Dictionary<Guid, Type>();
            return;
        }

        _operatorTypes = _types.Values
                               .Where(x => x.IsAssignableTo(typeof(Instance)))
                               .Select(x =>
                                       {
                                           var gotGuid = SymbolData.TryGetGuidOfType(x, out var id);
                                           return new GuidInfo(gotGuid, id, x);
                                       })
                               .Where(x => x.HasGuid)
                               .ToDictionary(x => x.Guid, x => x.Type);
    }

    public void UpdateType(Type updated, Guid guid = default)
    {
        _types[updated.FullName!] = updated;

        if (guid != default)
            _operatorTypes[guid] = updated;
    }

    readonly struct GuidInfo
    {
        public readonly bool HasGuid;
        public readonly Guid Guid;
        public readonly Type Type;

        public GuidInfo(bool hasGuid, Guid guid, Type type)
        {
            HasGuid = hasGuid;
            Guid = guid;
            this.Type = type;
        }
    }
}