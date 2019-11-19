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
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<float> typedSlot)
            {
                StartInvalidation(slot);
                _evaluationContext.Reset();
                var value = typedSlot.GetValue(_evaluationContext);
                
                if (float.IsNaN(_dampedValue))
                    _dampedValue = 0;
                
                _dampedValue = Im.Lerp(_dampedValue,value,0.01f);

                if (slot != _lastLost)
                {
                    _lastLost = slot;
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
        private ISlot _lastLost;
        private CurvePlot _curve = new CurvePlot(100);
    }
}