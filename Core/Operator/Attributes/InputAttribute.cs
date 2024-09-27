using System;

namespace T3.Core.Operator.Attributes;

public class InputAttribute : OperatorAttribute
{
    public Type MappedType { get; set; }
}