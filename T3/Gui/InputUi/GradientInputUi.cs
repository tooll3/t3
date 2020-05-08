using System;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.InputUi
{
    public class GradientInputUi : InputValueUi<T3.Core.DataTypes.Gradient>
    {
        public override IInputUi Clone()
        {
            return new GradientInputUi
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy,
                       Size = Size,
                   };
        }
        
        protected override InputEditStateFlags DrawEditControl(string name, ref Gradient gradient, bool isDefaultValue)
        {
            if (gradient == null)
            {
                // value was null!
                ImGui.Text(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }
            
            var inputEditStateFlags = InputEditStateFlags.Nothing;

            if (isDefaultValue)
            {
                gradient = gradient.Clone(); // cloning here every frame when the curve is default would be awkward, so can this be cached somehow?
                inputEditStateFlags |= InputEditStateFlags.Modified; // the will clear the IsDefault flag after editing
            }
            
            inputEditStateFlags |= DrawEditor(gradient);

            return inputEditStateFlags;
        }

        // TODO: Implement proper edit flags and Undo
        private static InputEditStateFlags DrawEditor(Gradient gradient)
        {
            var size = new Vector2(ImGui.GetContentRegionAvail().X, 
                                   ImGui.GetFrameHeight());
            var area = new ImRect(ImGui.GetCursorScreenPos(), 
                                  ImGui.GetCursorScreenPos() + size);
            var modified= GradientEditor.Draw(gradient, ImGui.GetWindowDrawList(), area);
            return modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }
        
        
        protected override void DrawReadOnlyControl(string name, ref Gradient value)
        {
        }
    }
}
