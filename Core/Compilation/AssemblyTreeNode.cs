#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using T3.Core.Logging;

namespace T3.Core.Compilation;

internal sealed class AssemblyTreeNode
{
    public readonly Assembly Assembly;
    public readonly AssemblyName Name;
    public readonly string NameStr;

    private readonly List<AssemblyTreeNode> _references = [];

    public readonly AssemblyLoadContext LoadContext;

    private readonly Lock _assemblyLock = new();
    
    internal readonly record struct DllReference(string Path, string Name, AssemblyName AssemblyName);

    private List<DllReference>? _unreferencedDlls;
    
    private readonly Lock _unreferencedLock = new();

    private List<DllReference> UnreferencedDlls
    {
        get
        {
            lock (_unreferencedLock)
            {
                if (_unreferencedDlls is not null)
                    return _unreferencedDlls;

                _unreferencedDlls = [];
                // locate "not used" dlls in the directory without loading them
                var directory = Path.GetDirectoryName(Assembly.Location);
                foreach (var file in Directory.GetFiles(directory!, "*.dll", SearchOption.AllDirectories))
                {
                    bool skip = false;
                    foreach (var dep in _references)
                    {
                        try
                        {
                            if (file == dep.Assembly.Location)
                            {
                                skip = true;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error($"{_parentName}: Exception getting assembly location: {e}");
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
    public AssemblyTreeNode(Assembly assembly, AssemblyLoadContext parent)
    {
        Assembly = assembly;
        Name = assembly.GetName();
        NameStr = Name.GetNameSafe();

        _parentName = parent.Name!;
        LoadContext = parent;

        // if (debug && !node.NameStr.StartsWith("System")) // don't log system assemblies - too much log spam for things that are probably not error-prone
        //Log.Debug($"{parent}: Loaded assembly {NameStr} from {assembly.Location}");
    }

    private DllReference Reference => new(Assembly.Location, NameStr, Name);

    // this should only be called externally
    /// <summary>
    /// This should only be called externally or on non-root nodes of the same context
    /// It establishes a relationship between the assemblies and returns true
    /// if a dependency is formed between separate load contexts
    /// </summary>
    /// <param name="child"></param>
    /// <returns></returns>
    public bool AddReferenceTo(AssemblyTreeNode child)
    {
        lock (_assemblyLock)
        {
            if (_references.Contains(child))
            {
                return false;
            }

            lock (_unreferencedLock)
            {
                if (UnreferencedDlls.Contains(child.Reference))
                {
                    UnreferencedDlls.Remove(child.Reference);
                }
            }

            _references.Add(child);
        }

        return true;
    }

    public bool TryFindUnreferenced(string nameToSearchFor, [NotNullWhen(true)] out AssemblyTreeNode? assembly)
    {
        // check unreferenced dlls
        lock (_assemblyLock)
        {
            lock (_unreferencedLock)
            {
                foreach (var dll in UnreferencedDlls)
                {
                    if (dll.Name != nameToSearchFor)
                        continue;

                    try
                    {
                        if (!File.Exists(dll.Path))
                        {
                            Log.Warning($"{_parentName}: Could not find assembly `{dll.Path}`");
                            continue;
                        }

                        var newAssembly = LoadContext.LoadFromAssemblyPath(dll.Path);
                        assembly = new AssemblyTreeNode(newAssembly, LoadContext);
                        AddReferenceTo(assembly);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"{_parentName}: Exception loading assembly: {e}");
                    }
                }
            }
        }

        /*lock (_assemblyLock)
        {
            // check those of our references
            foreach (var node in _references)
            {
                if (node.LoadContext != LoadContext)
                    continue;

                // search recursively
                if (node.TryFindUnreferenced(nameToSearchFor, out assembly))
                    return true;
            }
        }*/

        assembly = null;
        return false;
    }

    public bool TryFindExisting(string nameToSearchFor, [NotNullWhen(true)] out AssemblyTreeNode? assembly)
    {
        if (NameStr == nameToSearchFor)
        {
            assembly = this;
            return true;
        }

        lock (_assemblyLock)
        {
            foreach (var node in _references)
            {
                if (node.TryFindExisting(nameToSearchFor, out assembly))
                    return true;
            }
        }

        assembly = null;
        return false;
    }

    public void Unload()
    {
        Log.Debug($"{_parentName}: Unloading assembly {NameStr}");
        lock (_assemblyLock)
        {
            foreach (var node in _references.Where(x => x.LoadContext == LoadContext))
            {
                node.Unload();
            }

            _references.Clear();
            lock (_assemblyLock)
                _unreferencedDlls = null;
        }
    }
}