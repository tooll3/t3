using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class StringListOutputUi : OutputUi<List<string>>
    {
        public override void DrawValue(ISlot slot, bool recompute=true)
        {
            if (slot is Slot<List<string>> typedSlot)
            {
                if (recompute)
                {
                    StartInvalidation(slot);
                    _evaluationContext.Reset();
                }
                var list = recompute
                                ? typedSlot.GetValue(_evaluationContext) 
                                : typedSlot.Value;

                //var list = typedSlot.GetValue(_evaluationContext);
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