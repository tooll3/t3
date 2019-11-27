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
                
                if (slot != _lastSlot)
                {
                    _lastSlot = slot;
                    _curve.Reset(value);
                }

                _curve.Draw(value);
            }
            else
            {
                Debug.Assert(false);
            }
        }
        
        private ISlot _lastSlot;
        private readonly CurvePlot _curve = new CurvePlot("", resolution: 200, width:150);
    }
}