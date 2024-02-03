using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Core.Compilation;

public readonly struct InputSlotInfo(InputAttribute attribute, FieldInfo field)
{
    public readonly InputAttribute Attribute = attribute;
    public readonly FieldInfo Field = field;

    public void Deconstruct(out FieldInfo info, out InputAttribute attribute)
    {
        info = Field;
        attribute = Attribute;
    }
}

public struct OutputSlotInfo(OutputAttribute attribute, FieldInfo field)
{
    public readonly OutputAttribute Attribute = attribute;
    public readonly FieldInfo Field = field;
    
    public void Deconstruct(out FieldInfo info, out OutputAttribute attribute)
    {
        info = Field;
        attribute = Attribute;
    }
}

public class AssemblyInformation
{
    public readonly string Name;
    public readonly string Path;
    public readonly string Directory;
    private readonly AssemblyName _assemblyName;
    private readonly Assembly _assembly;

    public bool TryGetType(Guid typeId, out Type type) => _operatorTypes.TryGetValue(typeId, out type);

    public IReadOnlyDictionary<Guid, Type> OperatorTypes => _operatorTypes;
    private Dictionary<Guid, Type> _operatorTypes;
    private Dictionary<string, Type> _types;
    public IReadOnlyCollection<Type> Types => _types.Values;

    public Guid HomeGuid { get; private set; } = Guid.Empty;
    public bool HasHome => HomeGuid != Guid.Empty;

    public IEnumerable<Assembly> AllAssemblies => _loadContext.Assemblies;
    public IReadOnlyDictionary<Type, Func<object>> Constructors => _constructors;
    public IReadOnlyDictionary<Type,Type> GenericTypes => _genericTypes;
    public IReadOnlyDictionary<Type, IReadOnlyList<InputSlotInfo>> InputFields => _inputFields;
    public IReadOnlyDictionary<Type, IReadOnlyList<OutputSlotInfo>> OutputFields => _outputFields;
    public bool IsOperatorAssembly => _operatorTypes.Count > 0;

    private readonly List<string> _assemblyPaths = new();
    public IReadOnlyCollection<string> AssemblyPaths;
        

    public AssemblyInformation(string path, AssemblyName assemblyName, Assembly assembly, AssemblyLoadContext loadContext)
    {
        AssemblyPaths = _assemblyPaths;
        Name = assemblyName.Name;
        Path = path;
        _assemblyPaths.Add(path);
        _assemblyName = assemblyName;
        _assembly = assembly;
        Directory = System.IO.Path.GetDirectoryName(path);

        _loadContext = loadContext;

        LoadTypes(path, assembly, out ShouldShareResources);
    }

    private void LoadTypes(string path, Assembly assembly, out bool shouldShareResources)
    {
        TryResolveReferences(path);
        Type[] types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load types from assembly {assembly.FullName}\n{e.Message}\n{e.StackTrace}");
            _types = new Dictionary<string, Type>();
            _operatorTypes = new Dictionary<Guid, Type>();
            shouldShareResources = false;
            return;
        }
        
        _types = types.ToDictionary(type => type.FullName, type => type);

        ConcurrentBag<Type> nonOperatorTypes = new();
        _operatorTypes = _types.Values
                               .Where(type =>
                                      {
                                          var isOperator = type.IsAssignableTo(typeof(Instance));
                                          if (!isOperator)
                                              nonOperatorTypes.Add(type);
                                          else
                                              SetUpOperatorType(type);

                                          return isOperator;
                                      })
                               .Select(type =>
                                       {
                                           var gotGuid = TryGetGuidOfType(type, out var id);
                                           var isHome = type.GetCustomAttribute<HomeAttribute>() is not null;
                                           if (isHome && gotGuid)
                                           {
                                               HomeGuid = id;
                                           }

                                           return new GuidInfo(gotGuid, id, type);
                                       })
                               .Where(x => x.HasGuid)
                               .ToDictionary(x => x.Guid, x => x.Type);

        shouldShareResources = nonOperatorTypes
                              .Where(type =>
                                     {
                                         // check for shareable type
                                         if (!type.IsAssignableTo(typeof(IShareResources)))
                                         {
                                             return false;
                                         }

                                         try
                                         {
                                             var obj = Activator.CreateInstanceFrom(Path, type.FullName!);
                                             var unwrapped = obj?.Unwrap();
                                             if (unwrapped is IShareResources shareable)
                                             {
                                                 return shareable.ShouldShareResources;
                                             }

                                             Log.Error($"Failed to create {nameof(IShareResources)} for {type.FullName}");
                                         }
                                         catch (Exception e)
                                         {
                                             Log.Error($"Failed to create shareable resource for {type.FullName}\n{e.Message}");
                                         }

                                         return false;
                                     }).Any();
    }

    private void SetUpOperatorType(Type type)
    {
        _genericTypes[type] = typeof(Instance<>).MakeGenericType(type);
        _constructors[type] = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        

        var bindFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        var slots = type.GetFields(bindFlags)
                        .Where(field => field.FieldType.IsAssignableTo(typeof(ISlot)));

        List<InputSlotInfo> inputFields = new();
        List<OutputSlotInfo> outputFields = new();
        foreach (var field in slots)
        {
            if (field.FieldType.IsAssignableTo(typeof(IInputSlot)))
            {
                var inputAttribute = field.GetCustomAttribute<InputAttribute>();
                if(inputAttribute is not null)
                {
                    inputFields.Add(new InputSlotInfo(inputAttribute, field));
                }
                else
                {
                    Log.Error($"Input slot {field.Name} in {type.FullName} is missing {nameof(InputAttribute)}");
                }
            }
            else
            {
                var outputAttribute = field.GetCustomAttribute<OutputAttribute>();
                if(outputAttribute is not null)
                {
                    outputFields.Add(new OutputSlotInfo(outputAttribute, field));
                }
                else
                {
                    Log.Error($"Output slot {field.Name} in {type.FullName} is missing {nameof(OutputAttribute)}");
                }
            }
        }
        
        _inputFields[type] = inputFields;
        _outputFields[type] = outputFields;
    }

    /// <summary>
    /// Adapted from https://www.codeproject.com/Articles/1194332/Resolving-Assemblies-in-NET-Core
    /// </summary>
    /// <param name="path">path to dll</param>
    private void TryResolveReferences(string path)
    {
        if (DependencyContext == null)
        {
            Log.Error($"Failed to load dependency context for assembly {_assemblyName.FullName}");
            return;
        }

        var resolvers = new ICompilationAssemblyResolver[]
                            {
                                new AppBaseCompilationAssemblyResolver(path),
                                new ReferenceAssemblyPathResolver(),
                                new PackageCompilationAssemblyResolver()
                            };
        _assemblyResolver = new CompositeCompilationAssemblyResolver(resolvers);

        if (_loadContext == null)
        {
            Log.Error($"Failed to get load context for assembly {_assemblyName.FullName}");
            return;
        }

        _loadContext.Resolving += OnResolving;
        return;
    }

    private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        var library = DependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);

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
                var path = assemblies[0];
                _assemblyPaths.Add(path);
                return context.LoadFromAssemblyPath(path);
            default:
            {
                Log.Error($"Multiple assemblies found for {name.Name}");
                foreach (var assembly in assemblies)
                    Log.Info($"\t{assembly}");
                return null;
            }
        }

        bool NamesMatch(RuntimeLibrary runtime)
        {
            return string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(RemovePackageSuffixes(runtime.Name), name.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
    
    private static string RemovePackageSuffixes(string name)
    {
        string removed = name.Replace(".api", string.Empty);
        return removed;
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

    private readonly struct GuidInfo(bool hasGuid, Guid guid, Type type)
    {
        public readonly bool HasGuid = hasGuid;
        public readonly Guid Guid = guid;
        public readonly Type Type = type;
    }

    public void Unload()
    {
        _operatorTypes.Clear();
        _types.Clear();
        _loadContext.Resolving -= OnResolving;
        _loadContext.Unload();
        _loadContext = null;
    }

    private DependencyContext DependencyContext => _dependencyContext ??= DependencyContext.Load(_assembly);
    public readonly bool ShouldShareResources;
    private DependencyContext _dependencyContext;
    private readonly ConcurrentDictionary<Type, Func<object>> _constructors = new();
    private AssemblyLoadContext _loadContext;
    private CompositeCompilationAssemblyResolver _assemblyResolver;
    private readonly ConcurrentDictionary<Type, Type> _genericTypes = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<InputSlotInfo>> _inputFields = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<OutputSlotInfo>> _outputFields = new();
}