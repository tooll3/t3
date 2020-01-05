using System;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.OutputUi
{
    public interface IOutputUi : ISelectableNode
    {
        Symbol.OutputDefinition OutputDefinition { get; set; }
        Type Type { get; }
        IOutputUi Clone();
        void DrawValue(ISlot slot, EvaluationContext context, bool recompute = true);
    }
}