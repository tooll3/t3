using ImGuiNET;
using System;
using System.Diagnostics;
using T3.Core.Operator;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.OutputUi
{
    public class FloatOutputUi : OutputUi<float>
    {
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<float> typedSlot)
            {
                var value = typedSlot.Value;

                if (float.IsNaN(_dampedValue))
                    _dampedValue = 0;

                _dampedValue = Im.Lerp(_dampedValue, value, 0.01f);

                if (slot != _lastSlot)
                {
                    _lastSlot = slot;
                    _curve.Reset(value);
                }

                _curve.Draw(value);
                ImGui.Text($"~{_dampedValue:0.00}");
            }
            else
            {
                Debug.Assert(false);
            }
        }

        private float _dampedValue = 0;
        private ISlot _lastSlot;
        private readonly CurvePlot _curve = new CurvePlot("", resolution: 200, width:150);
    }
}