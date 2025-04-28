#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
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
    private readonly Lock _assemblyLock = new();

    // this is an ultimate list of assemblies that have been loaded for this context
    // keeping this cache allows us to avoid stack overflows during loading, and also
    // speeds up the loading process.

    private AssemblyTreeNode? _root;

    private readonly AssemblyName _rootName;
    private readonly string _rootNameStr;
    private readonly List<T3AssemblyLoadContext> _dependencyContexts = [];
    private static readonly AssemblyTreeNode[] _coreNodes;
    private static readonly List<T3AssemblyLoadContext> _loadContexts = [];
    private static readonly object _loadContextLock = new();
    private readonly string _directory;

    static T3AssemblyLoadContext()
    {
        _coreNodes = RuntimeAssemblies.CoreAssemblies
                                      .Select(x => new AssemblyTreeNode(x, nameof(RuntimeAssemblies)))
                                      .ToArray();
    }

    internal T3AssemblyLoadContext(AssemblyName rootName, string directory) : base(nameof(T3AssemblyLoadContext), true)
    {
        _directory = directory;
        Resolving += (thisCtx, name) => OnResolving(name, true);
        Unloading += (thisCtx) =>
                     {
                         Log.Debug($"{_rootNameStr}: Unloading assembly context");
                     };
        
        _rootName = rootName;
        _rootNameStr = rootName.GetNameSafe();
        Log.Debug($"{_rootNameStr}: Creating new assembly load context for {rootName.Name}");
        lock (_loadContextLock)
        {
            _loadContexts.Add(this);
        }
    }

    internal AssemblyTreeNode? Root
    {
        get
        {
            lock (_assemblyLock)
            {
                if (_root != null)
                    return _root;

                var path = Path.Combine(_directory, _rootName.GetNameSafe()) + ".dll";

                try
                {
                    var asm = Assembly.LoadFile(path);
                    _root = new AssemblyTreeNode(asm, $"[[{_rootName}]] (root)");
                }
                catch (Exception e)
                {
                    Log.Error($"{_rootNameStr}: Failed to load root assembly {_rootName.Name}: {e}");
                }

                return _root;
            }
        }
    }

    private Assembly? OnResolving(AssemblyName asmName, bool allowExternalContextSearch)
    {
        var name = asmName.GetNameSafe();
        // check the core assemblies first - these are the ones that are always loaded
        foreach (var coreRef in _coreNodes)
        {
            if (coreRef.TryFind(name, Default, out var coreAssembly))
            {
                return coreAssembly.Assembly;
            }
        }

        lock (_assemblyLock)
        {
            // the following are performed in order of preference:
            var root = Root;
            if (root != null)
            {
                if (root.TryFind(name, this, out var assembly)) //|| // first we try with the provided context
                {
                    return assembly.Assembly;
                }
            }
        }

        if (allowExternalContextSearch)
        {
            // try other assembly contexts
            lock (_loadContextLock)
            {
                foreach (var ctx in _loadContexts)
                {
                    if (ctx == this)
                        continue;

                    var root = ctx.Root;

                    if (root == null)
                        continue;

                    var asm = ctx.OnResolving(asmName, false);

                    if (asm != null)
                    {
                        AddDependency(root, ctx);
                        return asm;
                    }
                }
            }

            // guess we didn't find it :(
            Log.Error($"{_rootNameStr}: Failed to resolve assembly '{name}'");
        }

        return null;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var result = OnResolving(assemblyName, true);

        if (result != null)
        {
            var assemblyNameOfResult = result.GetName();

            if (assemblyNameOfResult.Version != assemblyName.Version)
            {
                Log.Warning($"{_rootNameStr}: Assembly {assemblyName.Name} loaded with different version: {assemblyNameOfResult.Version} vs {assemblyName.Version}");
            }

            return result;
        }

        // --- Error logging ---
        // check versions of the assembly - if different, log a warning. todo: actually do something with this information later
        // https://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor#answer-1177040
        var assemblyNameStr = assemblyName.Name;
        if (assemblyNameStr != null && assemblyNameStr.EndsWith("XmlSerializers"))
        {
            Log.Debug($"{_rootNameStr}: Failed to find Xml assembly {assemblyName}. This is expected for XmlSerializers");
            return null;
        }

        var root = Root;
        if (root == null)
        {
            Log.Error($"{_rootNameStr}: Failed to load assembly {assemblyName.Name} - root {_rootName} is null");
            return null;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append(_rootName).AppendLine($": Failed to load assembly {assemblyName.Name}")
          .AppendLine("Search paths:");

        foreach (var path in root.VisibleDirectories)
        {
            sb.Append('\t');
            sb.AppendLine(path);
        }

        Log.Error(sb.ToString());
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        Console.WriteLine($"{_rootNameStr}: Attempting to load unmanaged dll: {unmanagedDllName}");
        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    private void AddDependency(AssemblyTreeNode rootNode, T3AssemblyLoadContext ctx)
    {
        lock (_assemblyLock)
        {
            if (!Root!.AddReferenceTo(rootNode)) 
                return;
            
            // subscribe to the unload event of the dependency context
            ctx.UnloadTriggered += OnDependencyUnloaded;
            _dependencyContexts.Add(ctx);
        }
    }

    private void OnDependencyUnloaded(object? sender, EventArgs e)
    {
        var ctx = (T3AssemblyLoadContext)sender!;
        ctx.UnloadTriggered -= OnDependencyUnloaded;
        _dependencyContexts.Remove(ctx);
        BeginUnload(); // begin unloading ourselves too
    }

    public void BeginUnload()
    {
        try
        {
            UnloadTriggered?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            Log.Error($"{_rootNameStr}: Exception thrown on assembly unload: {e}");
        }

        lock (_assemblyLock)
        {
            // unsubscribe from all our dependencies
            for (int i = _dependencyContexts.Count - 1; i >= 0; i--)
            {
                var ctx = _dependencyContexts[i];
                ctx.UnloadTriggered -= OnDependencyUnloaded;
                ctx.Unload();
                _dependencyContexts.RemoveAt(i);
            }

            _root = null; // dereference our assembly as we will need to reload it 
        }

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