using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class ValueOutputUi<T> : OutputUi<T>
    {
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var value = typedSlot.GetValue(_evaluationContext);
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