#nullable enable
using System;
using System.Linq;
using System.Reflection;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Core.Compilation;

public readonly record struct OutputSlotInfo
{
    internal readonly Type? OutputDataType;

    internal OutputSlotInfo(string Name, OutputAttribute Attribute, Type type, Type[] GenericArguments, FieldInfo Field, int GenericTypeIndex)
    {
        this.Name = Name;
        this.Attribute = Attribute;
        this.GenericArguments = GenericArguments;
        this.Field = Field;
        this.GenericTypeIndex = GenericTypeIndex;
        OutputDataType = GetOutputDataType(type);
        IsGeneric = GenericTypeIndex >= 0;
    }

    public string Name { get; }
    public OutputAttribute Attribute { get; }
    public Type[] GenericArguments { get; }
    public FieldInfo Field { get; }

    public int GenericTypeIndex { get; }
    public bool IsGeneric { get; }

    private static Type? GetOutputDataType(Type fieldType)
    {
        var interfaces = fieldType.GetInterfaces();
        Type? foundInterface = null;
        foreach (var i in interfaces)
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOutputDataUser<>))
            {
                foundInterface = i;
                break;
            }
        }

        return foundInterface?.GetGenericArguments().Single();
    }
}