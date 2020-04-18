using System;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
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
                       // Usage = Usage
                   };
        }
        
        protected override InputEditStateFlags DrawEditControl(string name, ref T3.Core.Animation.Curve curve)
        {
            if (curve == null)
            {
                // value was null!
                ImGui.Text(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }
            
            ImGui.Dummy(Vector2.One);    // Add Line Break
            
            CurveInputEditing.DrawCanvasForCurve(curve);

            InputEditStateFlags inputEditStateFlags = InputEditStateFlags.Nothing;

            inputEditStateFlags |= ImGui.IsItemClicked() ? InputEditStateFlags.Started : InputEditStateFlags.Nothing;
            inputEditStateFlags |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditStateFlags.Finished : InputEditStateFlags.Nothing;

            return inputEditStateFlags;
        }
        
        protected override void DrawReadOnlyControl(string name, ref T3.Core.Animation.Curve value)
        {
        }
    }
}