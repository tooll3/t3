#nullable enable
using System;
using System.Collections;
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
                    _root = new AssemblyTreeNode(asm);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to load root assembly {_rootName.Name}: {e}");
                }

                return _root;
            }
        }
    }

    private readonly AssemblyName _rootName;
    private static readonly AssemblyTreeNode[] _coreNodes;
    private readonly string _directory;

    static T3AssemblyLoadContext()
    {
        _coreNodes = RuntimeAssemblies.CoreAssemblies
                                     .Select(x => new AssemblyTreeNode(x))
                                     .ToArray();
    }

    internal T3AssemblyLoadContext(AssemblyName rootName, string directory) : base(nameof(T3AssemblyLoadContext), true)
    {
        _directory = directory;
        Resolving += OnResolving;
        Log.Debug($"Creating new assembly load context for {rootName.Name}");
        _rootName = rootName;
    }

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName asmName)
    {
        lock (_assemblyLock)
        {
            var name = asmName.GetNameSafe();

            // check the core assemblies first - these are the ones that are always loaded
            foreach (var coreRef in _coreNodes)
            {
                if (coreRef.TryFind(name, context, out var coreAssembly))
                {
                    return coreAssembly.Assembly;
                }
            }

            // the following are performed in order of preference:
            var root = Root;
            if (root != null)
            {
                if(root.TryFind(name, context, out var assembly) || // first we try with the provided context
                   root.TryFind(name, Default, out assembly) || // then we check the "Default" load context - the root load context of Tooll
                   (context != this && root.TryFind(name, context, out assembly))) // then we try *this* context if it's not the one provided
                {
                    return assembly.Assembly;
                }
            }

            // try other assembly contexts
            foreach (var ctx in _dependencyContexts)
            {
                var asm = ctx.OnResolving(this, asmName);
                if (asm != null)
                    return asm;
            }

            // guess we didn't find it :(
            return null;
        }
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // check Core assemblies for a match
        foreach (var coreNode in _coreNodes)
        {
            var name = assemblyName.GetNameSafe();
            if (coreNode.TryFind(name, this, out var node))
                return node.Assembly;
        }

        var result = OnResolving(this, assemblyName);

        if (result != null)
        {
            var assemblyNameOfResult = result.GetName();

            if (assemblyNameOfResult.Version != assemblyName.Version)
            {
                Log.Warning($"Assembly {assemblyName.Name} loaded with different version: {assemblyNameOfResult.Version} vs {assemblyName.Version}");
            }

            return result;
        }

        // --- Error logging ---
        // check versions of the assembly - if different, log a warning. todo: actually do something with this information later
        // https://stackoverflow.com/questions/1127431/xmlserializer-giving-filenotfoundexception-at-constructor#answer-1177040
        var assemblyNameStr = assemblyName.Name;
        if (assemblyNameStr != null && assemblyNameStr.EndsWith("XmlSerializers"))
        {
            Log.Debug($"Failed to find Xml assembly {assemblyName}. This is expected for XmlSerializers");
            return null;
        }

        var root = Root;
        if (root == null)
        {
            Log.Error($"Failed to load assembly {assemblyName.Name} - root is null");
            return null;
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Failed to load assembly {assemblyName.Name}")
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
        Console.WriteLine($"Attempting to load unmanaged dll: {unmanagedDllName}");
        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    public void AddDependencyContext(T3AssemblyLoadContext ctx)
    {
        _dependencyContexts.Add(ctx);
        ctx.UnloadTriggered += OnDependencyUnloaded;
    }

    private void OnDependencyUnloaded(object? sender, EventArgs e)
    {
        var ctx = (T3AssemblyLoadContext)sender!;
        ctx.UnloadTriggered -= OnDependencyUnloaded;
        _dependencyContexts.Remove(ctx);
        BeginUnload(); // begin unloading ourself too
    }

    public void BeginUnload()
    {
        try
        {
            UnloadTriggered?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            Log.Error($"Exception thrown on assembly unload: {e}");
        }

        _root = null; // dereference our assembly as we will need to reload it 
        Unload();
    }

    public event EventHandler? UnloadTriggered;
    private readonly List<T3AssemblyLoadContext> _dependencyContexts = new();
}

internal static class AssemblyNameExtensions
{
    public static string GetNameSafe(this Assembly assembly) => assembly.GetName().GetNameSafe();

    public static string GetNameSafe(this AssemblyName asmName)
    {
        return asmName.Name ?? asmName.FullName;
    }
}