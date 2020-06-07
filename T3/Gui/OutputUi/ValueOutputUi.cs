using System;
using System.Diagnostics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Gui.OutputUi
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
                        ImGui.Text(s);
                        break;
                    default:
                    {
                        var t = value?.ToString();
                        ImGui.Text(t ?? typeof(T).ToString());
                        break;
                    }
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}