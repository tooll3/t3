using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Selection;

namespace T3.Editor.Gui.OutputUi
{
    public interface IOutputUi : ISelectableCanvasObject
    {
        Symbol.OutputDefinition OutputDefinition { get; set; }
        Type Type { get; }
        IOutputUi Clone();
        void DrawValue(ISlot slot, EvaluationContext context, bool recompute = true);
    }
}