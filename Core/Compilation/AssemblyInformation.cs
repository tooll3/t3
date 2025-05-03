#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using T3.Core.Logging;

namespace T3.Core.Compilation;

/// <summary>
/// This class is used as the primary entry point for loading assemblies and extracting information about the types within them.
/// This is where we find all of the operators and their slots, as well as any other type implementations that are relevant to tooll.
/// This is also where C# dependencies need to be resolved, which is why each instance of this class has a reference to a <see cref="T3AssemblyLoadContext"/>.
/// </summary>
public sealed partial class AssemblyInformation
{
    internal AssemblyInformation(AssemblyNameAndPath assemblyInfo)
    {
        Name = assemblyInfo.AssemblyName.Name ?? "Unknown Assembly Name";
        _assemblyName = assemblyInfo.AssemblyName;
        Directory = System.IO.Path.GetDirectoryName(assemblyInfo.Path)!;
        var loadContext = LoadContext;
        if (loadContext == null)
        {
            Log.Error($"{Name} Failed to get assembly load context in constructor");
        }
    }

    public readonly string Name;
    public readonly string Directory;

    private readonly AssemblyName _assemblyName;
    public event Action<AssemblyInformation>? Unloaded;
    public event Action<AssemblyInformation>? Loaded;

    internal const BindingFlags ConstructorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;

    public IReadOnlyDictionary<Guid, OperatorTypeInfo> OperatorTypeInfo => _operatorTypeInfo;
    private readonly ConcurrentDictionary<Guid, OperatorTypeInfo> _operatorTypeInfo = new();
    private Dictionary<string, Type>? _types;
    public IReadOnlySet<string> Namespaces => _namespaces;
    private readonly HashSet<string> _namespaces = [];

    internal bool ShouldShareResources;

    private T3AssemblyLoadContext? _loadContext;

    private readonly Lock _assemblyLock = new();

    private static readonly Dictionary<string, Type> _empty = new();

    private T3AssemblyLoadContext? LoadContext
    {
        get
        {
            lock (_assemblyLock)
            {
                if (_loadContext != null)
                    return _loadContext;

                try
                {
                    _loadContext = new T3AssemblyLoadContext(_assemblyName, Directory);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create assembly load context for {Name}\n{e.Message}\n{e.StackTrace}");
                    _loadContext = null;
                    return null;
                }

                _loadContext.UnloadTriggered += OnUnloadTriggered;

                try
                {
                    Loaded?.Invoke(this);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to invoke Loaded event for {Name}: {ex}");
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

            try
            {
                Unloaded?.Invoke(this);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to invoke Unloaded event for {Name}: {ex}");
            }
            
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
            var rootNode = LoadContext!.Root;
            if (rootNode != null && _types != null)
            {
                Log.Debug($"{Name}: Already loaded types");
                return true;
            }
            
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
                LoadTypes(types, rootNode.Assembly, out ShouldShareResources, _operatorTypeInfo, _namespaces, out _types);
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
        var assembly = LoadContext!.Root?.Assembly;

        if (assembly == null)
        {
            Log.Error($"Failed to get assembly for {Name}");
            return null;
        }

        return assembly.CreateInstance(constructorInfoInstanceType.FullName!);
    }
}