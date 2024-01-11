using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;

namespace T3.Core.Compilation;

public class AssemblyInformation
{
    public readonly string Name;
    public readonly string Path;
    public readonly string Directory;
    public readonly AssemblyName AssemblyName;
    public readonly Assembly Assembly;

    public bool TryGetType(Guid typeId, out Type type) => _operatorTypes.TryGetValue(typeId, out type);

    public IReadOnlyDictionary<Guid, Type> OperatorTypes => _operatorTypes;
    private readonly Dictionary<Guid, Type> _operatorTypes;
    private readonly Dictionary<string, Type> _types;
    public IReadOnlyCollection<Type> Types => _types.Values;

    public Guid HomeGuid { get; private set; } = Guid.Empty;
    public bool HasHome => HomeGuid != Guid.Empty;
    
    private AssemblyLoadContext _loadContext;
    private ICompilationAssemblyResolver _assemblyResolver;

    public AssemblyInformation(string path, AssemblyName assemblyName, Assembly assembly)
    {
        Name = assemblyName.Name;
        Path = path;
        AssemblyName = assemblyName;
        Assembly = assembly;
        Directory = System.IO.Path.GetDirectoryName(path);

        TryResolveReferences(path);

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
                                           var gotGuid = TryGetGuidOfType(x, out var id);
                                           var isHome = x.GetCustomAttribute<HomeAttribute>() is not null;
                                           if (isHome && gotGuid)
                                           {
                                               HomeGuid = id;
                                           }

                                           return new GuidInfo(gotGuid, id, x);
                                       })
                               .Where(x => x.HasGuid)
                               .ToDictionary(x => x.Guid, x => x.Type);
    }

    /// <summary>
    /// Adapted from https://www.codeproject.com/Articles/1194332/Resolving-Assemblies-in-NET-Core
    /// </summary>
    /// <param name="path">path to dll</param>
    private void TryResolveReferences(string path)
    {
        var dependencyContext = DependencyContext.Load(Assembly);

        if (dependencyContext == null)
        {
            Log.Error($"Failed to load dependency context for assembly {Assembly.FullName}");
            return;
        }

        var resolvers = new ICompilationAssemblyResolver[]
                            {
                                new AppBaseCompilationAssemblyResolver(path),
                                new ReferenceAssemblyPathResolver(),
                                new PackageCompilationAssemblyResolver()
                            };
        _assemblyResolver = new CompositeCompilationAssemblyResolver(resolvers);
        _loadContext = AssemblyLoadContext.GetLoadContext(Assembly);

        if (_loadContext == null)
        {
            Log.Error($"Failed to get load context for assembly {Assembly.FullName}");
            return;
        }

        _loadContext.Resolving += OnResolving;
        return;

        Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            var library = dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);

            if (library == null)
            {
                return null;
            }

            var wrapper = new CompilationLibrary(
                                                 library.Type,
                                                 library.Name,
                                                 library.Version,
                                                 library.Hash,
                                                 library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                                                 library.Dependencies,
                                                 library.Serviceable);

            var assemblies = new List<string>();
            _assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
            switch (assemblies.Count)
            {
                case 0:
                    return null;
                case 1:
                    return context.LoadFromAssemblyPath(assemblies[0]);
                default:
                {
                    Log.Error($"Multiple assemblies found for {name.Name}");
                    foreach (var assembly in assemblies)
                        Log.Info($"\t{assembly}");
                    return null;
                }
            }

            bool NamesMatch(RuntimeLibrary runtime) => string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool TryGetGuidOfType(Type newType, out Guid guid)
    {
        var guidAttributes = newType.GetCustomAttributes(typeof(GuidAttribute), false);
        if (guidAttributes.Length == 0)
        {
            Log.Error($"Type {newType.Name} has no GuidAttribute");
            guid = Guid.Empty;
            return false;
        }

        if (guidAttributes.Length > 1)
        {
            Log.Error($"Type {newType.Name} has multiple GuidAttributes");
            guid = Guid.Empty;
            return false;
        }

        var guidAttribute = (GuidAttribute)guidAttributes[0];
        var guidString = guidAttribute.Value;

        if (!Guid.TryParse(guidString, out guid))
        {
            Log.Error($"Type {newType.Name} has invalid GuidAttribute");
            return false;
        }

        return true;
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