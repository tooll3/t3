#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

public readonly record struct InputSlotInfo(
    string Name,
    InputAttribute Attribute,
    Type[] GenericArguments,
    FieldInfo Field,
    bool IsMultiInput,
    int GenericTypeIndex)
{
    public bool IsGeneric => GenericTypeIndex >= 0;
}

public sealed class OperatorTypeInfo
{
    internal OperatorTypeInfo(List<InputSlotInfo> inputs,
                            List<OutputSlotInfo> outputs,
                            bool isGeneric,
                            Type type,
                            bool isDescriptiveFileNameType,
                            ExtractableTypeInfo extractableTypeInfo)
    {
        Inputs = inputs;
        Outputs = outputs;
        Type = type;
        IsDescriptiveFileNameType = isDescriptiveFileNameType;
        ExtractableTypeInfo = extractableTypeInfo;
        
        if (!isGeneric)
        {
            _nonGenericConstructor = Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
        }
        else
        {
            GenericArguments = type.GetGenericArguments();
        }
    }

    public readonly List<InputSlotInfo> Inputs;
    public readonly List<OutputSlotInfo> Outputs;
    public readonly Type[]? GenericArguments;
    public readonly Type Type;
    public readonly bool IsDescriptiveFileNameType;
    public readonly ExtractableTypeInfo ExtractableTypeInfo;
    
    private readonly Func<object>? _nonGenericConstructor;

    public Func<object> GetConstructor()
    {
        if (_nonGenericConstructor != null)
            return _nonGenericConstructor;
        throw new InvalidOperationException("Generic types must be provided for generic operators - use TryGetConstructor instead");
    }
    
    public bool TryGetConstructor([NotNullWhen(true)] out Func<object>? constructor, params Type[] genericArguments)
    {
        Type constructedType;
        try
        {
            constructedType = Type.MakeGenericType(genericArguments);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to create constructor for {Type.FullName}<{string.Join(", ", genericArguments.Select(t => t.FullName))}>\n{e.Message}");
            constructor = null;
            return false;
        }

        if (_genericConstructors.TryGetValue(constructedType, out constructor))
            return true;
        
        constructor = Expression.Lambda<Func<object>>(Expression.New(constructedType)).Compile();
        _genericConstructors.Add(constructedType, constructor);

        return true;
    }
    

    #region Not needed?

    private static bool TryExtractGenericInformationOf(Type type, 
                                                       [NotNullWhen(true)] out Type[]? genericParameters, 
                                                       [NotNullWhen(true)] out Dictionary<Type, Type[]>? genericTypeConstraints)
    {
        if (!type.IsGenericTypeDefinition)
        {
            genericParameters = null;
            genericTypeConstraints = null;
            return false;
        }
        
        genericParameters = type.GetGenericArguments();
        genericTypeConstraints = new Dictionary<Type, Type[]>();
        foreach (var genericParameter in genericParameters)
        {
            var constraints = genericParameter.GetGenericParameterConstraints();
            genericTypeConstraints.Add(genericParameter, constraints);
        }

        return true;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericArguments"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">If this method is called on a non-generic operator</exception>
    private bool CanCreateWithArguments(params Type[] genericArguments)
    {
        ArgumentNullException.ThrowIfNull(genericArguments);
        
        if(genericArguments.Length != GenericArguments!.Length)
            throw new InvalidOperationException($"GenericArguments must have {GenericArguments.Length} elements for operator {Type.FullName}");

        var genericTypes = GenericArguments!;
        var genericTypeCount = genericTypes.Length;
        if (genericArguments.Length != genericTypeCount)
            return false;

        for (int i = 0; i < genericTypeCount; i++)
        {
            if (!genericTypes[i].IsAssignableFrom(genericArguments[i]))
                return false;
        }

        return true;
    }
    
    #endregion
    
    private readonly Dictionary<Type, Func<object>> _genericConstructors = new();
}

public readonly record struct ExtractableTypeInfo(bool IsExtractable, Type? ExtractableType);

public readonly record struct OutputSlotInfo
{
    internal readonly Type? OutputDataType;

    internal OutputSlotInfo(string Name, OutputAttribute Attribute, Type type, Type[] GenericArguments, FieldInfo Field, int GenericTypeIndex)
    {
        this.Name = Name;
        this.Attribute = Attribute;
        this.GenericArguments = GenericArguments;
        this.Field = Field;
        this.GenericTypeIndex = GenericTypeIndex;
        OutputDataType = GetOutputDataType(type);
        IsGeneric = GenericTypeIndex >= 0;
    }

    public string Name { get; }
    public OutputAttribute Attribute { get; }
    public Type[] GenericArguments { get; }
    public FieldInfo Field { get; }

    public int GenericTypeIndex { get; }
    public bool IsGeneric { get; }

    private static Type? GetOutputDataType(Type fieldType)
    {
        var interfaces = fieldType.GetInterfaces();
        Type? foundInterface = null;
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

    public bool IsOperatorAssembly => _operatorTypeInfo.Count > 0;

    private readonly AssemblyName _assemblyName;
    private readonly Assembly _assembly;

    internal const BindingFlags ConstructorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    public IReadOnlyDictionary<Guid, OperatorTypeInfo> OperatorTypeInfo => _operatorTypeInfo;
    private readonly ConcurrentDictionary<Guid, OperatorTypeInfo> _operatorTypeInfo = new();
    private Dictionary<string, Type> _types;

    internal readonly bool ShouldShareResources;

    private IEnumerable<Assembly> AllAssemblies => _loadContext?.Assemblies ?? Enumerable.Empty<Assembly>();

    internal AssemblyInformation(string path, AssemblyName assemblyName, Assembly assembly, AssemblyLoadContext loadContext)
    {
        Name = assemblyName.Name ?? "Unknown Assembly Name";
        Path = path;
        _assemblyName = assemblyName;
        _assembly = assembly;
        Directory = System.IO.Path.GetDirectoryName(path)!;

        _loadContext = loadContext;

        if (!TryResolveReferences(path, out _assemblyResolver!))
        {
            throw new Exception($"Failed to load assembly {path}");
        }
        
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load types from assembly {assembly.FullName}\n{e.Message}\n{e.StackTrace}");
            _types = new Dictionary<string, Type>();
            ShouldShareResources = false;
            return;
        }
        
        LoadTypes(types, assembly, out ShouldShareResources, out _types);
    }

    public IEnumerable<Type> TypesInheritingFrom(Type type) => _types.Values.Where(t => t.IsAssignableTo(type));

    private void LoadTypes(Type[] types, Assembly assembly, out bool shouldShareResources, out Dictionary<string, Type> typeDict)
    {
        typeDict = new Dictionary<string, Type>();
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

        if (!gotGuid)
        {
            Log.Error($"Failed to get guid for {type.FullName}");
            return;
        }

        bool isGeneric = type.IsGenericTypeDefinition;

        var bindFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        var slots = type.GetFields(bindFlags)
                        .Where(field => field.FieldType.IsAssignableTo(typeof(ISlot)));

        List<InputSlotInfo> inputFields = new();
        List<OutputSlotInfo> outputFields = new();
        foreach (var field in slots)
        {
            var fieldType = field.FieldType;
            var name = field.Name;
            var genericArguments = fieldType.GetGenericArguments();
            if (fieldType.IsAssignableTo(typeof(IInputSlot)))
            {
                var inputAttribute = field.GetCustomAttribute<InputAttribute>();
                if (inputAttribute is null)
                {
                    Log.Error($"Input slot {field.Name} in {type.FullName} is missing {nameof(InputAttribute)}");
                    continue;
                }

                var genericTypeDefinition = fieldType.GetGenericTypeDefinition();
                var isMultiInput = genericTypeDefinition == typeof(MultiInputSlot<>);

                int genericIndex = GetSlotGenericIndex(isGeneric, fieldType);
                inputFields.Add(new InputSlotInfo(name, inputAttribute, genericArguments, field, isMultiInput, genericIndex));
            }
            else
            {
                var outputAttribute = field.GetCustomAttribute<OutputAttribute>();
                if (outputAttribute is null)
                {
                    Log.Error($"Output slot {field.Name} in {type.FullName} is missing {nameof(OutputAttribute)}");
                    continue;
                }

                var genericIndex = GetSlotGenericIndex(isGeneric, fieldType);
                outputFields.Add(new OutputSlotInfo(name, outputAttribute, fieldType, genericArguments, field, genericIndex));
            }
        }

        ExtractableTypeInfo extractableTypeInfo = default;
        var isDescriptive = false;

        // collect information about implemented interfaces
        var interfaces = type.GetInterfaces();
        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IExtractedInput<>))
            {
                var extractableType = interfaceType.GetGenericArguments().Single();
                extractableTypeInfo = new ExtractableTypeInfo(true, extractableType);
            }
            else if(interfaceType == typeof(IDescriptiveFilename))
            {
                isDescriptive = true;
            }
        }

        var added = _operatorTypeInfo.TryAdd(id, new OperatorTypeInfo(
                                                                      type: type,
                                                                      inputs: inputFields,
                                                                      isGeneric: isGeneric,
                                                                      outputs: outputFields,
                                                                      isDescriptiveFileNameType: isDescriptive,
                                                                      extractableTypeInfo: extractableTypeInfo));

        if (!added)
        {
            Log.Error($"Failed to add operator type {type.FullName} with guid {id} because the id was already in use by {_operatorTypeInfo[id].Type.FullName}");
        }

        return;

        static int GetSlotGenericIndex(bool isGeneric, Type fieldType)
        {
            int genericIndex = -1;
            if (isGeneric && fieldType.IsGenericTypeDefinition)
            {
                genericIndex = fieldType.GenericParameterPosition;
            }

            return genericIndex;
        }
    }

    /// <summary>
    /// Adapted from https://www.codeproject.com/Articles/1194332/Resolving-Assemblies-in-NET-Core
    /// </summary>
    /// <param name="path">path to dll</param>
    /// <param name="assemblyResolver"></param>
    private bool TryResolveReferences(string path, [NotNullWhen(true)] out CompositeCompilationAssemblyResolver? assemblyResolver)
    {
        _dependencyContext = DependencyContext.Load(_assembly);
        if (_dependencyContext == null)
        {
            Log.Error($"Failed to load dependency context for assembly {_assemblyName.FullName}");
            assemblyResolver = null;
            return false;
        }

        var resolvers = new ICompilationAssemblyResolver[]
                            {
                                new AppBaseCompilationAssemblyResolver(path),
                                new ReferenceAssemblyPathResolver(),
                                new PackageCompilationAssemblyResolver()
                            };
        assemblyResolver = new CompositeCompilationAssemblyResolver(resolvers);

        if (_loadContext == null)
        {
            Log.Error($"Failed to get load context for assembly {_assemblyName.FullName}");
            return false;
        }

        _loadContext.Resolving += OnResolving;
        return true;
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
        if (_dependencyContext != null)
            _loadContext.Resolving -= OnResolving;

        _loadContext.Unload();
        _loadContext = null;
    }

    private DependencyContext? _dependencyContext;
    private AssemblyLoadContext? _loadContext;
    private readonly CompositeCompilationAssemblyResolver _assemblyResolver;

    public bool TryGetReleaseInfo([NotNullWhen(true)] out ReleaseInfo? releaseInfo)
    {
        var releaseInfoPath = System.IO.Path.Combine(Directory, RuntimeAssemblies.PackageInfoFileName);
        if (RuntimeAssemblies.TryLoadReleaseInfo(releaseInfoPath, out releaseInfo))
        {
            if (!releaseInfo.Version.Matches(_assemblyName.Version))
            {
                Log.Warning($"ReleaseInfo version does not match assembly version. " +
                            $"Assembly: {_assemblyName.FullName}, {_assemblyName.Version}\n" +
                            $"ReleaseInfo: {releaseInfo.Version}");
            }

            return true;
        }

        releaseInfo = null;
        return false;
    }
}

