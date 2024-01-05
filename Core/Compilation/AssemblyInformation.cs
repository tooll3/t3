using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;

namespace T3.Core.Compilation;

public class AssemblyInformation
{
    public readonly string Name;
    public readonly string Path;
    public readonly string Directory;
    public readonly AssemblyName AssemblyName;
    public readonly Assembly Assembly;

    public bool TryGetType(Guid typeId, out Type type) => _operatorTypes.TryGetValue(typeId, out type);

    private readonly Dictionary<Guid, Type> _operatorTypes;
    private readonly Dictionary<string, Type> _types;
    public IReadOnlyCollection<Type> Types => _types.Values;

    public AssemblyInformation(string path, AssemblyName assemblyName, Assembly assembly)
    {
        Name = assemblyName.Name;
        Path = path;
        AssemblyName = assemblyName;
        Assembly = assembly;
        Directory = System.IO.Path.GetDirectoryName(path);
        
        try
        {
            _types = assembly.GetExportedTypes().ToDictionary(type => type.FullName, type => type);
        }
        catch (Exception e)
        {
            Log.Warning($"Failed to load types from assembly {assembly.FullName}\n{e.Message}\n{e.StackTrace}");
            _types = new Dictionary<string, Type>();
            _operatorTypes = new Dictionary<Guid, Type>();
            return;
        }

        _operatorTypes = _types.Values
                               .Where(x => x.IsAssignableTo(typeof(Instance)))
                               .Select(x =>
                                       {
                                           var gotGuid = SymbolData.TryGetGuidOfType(x, out var id);
                                           return new GuidInfo(gotGuid, id, x);
                                       })
                               .Where(x => x.HasGuid)
                               .ToDictionary(x => x.Guid, x => x.Type);
    }

    public void UpdateType(Type updated, Guid guid = default)
    {
        _types[updated.FullName!] = updated;

        if (guid != default)
            _operatorTypes[guid] = updated;
    }

    readonly struct GuidInfo
    {
        public readonly bool HasGuid;
        public readonly Guid Guid;
        public readonly Type Type;

        public GuidInfo(bool hasGuid, Guid guid, Type type)
        {
            HasGuid = hasGuid;
            Guid = guid;
            this.Type = type;
        }
    }
}