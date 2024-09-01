#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    internal T3AssemblyLoadContext(AssemblyName assemblyName, string path) : base(assemblyName.Name, true)
    {
        _path = path;
        Resolving += OnResolving;
        _assemblyPaths[this] = path;
    }

    internal void AddAssemblyPath(object packageOwner, string assemblyInformationPath)
    {
        lock (_assemblyLock)
        {
            _assemblyPaths[packageOwner] = assemblyInformationPath;
            _dependencyContext = null;
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
                _dependencyContext = DependencyContext.Load(_myAssembly);
                AddToLoadedAssemblies(_myAssembly);

                foreach (var referenced in _myAssembly.GetReferencedAssemblies())
                {
                    // todo - maybe this should be properly recursive to capture all dependencies
                    OnResolving(context, referenced);
                }
            }
        }

        if (_dependencyContext == null)
        {
            Log.Error($"Failed to load dependency context for {Name}");
            return null;
        }

        if (!TryFindLibrary(name, _dependencyContext, out var library))
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

        lock (_assemblyLock)
        {
            _assemblyResolver ??= CreateAssemblyResolver(_assemblyPaths.Values);
        }

        _assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);

        switch (assemblies.Count)
        {
            case 0:
                return null;
            case 1:
                break;
            default:
            {
                Log.Error($"Multiple assemblies found for {name.Name}");
                foreach (var item in assemblies)
                    Log.Info($"\t{item}");
                break;
            }
        }

        var assemblyPath = assemblies[0];

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

    /// <summary>
    /// Adapted from https://www.codeproject.com/Articles/1194332/Resolving-Assemblies-in-NET-Core
    /// </summary>
    /// <param name="assemblyResolver"></param>
    /// <param name="directory">path to dll</param>
    private static CompositeCompilationAssemblyResolver CreateAssemblyResolver(IEnumerable<string> paths)
    {
        Log.Debug($"Creating assembly resolver for:\n{string.Join(", ", paths)}\n");
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
    private DependencyContext? _dependencyContext;
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

    private void AddToLoadedAssemblies(Assembly assembly)
    {
        lock (_assemblyLock)
        {
            var name = assembly.GetName().FullName;
            _loadedAssemblies[name] = assembly;
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