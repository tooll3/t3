using System;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;

namespace T3.Gui.InputUi
{
    public class CurveInputUi : InputValueUi<T3.Core.Animation.Curve>
    {
        public override IInputUi Clone()
        {
            return new CurveInputUi
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy,
                       Size = Size,
                   };
        }
        
        protected override InputEditStateFlags DrawEditControl(string name, ref Curve curve, bool isDefaultValue)
        {
            if (curve == null)
            {
                // value was null!
                ImGui.Text(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }
            
            ImGui.Dummy(Vector2.One);    // Add Line Break

            InputEditStateFlags inputEditStateFlags = InputEditStateFlags.Nothing;

            if (isDefaultValue)
            {
                curve = curve.Clone(); // cloning here every frame when the curve is default would be awkward, so can this be cached somehow?
                inputEditStateFlags |= InputEditStateFlags.Modified; // the will clear the IsDefault flag after editing
            }

            // TODO: with the return value of this function the inputEditStateFlags should somehow be set, or selfmade editing controls for reference types
            // handle cloning by themselves when previously in default state. Needs to be discussed
            CurveInputEditing.DrawCanvasForCurve(curve);

            return inputEditStateFlags;
        }
        
        protected override void DrawReadOnlyControl(string name, ref Curve value)
        {
        }
    }
}
