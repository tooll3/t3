using System;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.OutputUi
{
    public interface IOutputUi : ISelectable
    {
        Symbol.OutputDefinition OutputDefinition { get; set; }
        Type Type { get; }
        void DrawValue(ISlot slot, bool recompute = true);
    }
}