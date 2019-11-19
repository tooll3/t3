using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class FloatListOutputUi : OutputUi<List<float>>
    {
        public override void DrawValue(ISlot slot, bool recompute = true)
        {
            if (slot is Slot<List<float>> typedSlot)
            {
                if (recompute)
                {
                    StartInvalidation(slot);
                    _evaluationContext.Reset();
                }
                var list = recompute
                                ? typedSlot.GetValue(_evaluationContext) 
                                : typedSlot.Value;

                var outputString = string.Join(", ", list);
                ImGui.Text($"{outputString}");
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}