using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class ValueOutputUi<T> : OutputUi<T>
    {
        public override void DrawValue(ISlot slot, bool recompute = true)
        {
            if (slot is Slot<T> typedSlot)
            {
                if (recompute)
                {
                    StartInvalidation(slot);
                    _evaluationContext.Reset();
                }
                var value = recompute
                                ? typedSlot.GetValue(_evaluationContext) 
                                : typedSlot.Value;

                if (value == null)
                {
                    ImGui.Text($"{typeof(T)}");
                }
                else
                {
                    ImGui.Text($"{value}");
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}