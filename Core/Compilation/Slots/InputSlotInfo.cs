#nullable enable
using System;
using System.Reflection;
using T3.Core.Operator.Attributes;

namespace T3.Core.Compilation;

public readonly record struct InputSlotInfo(
    string Name,
    InputAttribute Attribute,
    Type[] GenericArguments,
    FieldInfo Field,
    bool IsMultiInput,
    int GenericTypeIndex)
{
    public bool IsGeneric => GenericTypeIndex >= 0;
}