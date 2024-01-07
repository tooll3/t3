using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;

namespace T3.Core.Compilation;

public class AssemblyInformation
{
    public readonly string Name;
    public readonly string Path;
    public readonly string Directory;
    public readonly AssemblyName AssemblyName;
    public readonly Assembly Assembly;

    public bool TryGetType(Guid typeId, out Type type) => _operatorTypes.TryGetValue(typeId, out type);

    public IReadOnlyDictionary<Guid, Type> OperatorTypes => _operatorTypes;
    private readonly Dictionary<Guid, Type> _operatorTypes;
    private readonly Dictionary<string, Type> _types;
    public IReadOnlyCollection<Type> Types => _types.Values;

    public Guid HomeGuid { get; private set; } = Guid.Empty;
    public bool HasHome => HomeGuid != Guid.Empty;

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
                                           var gotGuid = TryGetGuidOfType(x, out var id);
                                           var isHome = x.GetCustomAttribute<HomeAttribute>() is not null;
                                           if (isHome && gotGuid)
                                           {
                                               HomeGuid = id;
                                           }

                                           return new GuidInfo(gotGuid, id, x);
                                       })
                               .Where(x => x.HasGuid)
                               .ToDictionary(x => x.Guid, x => x.Type);
    }

    public static bool TryGetGuidOfType(Type newType, out Guid guid)
    {
        var guidAttributes = newType.GetCustomAttributes(typeof(GuidAttribute), false);
        if (guidAttributes.Length == 0)
        {
            Log.Error($"Type {newType.Name} has no GuidAttribute");
            guid = Guid.Empty;
            return false;
        }

        if (guidAttributes.Length > 1)
        {
            Log.Error($"Type {newType.Name} has multiple GuidAttributes");
            guid = Guid.Empty;
            return false;
        }

        var guidAttribute = (GuidAttribute)guidAttributes[0];
        var guidString = guidAttribute.Value;

        if (!Guid.TryParse(guidString, out guid))
        {
            Log.Error($"Type {newType.Name} has invalid GuidAttribute");
            return false;
        }

        return true;
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