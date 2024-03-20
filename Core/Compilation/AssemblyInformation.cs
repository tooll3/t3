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
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
namespace T3.Core.Compilation;

public readonly record struct InputSlotInfo(string Name, InputAttribute Attribute, Type Type, Type[] GenericArguments, FieldInfo Field, bool IsMultiInput);

public readonly record struct OperatorTypeInfo(Guid Id, List<InputSlotInfo> Inputs, List<OutputSlotInfo> Outputs, Func<object> Constructor, Type Type, FieldInfo StaticSymbolField, bool IsDescriptiveFileNameType);

public readonly record struct OutputSlotInfo(string Name, OutputAttribute Attribute, Type Type, Type[] GenericArguments, FieldInfo Field)
{
    public readonly Type? OutputDataType = GetOutputDataType(Type);

    private static Type? GetOutputDataType(Type fieldType)
    {
        var interfaces = fieldType.GetInterfaces();
        Type foundInterface = null;
        foreach (var i in interfaces)
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOutputDataUser<>))
            {
                foundInterface = i;
                break;
            }
        }

        return foundInterface?.GetGenericArguments().Single();
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
    public bool IsOperatorAssembly => _operatorTypeInfo.Count > 0;
    
    private readonly AssemblyName _assemblyName;
    private readonly Assembly _assembly;

    internal const BindingFlags ConstructorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    public IReadOnlyDictionary<Guid, OperatorTypeInfo> OperatorTypeInfo => _operatorTypeInfo;
    private readonly ConcurrentDictionary<Guid, OperatorTypeInfo> _operatorTypeInfo = new();
    private Dictionary<string, Type> _types;

    internal readonly bool ShouldShareResources;
    
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
        
        _types.Values.AsParallel().ForAll(type =>
                                          {
                                              var isOperator = type.IsAssignableTo(typeof(Instance));
                                              if (!isOperator)
                                                  nonOperatorTypes.Add(type);
                                              else
                                                  SetUpOperatorType(type);
                                              
                                          });

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
        var gotGuid = TryGetGuidOfType(type, out var id);
        var isHome = type.GetCustomAttribute<HomeAttribute>() is not null;

        if (!gotGuid)
        {
            Log.Error($"Failed to get guid for {type.FullName}");
            return;
        }
        
        if (isHome)
        {
            HomeGuid = id;
        }
        
        var constructor = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        
        // get static field to set the symbol to the instance type
        var genericType = typeof(Instance<>).MakeGenericType(type);
        var staticFieldInfos = genericType.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        var staticSymbolField = staticFieldInfos.Single(x => x.Name == "_typeSymbol");
        
        
        var isDescriptive = type.IsAssignableTo(typeof(IDescriptiveFilename));

        var bindFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        var slots = type.GetFields(bindFlags)
                        .Where(field => field.FieldType.IsAssignableTo(typeof(ISlot)));

        List<InputSlotInfo> inputFields = new();
        List<OutputSlotInfo> outputFields = new();
        foreach (var field in slots)
        {
            var fieldType = field.FieldType;
            var genericArguments = fieldType.GetGenericArguments();
            var name = field.Name;
            if (fieldType.IsAssignableTo(typeof(IInputSlot)))
            {
                var inputAttribute = field.GetCustomAttribute<InputAttribute>();
                if(inputAttribute is not null)
                {
                    var isMultiInput = fieldType.GetGenericTypeDefinition() == typeof(MultiInputSlot<>);
                    inputFields.Add(new InputSlotInfo(name, inputAttribute, fieldType, genericArguments, field, isMultiInput));
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
                    outputFields.Add(new OutputSlotInfo(name, outputAttribute, fieldType, genericArguments, field));
                }
                else
                {
                    Log.Error($"Output slot {field.Name} in {type.FullName} is missing {nameof(OutputAttribute)}");
                }
            }
        }

        var added = _operatorTypeInfo.TryAdd(id, new OperatorTypeInfo(
                                                       Id: id,
                                                       Type: type,
                                                       Constructor: constructor,
                                                       Inputs: inputFields,
                                                       Outputs: outputFields,
                                                       StaticSymbolField: staticSymbolField,
                                                       IsDescriptiveFileNameType: isDescriptive));

        if (!added)
        {
            Log.Error($"Failed to add operator type {type.FullName} with guid {id} because the id was already in use by {_operatorTypeInfo[id].Type.FullName}");
        }
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

    public void Unload()
    {
        _operatorTypeInfo.Clear();
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
}