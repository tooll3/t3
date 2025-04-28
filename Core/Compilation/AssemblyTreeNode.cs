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
    private readonly List<string> _visiblePaths = [];

    private readonly string _parentName;
    
    // warning : not thread safe, must be wrapped in a lock around _assemblyLock
    public AssemblyTreeNode(Assembly assembly, string parent)
    {
        Assembly = assembly;
        Name = assembly.GetName();
        NameStr = Name.GetNameSafe();
        _parentName = parent;

        // if (debug && !node.NameStr.StartsWith("System")) // don't log system assemblies - too much log spam for things that are probably not error-prone
        Log.Debug($"{parent}: Loaded assembly {NameStr} from {assembly.Location}");
    }

    private void LoadDependencies(AssemblyLoadContext ctx)
    {
        if(_loadedDependencies)
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
                var directory = Path.GetDirectoryName(path)!;
                if (!_visibleDirectories.Contains(directory)) // avoid duplicates for lookups
                    _visibleDirectories.Add(directory);
            }

            return _visibleDirectories;
        }
    }

    private List<string> DependencyPaths
    {
        get
        {
            if (_visiblePaths.Count > 0)
                return _visiblePaths;

            _visiblePaths.Add(Assembly.Location);
            if (_references.Count > 0)
            {
                var childPaths = _references.Select(x => x.Assembly.Location);
                _visiblePaths.AddRange(childPaths);
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

        _references.Add(child);
        _visiblePaths.Clear(); // invalidate path list
        _visibleDirectories.Clear(); // invalidate visible directories list
        return true;
    }

    private IEnumerable<AssemblyTreeNode> GetAssemblyNodes(AssemblyLoadContext ctx)
    {
        yield return this;

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
        return NameStr == nameToSearchFor;
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

        var fileName = nameToSearchFor + ".dll";
        List<string> searchedDirectories = [];
       
        foreach (var node in nodes)
        {
            foreach (var dir in node.VisibleDirectories)
            {
                if (searchedDirectories.Contains(dir))
                    continue;

                searchedDirectories.Add(dir);
                var path = Path.Combine(dir, fileName);
                try
                {
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var newAssembly = ctx.LoadFromAssemblyPath(path);
                    var name = ctx.Name ?? "unknown context";
                    var foundAssembly = new AssemblyTreeNode(newAssembly, $"{name} -> {node.NameStr}");
                    node.AddReferenceTo(foundAssembly);

                    assembly = foundAssembly;
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
}