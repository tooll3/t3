using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Gui.OutputUi
{
    public class FloatListOutputUi : OutputUi<List<float>>
    {
        public override IOutputUi Clone()
        {
            return new FloatListOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        }
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<List<float>> typedSlot)
            {
                var outputString = string.Join(", ", typedSlot.Value);
                ImGui.Text($"{outputString}");
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}