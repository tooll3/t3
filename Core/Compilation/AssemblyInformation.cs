#nullable enable
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

internal readonly struct InputSlotInfo(InputAttribute attribute, FieldInfo field)
{
    public readonly InputAttribute Attribute = attribute;
    public readonly FieldInfo Field = field;

    public void Deconstruct(out FieldInfo info, out InputAttribute attribute)
    {
        info = Field;
        attribute = Attribute;
    }
}

internal readonly struct OutputSlotInfo(OutputAttribute attribute, FieldInfo field)
{
    public readonly OutputAttribute Attribute = attribute;
    public readonly FieldInfo Field = field;
    
    public void Deconstruct(out FieldInfo info, out OutputAttribute attribute)
    {
        info = Field;
        attribute = Attribute;
    }
}

public sealed class AssemblyInformation
{
    public readonly string Name;
    public readonly string Path;
    public readonly string Directory;
    public readonly IReadOnlyCollection<string> AssemblyPaths;

    public Guid HomeGuid { get; private set; } = Guid.Empty;
    public bool HasHome => HomeGuid != Guid.Empty;
    public bool IsOperatorAssembly => _operatorTypes.Count > 0;
    
    private readonly AssemblyName _assemblyName;
    private readonly Assembly _assembly;

    internal const BindingFlags ConstructorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    internal bool TryGetType(Guid typeId, out Type? type) => _operatorTypes.TryGetValue(typeId, out type);

    internal IReadOnlyDictionary<Guid, Type> OperatorTypes => _operatorTypes;
    private Dictionary<Guid, Type> _operatorTypes;
    private Dictionary<string, Type> _types;

    internal readonly bool ShouldShareResources;
    internal IReadOnlyDictionary<Type, Func<object>> Constructors => _constructors;
    internal IReadOnlyDictionary<Type,Type> GenericTypes => _genericTypes;
    internal IReadOnlyDictionary<Type, IReadOnlyList<InputSlotInfo>> InputFields => _inputFields;
    internal IReadOnlyDictionary<Type, IReadOnlyList<OutputSlotInfo>> OutputFields => _outputFields;

    private IEnumerable<Assembly> AllAssemblies => _loadContext?.Assemblies ?? Enumerable.Empty<Assembly>();
    private readonly List<string> _assemblyPaths = [];

    internal AssemblyInformation(string path, AssemblyName assemblyName, Assembly assembly, AssemblyLoadContext loadContext)
    {
        AssemblyPaths = _assemblyPaths;
        Name = assemblyName.Name ?? "Unknown Assembly Name";
        Path = path;
        _assemblyPaths.Add(path);
        _assemblyName = assemblyName;
        _assembly = assembly;
        Directory = System.IO.Path.GetDirectoryName(path)!;

        _loadContext = loadContext;

        LoadTypes(path, assembly, out ShouldShareResources);
    }
    
    public IEnumerable<Type> TypesInheritingFrom(Type type) => _types.Values.Where(t => t.IsAssignableTo(type));

    private void LoadTypes(string path, Assembly assembly, out bool shouldShareResources)
    {
        TryResolveReferences(path);
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load types from assembly {assembly.FullName}\n{e.Message}\n{e.StackTrace}");
            _types = new Dictionary<string, Type>();
            _operatorTypes = new Dictionary<Guid, Type>();
            shouldShareResources = false;
            return;
        }

        var typeDict = new Dictionary<string, Type>();
        foreach (var type in types)
        {
            if (!typeDict.TryAdd(type.FullName!, type))
            {
                Log.Warning($"Duplicate type {type.FullName} in assembly {assembly.FullName}");
            }
        }
        
        _types = typeDict;

        ConcurrentBag<Type> nonOperatorTypes = new();
        var opGuidInfoEnumerable = _types.Values
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
                               .Where(x => x.HasGuid);

        _operatorTypes = new Dictionary<Guid, Type>(_types.Count);
        foreach (var op in opGuidInfoEnumerable)
        {
            if (!_operatorTypes.TryAdd(op.Guid, op.Type))
            {
                var existingType = _operatorTypes[op.Guid];
                throw new Exception($"Duplicate operator type {op.Type.FullName} with guid {op.Guid}. Existing type: {existingType.FullName}");
            }
        }
        
        _operatorTypes.TrimExcess();

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
                                             var obj = Activator.CreateInstanceFrom(
                                                                                    assemblyFile: Path, 
                                                                                    typeName: type.FullName!, 
                                                                                    ignoreCase: false, 
                                                                                    bindingAttr: ConstructorBindingFlags, 
                                                                                    binder: null, args: null, culture: null, activationAttributes: null);
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
        _dependencyContext = DependencyContext.Load(_assembly); 
        if (_dependencyContext == null)
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
    }

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        if (!TryFindLibrary(name, _dependencyContext!, out var library))
        {
            return null;
        }

        var wrapper = new CompilationLibrary(
                                             library!.Type,
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

        static bool TryFindLibrary(AssemblyName name, DependencyContext dependencyContext, out RuntimeLibrary? library)
        {
            var nameToSearchFor = name.Name;
            foreach (var runtime in dependencyContext.RuntimeLibraries)
            {
                if (string.Equals(runtime.Name, nameToSearchFor, StringComparison.OrdinalIgnoreCase))
                {
                    library = runtime;
                    return true;
                }

                var assemblyGroups = runtime.RuntimeAssemblyGroups;
                foreach (var assemblyGroup in assemblyGroups)
                {
                    var runtimeFiles = assemblyGroup.RuntimeFiles;
                    foreach (var runtimeFile in runtimeFiles)
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(runtimeFile.Path);
                        if (string.Equals(fileName, nameToSearchFor, StringComparison.Ordinal))
                        {
                            library = runtime;
                            return true;
                        }
                    }
                }
            }

            library = default;
            return false;
        }
    }

    private static bool TryGetGuidOfType(Type newType, out Guid guid)
    {
        var guidAttributes = newType.GetCustomAttributes(typeof(GuidAttribute), false);
        switch (guidAttributes.Length)
        {
            case 0:
                Log.Error($"Type {newType.Name} has no GuidAttribute");
                guid = Guid.Empty;
                return false;
            
            case 1: // This is what we want - types with a single GuidAttribute
                var guidAttribute = (GuidAttribute)guidAttributes[0];
                var guidString = guidAttribute.Value;

                if (!Guid.TryParse(guidString, out guid))
                {
                    Log.Error($"Type {newType.Name} has invalid GuidAttribute");
                    return false;
                }

                return true;
            default:
                Log.Error($"Type {newType.Name} has multiple GuidAttributes");
                guid = Guid.Empty;
                return false;
        }
    }

    private readonly record struct GuidInfo(bool HasGuid, Guid Guid, Type Type);

    public void Unload()
    {
        _operatorTypes.Clear();
        _types.Clear();

        if (_loadContext == null)
            return;
        
        // because we only subscribe to the Resolving event once we've found the dependency context
        if(_dependencyContext != null)
            _loadContext.Resolving -= OnResolving;
        
        _loadContext.Unload();
        _loadContext = null;
    }

    private DependencyContext? _dependencyContext;
    private readonly ConcurrentDictionary<Type, Func<object>> _constructors = new();
    private AssemblyLoadContext? _loadContext;
    private CompositeCompilationAssemblyResolver _assemblyResolver;
    private readonly ConcurrentDictionary<Type, Type> _genericTypes = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<InputSlotInfo>> _inputFields = new();
    private readonly ConcurrentDictionary<Type, IReadOnlyList<OutputSlotInfo>> _outputFields = new();
}