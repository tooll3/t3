#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Core.Compilation;

public sealed class AssemblyInformation
{
    internal AssemblyInformation(AssemblyNameAndPath assemblyInfo)
    {
        Name = assemblyInfo.AssemblyName.Name ?? "Unknown Assembly Name";
        Path = assemblyInfo.Path;
        _assemblyName = assemblyInfo.AssemblyName;
        Directory = System.IO.Path.GetDirectoryName(Path)!;
        IsEditorOnly = assemblyInfo.IsEditorOnly;
    }

    public readonly string Name;
    public readonly string Path;
    public readonly string Directory;

    private readonly AssemblyName _assemblyName;

    internal const BindingFlags ConstructorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    public IReadOnlyDictionary<Guid, OperatorTypeInfo> OperatorTypeInfo => _operatorTypeInfo;
    private readonly ConcurrentDictionary<Guid, OperatorTypeInfo> _operatorTypeInfo = new();
    private Dictionary<string, Type>? _types;

    internal bool ShouldShareResources;
    public readonly bool IsEditorOnly;

    private T3AssemblyLoadContext? _loadContextUnsafe;

    private Assembly? _assemblyUnsafe;

    private readonly object _assemblyLock = new();

    private Assembly GetAssembly()
    {
        lock (_assemblyLock)
        {
            if (_assemblyUnsafe != null)
                return _assemblyUnsafe;

            var loadContext = GetLoadContext();
            var assembly = loadContext.LoadFromAssemblyPath(Path);

            _assemblyUnsafe = assembly;
            return _assemblyUnsafe;
        }
    }

    private T3AssemblyLoadContext GetLoadContext()
    {
        if (_loadContextUnsafe != null)
            return _loadContextUnsafe;

        _loadContextUnsafe = new T3AssemblyLoadContext(_assemblyName, Path);
        Log.Debug($"Created load context for {Directory}");
        return _loadContextUnsafe;
    }

    internal bool TryLoadTypes()
    {
        lock (_assemblyLock)
        {
            Type[] types;
            var assembly = GetAssembly();
            try
            {
                types = assembly.GetTypes();
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to load types from assembly {assembly.FullName}\n{e.Message}\n{e.StackTrace}");
                _types = new Dictionary<string, Type>();
                ShouldShareResources = false;
                return false;
            }

            LoadTypes(types, assembly, out ShouldShareResources, out _types);
            return true;
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

    private void LoadTypes(Type[] types, Assembly assembly, out bool shouldShareResources, out Dictionary<string, Type> typeDict)
    {
        var typesByName = new Dictionary<string, Type>();
        foreach (var type in types)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (type == null)
            {
                Log.Error($"Null type in assembly {assembly.FullName}");
                continue;
            }
            
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
        
        typeDict = typesByName;
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

        var bindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static;
        
        var allMembers = type.GetMembers(bindFlags);
        var memberNames = new string[allMembers.Length];

        List<InputSlotInfo> inputFields = new();
        List<OutputSlotInfo> outputFields = new();
        
        for(int i = 0; i < allMembers.Length; i++)
        {
            var member = allMembers[i];
            var name = member.Name;
            memberNames[i] = name;

            if (member is not FieldInfo field || field.IsSpecialName)
            {
                continue;
            }

            var fieldType = field.FieldType;
            
            if(!fieldType.IsAssignableTo(typeof(ISlot)))
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

    public void Unload()
    {
        _operatorTypeInfo.Clear();
        lock (_assemblyLock)
        {
            _types?.Clear();
            _assemblyUnsafe = null;

            var context = _loadContextUnsafe;
            if (context == null)
                return;

            // it's not on me to unload the context if it is not mine
            if (_loadContextOverridden)
            {
                _loadContextUnsafe = null;
                return;
            }

            context.Unload();
            _loadContextUnsafe = null;
        }
    }

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
        
        foreach(var dependency in releaseInfo.OperatorPackages)
        {
            if (Matches(dependency, package.ReleaseInfo))
                return true;
        }
        
        foreach(var assemblyDependency in GetAssembly().GetReferencedAssemblies())
        {
            if (assemblyDependency.Name == package.ReleaseInfo.AssemblyFileName)
                return true;
        }

        return false;
    }

    // todo - should this relationship be inverted? should the load contexts
    // of the operator assemblies be responsible for loading the editor assemblies? that way 
    // one operator assembly can load multiple editor assemblies
    // probably not, editor references etc
    public bool ReplaceResolversOf(IReadOnlyList<PackageWithReleaseInfo> packages)
    {
        var loadContext = GetLoadContext();

        var matched = false;

        foreach (var package in packages)
        {
            if (!DependsOn(package))
                continue;

            matched = true;
            var assemblyInformation = package.Package.AssemblyInformation;

            assemblyInformation.ReplaceLoadContextWith(loadContext);
            loadContext.AddAssemblyPath(package.Package, assemblyInformation.Path);
        }

        return matched;
    }

    private void ReplaceLoadContextWith(T3AssemblyLoadContext loadContext)
    {
        Log.Debug($"Replacing load context for {Name} with {loadContext.Name}");
        _loadContextUnsafe = loadContext;
        _loadContextOverridden = true;
    }

    public static bool Matches(OperatorPackageReference reference, ReleaseInfo releaseInfo)
    {
        if (reference.ResourcesOnly)
            return false;
        
        var identity = reference.Identity;
        var assemblyFileName = releaseInfo.AssemblyFileName;

        // todo : version checks

        return identity.SequenceEqual(assemblyFileName);
    }

    private bool _loadContextOverridden;

    public object? CreateInstance(Type constructorInfoInstanceType)
    {
        var assembly = GetAssembly();
        return assembly.CreateInstance(constructorInfoInstanceType.FullName!);
    }
}