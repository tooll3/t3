using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class FloatListOutputUi : OutputUi<List<float>>
    {
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<List<float>> typedSlot)
            {
                StartInvalidation(slot);
                _evaluationContext.Reset();
                var list = typedSlot.GetValue(_evaluationContext);
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