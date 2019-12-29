using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class ValueOutputUi<T> : OutputUi<T>
    {
        public override IOutputUi Clone()
        {
            return new ValueOutputUi<T>
                   {
                       OutputDefinition = OutputDefinition,
                       IsSelected = IsSelected,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        }

        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                var value = typedSlot.Value;
                ImGui.Text(value == null ? $"{typeof(T)}" : $"{value}");
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}