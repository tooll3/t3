#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using T3.Core.Logging;

namespace T3.Core.Compilation;

internal sealed class AssemblyTreeNode
{
    public readonly Assembly Assembly;
    public readonly AssemblyName Name;
    public readonly string NameStr;

    private bool _loadedDependencies = false;

    private readonly List<AssemblyTreeNode> _references = [];
    private readonly List<string> _visibleDirectories = [];
    private readonly List<DllReference> _visiblePaths = [];

    private readonly object _assemblyLock = new();

    private readonly record struct DllReference(string Path, string Name, AssemblyName AssemblyName);

    private List<DllReference>? _unreferencedDlls;

    private List<DllReference> UnreferencedDlls
    {
        get
        {
            lock (_assemblyLock)
            {
                if (_unreferencedDlls is not null)
                    return _unreferencedDlls;

                _unreferencedDlls = [];
                // locate "not used" dlls in the directory without loading them
                var directory = Path.GetDirectoryName(Assembly.Location);
                foreach (var file in Directory.GetFiles(directory!, "*.dll", SearchOption.AllDirectories))
                {
                    bool skip = false;
                    foreach (var dep in _visiblePaths)
                    {
                        if (file == dep.Path)
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (skip)
                        continue;

                    AssemblyName assemblyName;
                    try
                    {
                        assemblyName = AssemblyName.GetAssemblyName(file);
                    }
                    catch
                    {
                        continue;
                    }

                    var reference = new DllReference(file, assemblyName.GetNameSafe(), assemblyName);
                    _unreferencedDlls.Add(reference);
                }
                
                return _unreferencedDlls;
            }
        }
    }

    private readonly string _parentName;

    // warning : not thread safe, must be wrapped in a lock around _assemblyLock
    public AssemblyTreeNode(Assembly assembly, string parent)
    {
        Assembly = assembly;
        Name = assembly.GetName();
        NameStr = Name.GetNameSafe();
        _parentName = parent;

        // if (debug && !node.NameStr.StartsWith("System")) // don't log system assemblies - too much log spam for things that are probably not error-prone
        //Log.Debug($"{parent}: Loaded assembly {NameStr} from {assembly.Location}");
    }

    private void LoadDependencies(AssemblyLoadContext ctx)
    {
        lock (_assemblyLock)
        {
            if (_loadedDependencies)
                throw new InvalidOperationException($"{_parentName}: Dependencies already loaded");

            var directory = Path.GetDirectoryName(Assembly.Location);
            if (directory == null)
                throw new InvalidOperationException($"{_parentName}: Could not get directory for {Assembly.Location}");

            var deps = Assembly.GetReferencedAssemblies();

            foreach (var dep in deps)
            {
                var path = Path.Combine(directory, dep.GetNameSafe());

                Assembly asm;

                try
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    asm = ctx.LoadFromAssemblyPath(path);
                }
                catch (FileNotFoundException)
                {
                    Log.Warning($"{_parentName}: Could not find assembly `{path}`");
                    continue;
                }
                catch (Exception e)
                {
                    Log.Error($"{_parentName}: Failed to load assembly from `{path}`\n{e}");
                    continue;
                }

                AddReferenceTo(new AssemblyTreeNode(asm, ctx.Name ?? "unknown context"));
            }

            _loadedDependencies = true;
        }
    }

    public IReadOnlyList<string> VisibleDirectories
    {
        get
        {
            if (_visibleDirectories.Count > 0)
            {
                return _visibleDirectories;
            }

            foreach (var path in DependencyPaths)
            {
                var directory = Path.GetDirectoryName(path.Path)!;
                if (!_visibleDirectories.Contains(directory)) // avoid duplicates for lookups
                    _visibleDirectories.Add(directory);
            }

            return _visibleDirectories;
        }
    }

    private DllReference Reference => new(Assembly.Location, NameStr, Name);

    private List<DllReference> DependencyPaths
    {
        get
        {
            if (_visiblePaths.Count > 0)
                return _visiblePaths;

            _visiblePaths.Add(Reference);

            foreach (var r in _references)
            {
                _visiblePaths.Add(r.Reference);
            }

            return _visiblePaths;
        }
    }

    public bool AddReferenceTo(AssemblyTreeNode child)
    {
        if (_references.Contains(child))
        {
            return false;
        }

        if (UnreferencedDlls.Contains(child.Reference))
        {
            UnreferencedDlls.Remove(child.Reference);
        }

        _references.Add(child);
        _visiblePaths.Clear(); // invalidate path list
        _visibleDirectories.Clear(); // invalidate visible directories list
        return true;
    }

    private IEnumerable<AssemblyTreeNode> GetAssemblyNodes(AssemblyLoadContext ctx)
    {
        yield return this;

        lock (_assemblyLock)
        {
            if (!_loadedDependencies)
            {
                try
                {
                    LoadDependencies(ctx);
                }
                catch (Exception e)
                {
                    Log.Error($"{_parentName}: Failed to load dependencies for {NameStr}\n{e}");
                }
            }
        }

        foreach (var reference in _references)
        {
            foreach (var node in reference.GetAssemblyNodes(ctx))
            {
                yield return node;
            }
        }
    }

    public bool Matches(string nameToSearchFor)
    {
        var files = Directory.GetFiles(Path.GetDirectoryName(Assembly.Location)!, "*.dll");
        foreach (var file in files)
        {
            AssemblyName assemblyName;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(file);
            }
            catch (Exception e)
            {
                //Log.Error($"{_parentName} ({NameStr}): Failed to get assembly name for {file}\n{e}");
                continue;
            }

            Log.Debug($"{_parentName} ({NameStr}): Located assembly {assemblyName.Name} in {file}");

            var reference = new DllReference(file, assemblyName.GetNameSafe(), assemblyName);
            if (!_visiblePaths.Contains(reference))
                _visiblePaths.Add(reference);
        }

        return NameStr == nameToSearchFor;
    }

    public bool TryFindUnreferenced(string nameToSearchFor, AssemblyLoadContext ctx, [NotNullWhen(true)] out AssemblyTreeNode? assembly)
    {
        // check unreferenced dlls
        foreach (var node in GetAssemblyNodes(ctx))
        {
            foreach (var dir in node.UnreferencedDlls)
            {
                if (dir.Name != nameToSearchFor)
                    continue;

                try
                {
                    if (!File.Exists(dir.Path))
                    {
                        Log.Warning($"{_parentName}: Could not find assembly `{dir.Path}`");
                        continue;
                    }

                    var newAssembly = ctx.LoadFromAssemblyPath(dir.Path);
                    var name = ctx.Name ?? "unknown context";
                    var newNode = new AssemblyTreeNode(newAssembly, $"{name} -> {node.NameStr}");
                    node.AddReferenceTo(newNode);

                    assembly = newNode;
                    return true;
                }
                catch (Exception e)
                {
                    Log.Error($"{_parentName}: Exception loading assembly: {e}");
                }
            }
        }

        assembly = null;
        return false;
    }

    public bool TryFind(string nameToSearchFor, AssemblyLoadContext ctx, [NotNullWhen(true)] out AssemblyTreeNode? assembly)
    {
        if (NameStr == nameToSearchFor)
        {
            assembly = this;
            return true;
        }

        var nodes = GetAssemblyNodes(ctx).ToArray();

        foreach (var node in nodes)
        {
            if (node.NameStr == nameToSearchFor)
            {
                assembly = node;
                return true;
            }
        }

      

        assembly = null;
        return false;
    }
}