using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace Editor.Gui.OutputUi
{
    public class StringListOutputUi : OutputUi<List<string>>
    {
        public override IOutputUi Clone()
        {
            return new StringListOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        }
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<List<string>> typedSlot)
            {
                var stringValue = typedSlot.Value;
                var outputString = stringValue == null ? "null" : string.Join(", ", stringValue);
                ImGui.TextUnformatted(outputString);
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}