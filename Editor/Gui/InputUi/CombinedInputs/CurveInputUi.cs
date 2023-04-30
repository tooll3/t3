using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Operator;

namespace T3.Editor.Gui.InputUi.CombinedInputs
{
    public class CurveInputUi : InputValueUi<Curve>
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
        
        protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref Curve curve)
        {
            if (curve == null)
            {
                // value was null!
                ImGui.TextUnformatted(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }
            
            ImGui.Dummy(Vector2.One);    // Add Line Break

            return CurveInputEditing.DrawCanvasForCurve(curve, T3Ui.EditingFlags.PreventZoomWithMouseWheel);
        }
        
        protected override void DrawReadOnlyControl(string name, ref Curve value)
        {
        }
    }
}
