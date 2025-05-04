#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using T3.Core.IO;
using T3.Core.Logging;
using NuGet.Configuration;

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
    private readonly Lock _dependencyLock = new();

    internal AssemblyTreeNode? Root { get; private set; }

    private readonly List<T3AssemblyLoadContext> _dependencyContexts = [];
    private static readonly List<AssemblyTreeNode> _coreNodes = [];

    private static readonly AssemblyNameAndPath[] _availableNugetAssemblies;
    private static readonly Lock _nugetLock = new();
    private static readonly AssemblyLoadContext _nugetContext = new("NuGet", true);
    private static readonly List<AssemblyTreeNode> _loadedNuGetAssemblies = [];

    private sealed class AssemblyNameAndPath(string path, string fileBasedName)
    {
        private AssemblyName? _assemblyName;
        private bool _triedToLoad;
        public string Path { get; } = path;
        public string FileBasedName { get; } = fileBasedName;
        public bool Claimed;

        public AssemblyName? AssemblyName
        {
            get
            {
                if (_triedToLoad)
                    return _assemblyName;

                try
                {
                    _assemblyName = AssemblyName.GetAssemblyName(Path);
                }
                catch
                {
                    _assemblyName = null;
                }

                _triedToLoad = true;
                return _assemblyName;
            }
        }
    }

    static T3AssemblyLoadContext()
    {
        // prepare nuget assembly loading (without actually loading them)
        var settings = Settings.LoadDefaultSettings(null, null, null);
        var nuGetDirectory = SettingsUtility.GetGlobalPackagesFolder(settings);

        var directoryInfo = new DirectoryInfo(nuGetDirectory);
        if (directoryInfo.Exists)
        {
            // get all dlls in the nuget directory
            // todo - this should be invalidated at runtime
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _availableNugetAssemblies = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                                                     .AsParallel()
                                                     .Select(x =>
                                                             {
                                                                 const string extension = ".dll";
                                                                 const int extensionLength = 4;
                                                                 if (!x.Name.EndsWith(extension, StringComparison.Ordinal))
                                                                     return null;

                                                                 try
                                                                 {
                                                                     return new AssemblyNameAndPath(x.FullName, x.Name[..^extensionLength]);
                                                                 }
                                                                 catch
                                                                 {
                                                                     return null;
                                                                 }
                                                             })
                                                      // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                                                     .Where(x => x != null)
                                                     .ToArray()!;

            stopwatch.Stop();
            Log.Debug($"Found {_availableNugetAssemblies.Length} nuget assemblies in {stopwatch.ElapsedMilliseconds}ms");

            if (_availableNugetAssemblies.Length > 10_000)
            {
                Log.Warning("You have a lot of nuget packages installed - consider clearing your cache for faster load times.");
            }
        }
        else
        {
            Log.Error($"NuGet directory {nuGetDirectory} does not exist");
            _availableNugetAssemblies = [];
        }

        (AssemblyLoadContext Context, (Assembly Assembly, AssemblyName name)[] assemblies)[]? allAssemblies = All
           .Select(ctx => (
                              ctx: ctx,
                              assemblies: ctx.Assemblies
                                             .Select(x => (asm: x, name: x.GetName()))
                                             .ToArray()))
           .ToArray();

        // create "root" nodes for each assembly context - one per context and one per directory for each context
        foreach (var ctxGroup in allAssemblies)
        {
            List<string> directories = new();
            foreach (var assemblyDef in ctxGroup.assemblies)
            {
                string? directory;

                try
                {
                    directory = Path.GetDirectoryName(assemblyDef.Assembly.Location);
                }
                catch
                {
                    continue;
                }

                if (directory == null || directories.Contains(directory))
                    continue;

                directories.Add(directory);
                var node = new AssemblyTreeNode(assemblyDef.Assembly, ctxGroup.Context, false);
                _coreNodes.Add(node);
            }
        }

        // add references to each core node where applicable, reusing existing nodes to create the tree
        for (var index = 0; index < _coreNodes.Count; index++)
        {
            var node = _coreNodes[index];
            var dependencies = node.Assembly.GetReferencedAssemblies();
            foreach (var dependencyName in dependencies)
            {
                foreach (var ctxGroup in allAssemblies)
                {
                    foreach (var asmAndName in ctxGroup.assemblies)
                    {
                        if (asmAndName.name != dependencyName)
                            continue;

                        AssemblyTreeNode? depNode = null;
                        var nameStr = dependencyName.GetNameSafe();
                        foreach (var coreNode in _coreNodes)
                        {
                            if (coreNode.TryFindExisting(nameStr, out depNode))
                                break;
                        }

                        depNode ??= new AssemblyTreeNode(asmAndName.Assembly, ctxGroup.Context, false);

                        node.AddReferenceTo(depNode);
                    }
                }
            }
        }
    }

    private static List<AssemblyTreeNode> CoreNodes => _coreNodes;

    private static readonly List<T3AssemblyLoadContext> _loadContexts = [];
    private static readonly Lock _loadContextLock = new();

    internal T3AssemblyLoadContext(AssemblyName rootName, string directory) :
        base(rootName.GetNameSafe(), true)
    {
        Resolving += (_, name) =>
                     {
                         var result = OnResolving(name);

                         if (result != null)
                         {
                             // check versions of the assembly - if different, log a warning.
                             // todo: actually do something with this information later
                             if (ProjectSettings.Config.LogAssemblyVersionMismatches)
                             {
                                 var assemblyNameOfResult = result.GetName();

                                 if (assemblyNameOfResult.Version != name.Version)
                                 {
                                     Log.Warning($"Assembly {name.Name} loaded with different version: {assemblyNameOfResult.Version} vs {name.Version}");
                                 }
                             }
                         }

                         if (result == null)
                         {
                             Log.Error($"{Name!}: Failed to resolve assembly '{name.Name}'");
                             return Root!.Assembly;
                         }

                         return result;
                     };
        Unloading += (_) => { Log.Debug($"{Name!}: Unloading assembly context"); };

        Log.Debug($"{Name}: Creating new assembly load context for {rootName.Name}");
        lock (_loadContextLock)
        {
            _loadContexts.Add(this);
        }

        var path = Path.Combine(directory, Name!) + ".dll";

        try
        {
            var asm = LoadFromAssemblyPath(path);
            Root = new AssemblyTreeNode(asm, this, true);
        }
        catch (Exception e)
        {
            Log.Error($"{Name!}: Failed to load root assembly {Name}: {e}");
        }
    }

    // called if Load method returns null - searches other contexts and nuget packages
    private Assembly? OnResolving(AssemblyName asmName)
    {
        var name = asmName.GetNameSafe();

        // try other assembly contexts
        lock (_loadContextLock)
        {
            // try to find existing in others
            foreach (var ctx in _loadContexts)
            {
                if (ctx == this)
                    continue;

                var root = ctx.Root;

                if (root == null)
                    continue;

                if (root.TryFindExisting(name, out var asmNode))
                {
                    // add the dependency to our context
                    ctx.AddDependency(asmNode);
                    AddDependency(asmNode);
                    return asmNode.Assembly;
                }
            }

            // try to find unreferenced in others
            foreach (var ctx in _loadContexts)
            {
                if (ctx == this)
                    continue;

                var root = ctx.Root;

                if (root == null)
                    continue;

                if (root.TryFindUnreferenced(name, out var asmNode))
                {
                    // add the dependency to our context
                    ctx.AddDependency(asmNode);
                    AddDependency(asmNode);
                    return asmNode.Assembly;
                }
            }
        }
        
        // check nuget packages
        lock (_nugetLock)
        {
            foreach (var node in _loadedNuGetAssemblies)
            {
                if (node.TryFindExisting(name, out var asmNode))
                {
                    // add the dependency to our context
                    AddDependency(asmNode);
                    return asmNode.Assembly;
                }

                if (node.TryFindUnreferenced(name, out asmNode))
                {
                    // add the dependency to our context
                    AddDependency(asmNode);
                    return asmNode.Assembly;
                }
            }

            foreach (var package in _availableNugetAssemblies)
            {
                if (package.Claimed || package.FileBasedName != name) 
                    continue;

                var potentialAssemblyName = package.AssemblyName;
                if (potentialAssemblyName == null)
                    continue;

                if (potentialAssemblyName.FullName != asmName.FullName)
                    continue;

                try
                {
                    package.Claimed = true;
                    var assembly = _nugetContext.LoadFromAssemblyPath(package.Path);
                    var node = new AssemblyTreeNode(assembly, _nugetContext, true);
                    AddDependency(node);
                    _loadedNuGetAssemblies.Add(node);
                    Log.Debug($"{Name!}: Loaded assembly {asmName.FullName} from nuget package");
                    return assembly;
                }
                catch (Exception e)
                {
                    Log.Error($"{Name!}: Failed to load assembly {asmName.FullName} from nuget package: {e}");
                }
            }
        }

        // guess we didn't find it :(
        return null;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.GetNameSafe();

        foreach (var coreRef in CoreNodes)
        {
            if (coreRef.TryFindExisting(name, out var coreAssembly))
            {
                AddDependency(coreAssembly);
                return coreAssembly.Assembly;
            }

            if (coreRef.TryFindUnreferenced(name, out coreAssembly))
            {
                AddDependency(coreAssembly);
                return coreAssembly.Assembly;
            }
        }

        if (Root is null)
        {
            Log.Error($"{Name!}: Root is null, cannot resolve assembly {name}");
            return null;
        }

        if (Root!.TryFindExisting(name, out var node))
        {
            AddDependency(node);
            return node.Assembly;
        }

        if (Root!.TryFindUnreferenced(name, out node))
        {
            AddDependency(node);
            return node.Assembly;
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        Console.WriteLine($"{Name!}: Attempting to load unmanaged dll: {unmanagedDllName}");
        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    private void AddDependency(AssemblyTreeNode node)
    {
        _ = Root!.AddReferenceTo(node);

        if (node.LoadContext == this || node.LoadContext is not T3AssemblyLoadContext tixlCtx)
            return;
        lock (_dependencyLock)
        {
            if (!_dependencyContexts.Contains(node.LoadContext))
            {
                // subscribe to the unload event of the dependency context
                tixlCtx.UnloadTriggered += OnDependencyUnloaded;
                _dependencyContexts.Add(tixlCtx);
            }
        }
    }

    private void OnDependencyUnloaded(object? sender, EventArgs e)
    {
        var ctx = (T3AssemblyLoadContext)sender!;

        lock (_dependencyLock)
        {
            ctx.UnloadTriggered -= OnDependencyUnloaded;
            _dependencyContexts.Remove(ctx);
            BeginUnload(); // begin unloading ourselves too
        }
    }

    public void BeginUnload()
    {
        try
        {
            UnloadTriggered?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            Log.Error($"{Name!}: Exception thrown on assembly unload: {e}");
        }

        lock (_dependencyLock)
        {
            // unsubscribe from all our dependencies
            for (int i = _dependencyContexts.Count - 1; i >= 0; i--)
            {
                var ctx = _dependencyContexts[i];
                ctx.UnloadTriggered -= OnDependencyUnloaded;
                ctx.Unload();
                _dependencyContexts.RemoveAt(i);
            }
        }

        Root?.Unload();
        Root = null; // dereference our assembly as we will need to reload it 

        lock (_loadContextLock)
        {
            _loadContexts.Remove(this);
        }

        Unload();
    }

    public event EventHandler? UnloadTriggered;
}

internal static class AssemblyNameExtensions
{
    public static string GetNameSafe(this AssemblyName asmName) => asmName.Name ?? asmName.FullName;
}