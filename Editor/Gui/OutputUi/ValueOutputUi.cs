using System.Diagnostics;
using ImGuiNET;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.OutputUi
{
    public class ValueOutputUi<T> : OutputUi<T>
    {
        public override IOutputUi Clone()
        {
            return new ValueOutputUi<T>
                   {
                       OutputDefinition = OutputDefinition,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        }

        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<T> typedSlot)
            {
                var value = typedSlot.Value;
                switch (value)
                {
                    case float f:
                        ImGui.Value("", f);
                        break;
                    case string s:
                        ImGui.BeginChild("scrollable");
                        ImGui.TextUnformatted(s);
                        ImGui.EndChild();
                        break;
                    default:
                        var t = value?.ToString();
                        ImGui.TextUnformatted(t ?? typeof(T).ToString());
                        break;
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}