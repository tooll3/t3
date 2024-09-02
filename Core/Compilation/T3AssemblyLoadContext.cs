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

internal sealed class T3AssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _path;
    private Assembly? _myAssembly;

    private static readonly IReadOnlyList<Assembly> CoreAssemblies = RuntimeAssemblies.CoreAssemblies;

    internal T3AssemblyLoadContext(AssemblyName assemblyName, string path) : base(assemblyName.Name, true)
    {
        _path = path;
        Resolving += OnResolving;
        _assemblyPaths[this] = path;

        foreach (var assembly in CoreAssemblies)
        {
            AddToLoadedAssemblies(assembly, debug: false);
            var location = assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
            {
                location = Path.Combine(RuntimeAssemblies.CoreDirectory, $"{assembly.GetName().Name}.dll");
            }
            
            AddAssemblyPath(assembly, location);
        }
    }

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
            if (TryGetLoadedAssembly(name, out var assembly))
                return assembly;

            if (_myAssembly == null)
            {
                _myAssembly = LoadFromAssemblyPath(_path);

                foreach (var referenced in _myAssembly.GetReferencedAssemblies())
                {
                    // todo - maybe this should be properly recursive to capture all dependencies
                    OnResolving(context, referenced);
                }
            }
        }

        var allLoadedAssemblies = _loadedAssemblies.Values.Append(_myAssembly);
        List<string> assemblyLocations = [];

        foreach (var assembly in allLoadedAssemblies.Reverse()) // reverse to start with our assembly
        {
            var dependencyContext = DependencyContext.Load(assembly);
            if (dependencyContext == null)
            {
                continue;
            }

            if (!TryFindLibrary(name, dependencyContext, out var library))
                continue;

            var wrapper = new CompilationLibrary(
                                                 library!.Type,
                                                 library.Name,
                                                 library.Version,
                                                 library.Hash,
                                                 library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                                                 library.Dependencies,
                                                 library.Serviceable);

            lock (_assemblyLock)
            {
                _assemblyResolver ??= CreateAssemblyResolver(_assemblyPaths.Values);
                _assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblyLocations);
            }
        }

        var nameToSearchFor = name.Name;
        if (nameToSearchFor == null)
        {
            Log.Error("Failed to get name for assembly");
            return null;
        }

        if (assemblyLocations.Count == 0)
        {
            // search on our own using the paths we have
            foreach (var path in _assemblyPaths.Values)
            {
                if (IsLikelyFile(path, nameToSearchFor))
                    assemblyLocations.Add(path);
            }
        }

        if (assemblyLocations.Count == 0)
        {
            // search directories of our explicitly added assembly paths
            foreach (var path in _assemblyPaths.Values)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    Log.Warning("Null path in assembly paths");
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

        static bool IsLikelyFile(string path, string nameToSearchFor)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;
            
            var inferredName = Path.GetFileNameWithoutExtension(path);
            return !string.IsNullOrWhiteSpace(inferredName) 
                   && string.Equals(inferredName, nameToSearchFor, StringComparison.OrdinalIgnoreCase);
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

        lock (_assemblyLock)
        {
            var assembly = context.LoadFromAssemblyPath(assemblyPath);
            AddToLoadedAssemblies(assembly);

            foreach (var referenced in assembly.GetReferencedAssemblies())
            {
                OnResolving(context, referenced);
            }

            return assembly;
        }

        static bool TryFindLibrary(AssemblyName name, DependencyContext dependencyContext, out RuntimeLibrary? library)
        {
            var nameToSearchFor = name.Name;
            if (nameToSearchFor == null)
            {
                library = default;
                Log.Error("Failed to get name for assembly");
                return false;
            }

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

    private readonly object _assemblyLock = new();
    private CompositeCompilationAssemblyResolver? _assemblyResolver;
    private readonly Dictionary<object, string> _assemblyPaths = new();
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (!TryGetLoadedAssembly(assemblyName, out var result))
        {
            result = OnResolving(this, assemblyName);
        }

        if (result == null)
        {
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

    private void AddToLoadedAssemblies(Assembly assembly, bool debug = true)
    {
        lock (_assemblyLock)
        {
            var name = assembly.GetName().FullName;
            _loadedAssemblies[name] = assembly;
            if(debug && !name.StartsWith("System")) // don't log system assemblies - too much log spam for things that are probably not error-prone
                Log.Debug($"Loaded assembly {name} from {assembly.Location}");
        }
    }

    private bool TryGetLoadedAssembly(AssemblyName assemblyName, [NotNullWhen(true)] out Assembly? assembly)
    {
        lock (_assemblyLock)
        {
            if (_loadedAssemblies.TryGetValue(assemblyName.FullName, out assembly))
                return true;
        }

        assembly ??= Default.Assemblies.FirstOrDefault(x => x.FullName == assemblyName.FullName);
        assembly ??= Assemblies.FirstOrDefault(x => x.FullName == assemblyName.FullName);
        return assembly != null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        Console.WriteLine($"Attempting to load unmanaged dll: {unmanagedDllName}");
        return base.LoadUnmanagedDll(unmanagedDllName);
    }
}