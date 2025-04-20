#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using T3.Core.Logging;

namespace T3.Core.Compilation;

/// <summary>
/// This is the actual place where assemblies are loaded and dependencies are resolved on a per-dll level.
/// Inheriting from <see cref="AssemblyLoadContext"/> allows us to load assemblies in a custom way, which is required
/// as assemblies are loaded from different locations for each package.
///
/// Each package has its own <see cref="T3AssemblyLoadContext"/> that is used to load the assemblies of that package. If a package relies on another package
/// from a CSProj-level, the dependency's load context and dlls are added to the dependent's load context such that the dependent's dlls can be loaded
/// referencing the types provided by the dependency.
///
/// For example, the LibEditor package has a dependency on Lib. When LibEditor is loaded, the Lib package is loaded first via LibEditor's load context. Then
/// the loading procedure continues until LibEditor is fully loaded with all its dependencies.
///
/// Unfortunately this process is very complex, and is not thoroughly tested with large dependency chains.
/// </summary>
internal sealed class T3AssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _path;
    private Assembly? _myAssembly;
    
    private readonly object _assemblyLock = new();
    private CompositeCompilationAssemblyResolver? _assemblyResolver;
    private readonly Dictionary<object, string> _assemblyPaths = new();
    
    // this is an ultimate list of assemblies that have been loaded for this context
    // keeping this cache allows us to avoid stack overflows during loading, and also
    // speeds up the loading process.
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

    internal T3AssemblyLoadContext(AssemblyName assemblyName, string path) : base(assemblyName.Name, true)
    {
        _path = path;
        Resolving += OnResolving;
        _assemblyPaths[this] = path;

        lock (_assemblyLock)
        {
            foreach (var assembly in RuntimeAssemblies.CoreAssemblies)
            {
                AddToLoadedAssemblies(assembly, true, debug: false);
                //CollectReferencedAssembliesOf(assembly, Default);
            }
        }
    }

    /// <summary>
    /// This function is used to establish dependencies between packages. 
    /// </summary>
    /// <param name="packageOwner"></param>
    /// <param name="assemblyInformationPath"></param>
    internal void AddAssemblyPath(object packageOwner, string assemblyInformationPath)
    {
        lock (_assemblyLock)
        {
            _assemblyPaths[packageOwner] = assemblyInformationPath;
            _assemblyResolver = null;
        }
    }

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        lock (_assemblyLock)
        {
            if (TryGetLoadedAssembly(name, context, out var alreadyLoadedAssembly))
                return alreadyLoadedAssembly;

            var nameToSearchFor = name.Name;

            if (_myAssembly == null)
            {
                Log.Debug( "T3AssemblyLoadContext > LoadFromAssemblyPath " + _path);
                _myAssembly = LoadFromAssemblyPath(_path);
                CollectReferencedAssembliesOf(_myAssembly, this);
                if (nameToSearchFor == _myAssembly.GetName().Name)
                    return _myAssembly;
            }

            if (nameToSearchFor == null)
            {
                Log.Error("Failed to get name for assembly");
                return null;
            }

            var allLoadedAssemblies = _loadedAssemblies.Values.Append(_myAssembly);
            List<string> assemblyLocations = [];

            foreach (var potentiallyDependentAssembly in allLoadedAssemblies.Reverse()) // reverse to start with our assembly
            {
                var dependencyContext = DependencyContext.Load(potentiallyDependentAssembly);
                if (dependencyContext == null)
                {
                    continue;
                }

                if (!TryFindLibrary(nameToSearchFor, dependencyContext, out var library))
                    continue;

                var wrapper = new CompilationLibrary(
                                                     library!.Type,
                                                     library.Name,
                                                     library.Version,
                                                     library.Hash,
                                                     library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                                                     library.Dependencies,
                                                     library.Serviceable);

                _assemblyResolver ??= CreateAssemblyResolver(_assemblyPaths.Values);
                _assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblyLocations);
            }

            if (assemblyLocations.Count == 0)
            {
                // search on our own using the paths we have
                foreach (var path in _assemblyPaths.Values)
                {
                    if (IsLikelyFile(path, nameToSearchFor))
                    {
                        assemblyLocations.Add(path);
                    }
                }
            }

            if (assemblyLocations.Count == 0)
            {
                if (_assemblyPaths.Values.Any(string.IsNullOrEmpty))
                {
                    Log.Warning($"Assembly '{this.Name}' has undefined assembly paths:");
                    foreach (var (k, v) in _assemblyPaths)
                    {
                        if (string.IsNullOrEmpty(v))
                        {
                            Log.Warning($" -> {k}");
                        }
                    }
                }
                
                // search directories of our explicitly added assembly paths
                foreach (var path in _assemblyPaths.Values)
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    var directory = Path.GetDirectoryName(path);
                    if (directory == null)
                    {
                        continue;
                    }

                    var files = Directory.GetFiles(directory, "", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                    {
                        if (IsLikelyFile(file, nameToSearchFor))
                            assemblyLocations.Add(file);
                    }
                }
            }

            string assemblyPath;
            switch (assemblyLocations.Count)
            {
                case 0:
                    return null;
                case 1:
                    assemblyPath = assemblyLocations[0];
                    break;
                default:
                {
                    var distinct = assemblyLocations.Distinct().ToArray();

                    if (distinct.Length > 1)
                    {
                        Log.Error($"Multiple assemblies found for {name.Name}");
                        foreach (var item in assemblyLocations)
                            Log.Info($"\t{item}");
                    }

                    assemblyPath = distinct[0];
                    break;
                }
            }

            var inferredAssemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            if (_loadedAssemblies.TryGetValue(inferredAssemblyName, out var assembly))
            {
                return assembly;
            }

            assembly = context.LoadFromAssemblyPath(assemblyPath);
            AddToLoadedAssemblies(assembly, false);
            CollectReferencedAssembliesOf(assembly, context);

            return assembly;
        }

        static bool IsLikelyFile(string path, string nameToSearchFor)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var inferredName = Path.GetFileNameWithoutExtension(path);
            return string.Equals(inferredName, nameToSearchFor, StringComparison.OrdinalIgnoreCase);
        }

        static bool TryFindLibrary(string nameToSearchFor, DependencyContext dependencyContext, out RuntimeLibrary? library)
        {
            foreach (var runtime in dependencyContext.RuntimeLibraries)
            {
                if (TryFindInLibrary(runtime, nameToSearchFor))
                {
                    library = runtime;
                    return true;
                }
            }

            library = default;
            return false;

            static bool TryFindInLibrary(RuntimeLibrary runtime, string nameToSearchFor)
            {
                if (string.Equals(runtime.Name, nameToSearchFor, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                foreach (var assemblyGroup in runtime.RuntimeAssemblyGroups)
                {
                    foreach (var runtimeFile in assemblyGroup.RuntimeFiles)
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(runtimeFile.Path);
                        if (string.Equals(fileName, nameToSearchFor, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }

    /// <summary>
    /// Adapted from https://www.codeproject.com/Articles/1194332/Resolving-Assemblies-in-NET-Core
    /// </summary>
    /// <param name="assemblyResolver"></param>
    /// <param name="directory">path to dll</param>
    private static CompositeCompilationAssemblyResolver CreateAssemblyResolver(IEnumerable<string> paths)
    {
        var resolvers = paths
                       .Select(p => new AppBaseCompilationAssemblyResolver(p))
                       .Cast<ICompilationAssemblyResolver>()
                       .Append(new ReferenceAssemblyPathResolver())
                       .Append(new PackageCompilationAssemblyResolver())
                       .ToArray();
        return new CompositeCompilationAssemblyResolver(resolvers);
    }
    
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var result = OnResolving(this, assemblyName);

        if (result == null)
        {
            // https://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor#answer-1177040
            var assemblyNameStr = assemblyName.Name;
            if (assemblyNameStr != null && assemblyNameStr.EndsWith("XmlSerializers"))
            {
                Log.Debug($"Failed to find Xml assembly {assemblyName}. This is expected for XmlSerializers");
                return null;
            }
            
            Log.Error($"Failed to load assembly {assemblyName.Name}");
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in _assemblyPaths)
            {
                var path = kvp.Value;
                var owner = kvp.Key;
                sb.Append(owner);
                sb.AppendLine(path);
            }

            if (sb.Length == 0)
                Log.Error("No assembly paths found");
            else
                Log.Error(sb.ToString());
            return null;
        }

        // check versions of the assembly - if different, log a warning. todo: actually do something with this information later
        var assemblyNameOfResult = result.GetName();

        if (assemblyNameOfResult.Version != assemblyName.Version)
        {
            Log.Warning($"Assembly {assemblyName.Name} loaded with different version: {assemblyNameOfResult.Version} vs {assemblyName.Version}");
        }

        return result;
    }

    // warning : not thread safe, must be wrapped in a lock around _assemblyLock
    private void AddToLoadedAssemblies(Assembly assembly, bool addPath, bool debug = true)
    {
        var name = GetName(assembly);
        if (!_loadedAssemblies.TryAdd(name, assembly))
        {
            Log.Debug($"{Name}: Skipping caching duplicate assembly {name}");
            return;
        }

        if (addPath)
            AddAssemblyPath(assembly, assembly.Location);

        if (debug && !name.StartsWith("System")) // don't log system assemblies - too much log spam for things that are probably not error-prone
            Log.Debug($"{Name}: Loaded assembly {name} from {assembly.Location}");
    }

    private void CollectReferencedAssembliesOf(Assembly assembly, AssemblyLoadContext context)
    {
        foreach (var referenced in assembly.GetReferencedAssemblies())
        {
            if (_loadedAssemblies.TryGetValue(GetName(referenced), out _))
                continue;

            _ = OnResolving(context, referenced);
        }
    }

    private static string GetName(Assembly assembly) => GetName(assembly.GetName());

    private static string GetName(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? assemblyName.FullName;
        return name;
    }

    private bool TryGetLoadedAssembly(AssemblyName assemblyName, AssemblyLoadContext context, [NotNullWhen(true)] out Assembly? assembly)
    {
        var name = GetName(assemblyName);

        // the following are performed in order of preference:

        // first we check if we already have the assembly in our cache
        if (_loadedAssemblies.TryGetValue(name, out assembly))
            return true;

        // then we check the "Default" load context - the root load context of Tooll
        if (TryGetExistingFromLoadContext(Default, name, out assembly))
            return true;

        // then we check the context provided
        if (TryGetExistingFromLoadContext(context, name, out assembly))
            return true;

        // then we check our own context, if the context provided is not our own
        if (context != this && TryGetExistingFromLoadContext(this, name, out assembly))
        {
           return true;
        }
        
        // guess we didn't find it :(
        return false;

        bool TryGetExistingFromLoadContext(AssemblyLoadContext context, string name, [NotNullWhen(true)] out Assembly? assembly)
        {
            assembly = context.Assemblies.FirstOrDefault(x => GetName(x) == name);

            if (assembly != null)
            {
                AddToLoadedAssemblies(assembly, false);
                CollectReferencedAssembliesOf(assembly, context);
                return true;
            }

            return false;
        }
    }


    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        Console.WriteLine($"Attempting to load unmanaged dll: {unmanagedDllName}");
        return base.LoadUnmanagedDll(unmanagedDllName);
    }
}