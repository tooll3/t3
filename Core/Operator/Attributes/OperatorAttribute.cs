using System;

namespace T3.Core.Operator.Attributes;

public class OperatorAttribute : Attribute
{
    public Guid Id { get; set; }
    public string Guid { get => Id.ToString(); set => Id = System.Guid.Parse(value); }
}