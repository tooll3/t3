using System;
using T3.Core.Operator;

namespace T3.Core.DataTypes;

public class Command
{
    public Action<EvaluationContext> PrepareAction { get; init; }
    public Action<EvaluationContext> RestoreAction { get; set; }
}