using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.Interaction;

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
        
        protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref Curve curve, bool readOnly)
        {
            if (curve == null)
            {
                // value was null!
                ImGui.TextUnformatted(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }
            
            ImGui.Dummy(Vector2.One);    // Add Line Break

            var keepPositionForIcon = ImGui.GetCursorPos();
            
            var cloneIfModified = input.IsDefault;
            var modified= CurveInputEditing.DrawCanvasForCurve(ref curve, input, cloneIfModified, T3Ui.EditingFlags.PreventZoomWithMouseWheel);

            if (CurveEditPopup.DrawPopupIndicator(input, ref curve, keepPositionForIcon, cloneIfModified, out var popupResult))
            {
                modified = popupResult;
            }

            if (cloneIfModified && (modified & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing)
            {
                input.IsDefault = false;
            } 
            return modified;
        }
        
        protected override void DrawReadOnlyControl(string name, ref Curve value)
        {
        }
    }
}
