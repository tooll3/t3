using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class StringListOutputUi : OutputUi<List<string>>
    {
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<List<string>> typedSlot)
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