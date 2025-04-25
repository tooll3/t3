#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Core.Compilation;

/// <summary>
/// This class is used as the primary entry point for loading assemblies and extracting information about the types within them.
/// This is where we find all of the operators and their slots, as well as any other type implementations that are relevant to tooll.
/// This is also where C# dependencies need to be resolved, which is why each instance of this class has a reference to a <see cref="T3AssemblyLoadContext"/>.
/// </summary>
public sealed class AssemblyInformation
{
    internal AssemblyInformation(AssemblyNameAndPath assemblyInfo)
    {
        Name = assemblyInfo.AssemblyName.Name ?? "Unknown Assembly Name";
        _assemblyName = assemblyInfo.AssemblyName;
        Directory = System.IO.Path.GetDirectoryName(assemblyInfo.Path)!;
        IsEditorOnly = assemblyInfo.IsEditorOnly;
    }

    public readonly string Name;
    public readonly string Directory;

    private readonly AssemblyName _assemblyName;

    internal const BindingFlags ConstructorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    public IReadOnlyDictionary<Guid, OperatorTypeInfo> OperatorTypeInfo => _operatorTypeInfo;
    private readonly ConcurrentDictionary<Guid, OperatorTypeInfo> _operatorTypeInfo = new();
    private Dictionary<string, Type>? _types;
    public IReadOnlySet<string> Namespaces => _namespaces;
    private readonly HashSet<string> _namespaces = [];

    internal bool ShouldShareResources;
    public readonly bool IsEditorOnly;

    private T3AssemblyLoadContext? _loadContext;

    private readonly Lock _assemblyLock = new();

    private static readonly Dictionary<string, Type> _empty = new();

    private T3AssemblyLoadContext LoadContext
    {
        get
        {
            lock (_assemblyLock)
            {
                if (_loadContext != null)
                    return _loadContext;

                _loadContext = new T3AssemblyLoadContext(_assemblyName, Directory);
                _loadContext.UnloadTriggered += OnUnloadTriggered;

                foreach (var dependency in _packageDependencyAssemblies)
                {
                    _loadContext.AddDependencyContext(dependency.LoadContext);
                }

                return _loadContext;
            }
        }
    }

    private void OnUnloadTriggered(object? sender, EventArgs e)
    {
        lock (_assemblyLock)
        {
            _loadContext!.UnloadTriggered -= OnUnloadTriggered;
            _loadContext = null;
            _operatorTypeInfo.Clear();
            _types?.Clear(); // explicitly dereference all our types
            _types = null; // set collection to null to indicate that we need to reload the types todo: do better than this null check
            _namespaces.Clear();
        }
    }

    /// <summary>
    /// The entry point for loading the assembly and extracting information about the types within it - particularly the operators.
    /// However, loading an assembly's types in this way will also trigger the <see cref="T3AssemblyLoadContext"/> so that its dependencies are resolved.
    /// </summary>
    internal bool TryLoadTypes()
    {
        lock (_assemblyLock)
        {
            if (_operatorTypeInfo.Count > 0)
            {
                Log.Debug($"{Name}: Already loaded types");
                return true;
            }
            
            var rootNode = LoadContext.Root;
            if (rootNode == null)
            {
                Log.Error($"Failed to get assembly for {Name}");
                _types = null;
                ShouldShareResources = false;
                return false;
            }

            try
            {
                var types = rootNode.Assembly.GetTypes();
                LoadTypes(types, rootNode.Assembly, out ShouldShareResources, out _types);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to load types from assembly {rootNode.Assembly.FullName}\n{e.Message}\n{e.StackTrace}");
                _types = _empty; // this non-null value indicates that we have tried to load the types and none were found
                ShouldShareResources = false;
                return false;
            }
        }
    }

    public IEnumerable<Type> TypesInheritingFrom(Type type)
    {
        lock (_assemblyLock)
        {
            if (_types == null && !TryLoadTypes())
            {
                return [];
            }

            return _types!.Values.Where(t => t.IsAssignableTo(type));
        }
    }

    /// <summary>
    /// The routine that calls relevant methods to extract operator information from the types in the assembly and caches them.
    /// </summary>
    /// <param name="types"></param>
    /// <param name="assembly"></param>
    /// <param name="shouldShareResources"></param>
    /// <param name="typeDict"></param>
    private void LoadTypes(Type[] types, Assembly assembly, out bool shouldShareResources, out Dictionary<string, Type> typeDict)
    {
        if (!_operatorTypeInfo.IsEmpty)
        {
            throw new InvalidOperationException("Operator types already loaded");
        }
        
        var typesByName = new Dictionary<string, Type>();
        foreach (var type in types)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type == null)
            {
                Log.Error($"Null type in assembly {assembly.FullName}");
                continue;
            }

            var nsp = type.Namespace;
            if (nsp != null)
                _namespaces.Add(nsp);

            var name = type.FullName;
            if (name == null)
                continue;

            if (!typesByName.TryAdd(name, type))
            {
                Log.Warning($"Duplicate type {name} in assembly {assembly.FullName}");
            }
        }

        ConcurrentBag<Type> nonOperatorTypes = new();

        typesByName.Values.AsParallel().ForAll(type =>
                                               {
                                                   var isOperator = type.IsAssignableTo(typeof(Instance));
                                                   if (!isOperator)
                                                       nonOperatorTypes.Add(type);
                                                   else
                                                   {
                                                       try
                                                       {
                                                           SetUpOperatorType(type);
                                                       }
                                                       catch (Exception e)
                                                       {
                                                           Log.Error($"Failed to set up operator type {type.FullName}\n{e.Message}");
                                                       }
                                                   }
                                               });

        var assemblyLocation = assembly.Location;
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
                                                                                    assemblyFile: assemblyLocation,
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

        typeDict = typesByName;
    }

    /// <summary>
    /// Actually extracts operator information from the type - this is the meat and beans of how we get the operator's slots, implemented interfaces, etc
    /// </summary>
    private void SetUpOperatorType(Type type)
    {
        var gotGuid = TryGetGuidOfType(type, out var id);

        if (!gotGuid)
        {
            Log.Error($"Failed to get guid for {type.FullName}");
            return;
        }

        bool isGeneric = type.IsGenericTypeDefinition;

        var bindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static;

        var allMembers = type.GetMembers(bindFlags);
        var memberNames = new string[allMembers.Length];

        List<InputSlotInfo> inputFields = new();
        List<OutputSlotInfo> outputFields = new();

        for (int i = 0; i < allMembers.Length; i++)
        {
            var member = allMembers[i];
            var name = member.Name;
            memberNames[i] = name;

            if (member is not FieldInfo field || field.IsSpecialName)
            {
                continue;
            }

            var fieldType = field.FieldType;

            if (!fieldType.IsAssignableTo(typeof(ISlot)))
                continue;

            if (field.IsStatic)
            {
                Log.Error($"Static slot '{name}' in '{type.FullName}' is not allowed - please remove the static modifier");
                continue;
            }

            if (!field.IsInitOnly)
            {
                Log.Warning($"Slot '{name}' in '{type.FullName}' is not read-only - it is recommended to make slots read-only");
            }

            var genericArguments = fieldType.GetGenericArguments();
            if (fieldType.IsAssignableTo(typeof(IInputSlot)))
            {
                var inputAttribute = member.GetCustomAttribute<InputAttribute>();
                if (inputAttribute is null)
                {
                    Log.Error($"Input slot {name} in {type.FullName} is missing {nameof(InputAttribute)}");
                    continue;
                }

                var genericTypeDefinition = fieldType.GetGenericTypeDefinition();
                var isMultiInput = genericTypeDefinition == typeof(MultiInputSlot<>);

                int genericIndex = GetSlotGenericIndex(isGeneric, fieldType);
                inputFields.Add(new InputSlotInfo(name, inputAttribute, genericArguments, field, isMultiInput, genericIndex));
            }
            else
            {
                var outputAttribute = member.GetCustomAttribute<OutputAttribute>();
                if (outputAttribute is null)
                {
                    Log.Error($"Output slot {name} in {type.FullName} is missing {nameof(OutputAttribute)}");
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
            else if (interfaceType == typeof(IDescriptiveFilename))
            {
                isDescriptive = true;
            }
        }

        var added = _operatorTypeInfo.TryAdd(id, new OperatorTypeInfo(
                                                                      type: type,
                                                                      inputs: inputFields,
                                                                      isGeneric: isGeneric,
                                                                      outputs: outputFields,
                                                                      memberNames: memberNames,
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
    /// Tries to get the GuidAttribute from the given type. If the type has no GuidAttribute, or multiple GuidAttributes, this method will return false.
    /// Todo: this should support multiple guids for operator refactoring/replacement/deprecation purposes
    /// </summary>
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
                // this indicates there are multiple GuidAttributes on the type
                // we may want to support this at some point to allow for "refactoring" of operators
                // but it is not currently supported
                Log.Error($"Type {newType.Name} has multiple GuidAttributes");
                guid = Guid.Empty;
                return false;
        }
    }

    /// <summary>
    /// Does all within its power to unload the assembly from its load context.
    /// In order for an assembly to properly be unloaded, ALL references to it, including existing instances, references to its types, etc,
    /// must be released and dereferenced.
    /// </summary>
    public void Unload()
    {
        _loadContext?.BeginUnload();
    }

    /// <summary>
    /// Tries to get the release info for the package by looking for <see cref="RuntimeAssemblies.PackageInfoFileName"/> in the directory of the assembly.
    /// </summary>
    /// <param name="releaseInfo"></param>
    /// <returns></returns>
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

    public bool DependsOn(PackageWithReleaseInfo package)
    {
        if (!TryGetReleaseInfo(out var releaseInfo))
        {
            Log.Error($"Failed to get release info for {Name}");
            throw new InvalidOperationException($"Failed to get release info for {Name}");
        }

        // todo: this operator packages thing probably shouldnt be here - just assemblies.
        // UNLESS types are shared? how do we best manage this?
        foreach (var dependency in releaseInfo.OperatorPackages)
        {
            if (Matches(dependency, package.ReleaseInfo))
                return true;
        }

        var assembly = LoadContext.Root?.Assembly;

        if (assembly == null)
        {
            Log.Error($"Failed to get assembly for {Name}");
            return false;
        }

        var referencedAssemblies = assembly.GetReferencedAssemblies();
        foreach (var assemblyDependency in referencedAssemblies)
        {
            if (assemblyDependency.Name == package.ReleaseInfo.AssemblyFileName)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the given package reference matches the given release info.
    /// </summary>
    public static bool Matches(OperatorPackageReference reference, ReleaseInfo releaseInfo)
    {
        if (reference.ResourcesOnly)
            return false;

        var identity = reference.Identity;
        var assemblyFileName = releaseInfo.AssemblyFileName;

        // todo : version checks

        return identity.SequenceEqual(assemblyFileName);
    }

    /// <summary>
    /// Creates an instance of the given type using this assembly via (slow) reflection.
    /// </summary>
    public object? CreateInstance(Type constructorInfoInstanceType)
    {
        var assembly = LoadContext.Root?.Assembly;

        if (assembly == null)
        {
            Log.Error($"Failed to get assembly for {Name}");
            return null;
        }

        return assembly.CreateInstance(constructorInfoInstanceType.FullName!);
    }

    public void AddDependencyOn(AssemblyInformation dependency)
    {
        if (_packageDependencyAssemblies.Contains(dependency))
            return;

        Log.Info($"Adding dependency {dependency.Name} to {Name}");

        _packageDependencyAssemblies.Add(dependency);
        LoadContext.AddDependencyContext(dependency.LoadContext);
    }

    private readonly List<AssemblyInformation> _packageDependencyAssemblies = [];
}