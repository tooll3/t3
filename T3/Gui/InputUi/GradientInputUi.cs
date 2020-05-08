using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.InputUi
{
    public class GradientInputUi : InputValueUi<Gradient>
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
        
        protected override InputEditStateFlags DrawEditControl(string name, ref Gradient gradient)
        {
            if (gradient == null)
            {
                // value was null!
                ImGui.Text(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }
            
            return DrawEditor(gradient);
        }

        // TODO: Implement proper edit flags and Undo
        private static InputEditStateFlags DrawEditor(Gradient gradient)
        {
            var size = new Vector2(ImGui.GetContentRegionAvail().X - GradientEditor.StepHandleSize.X, 
                                   ImGui.GetFrameHeight());
            var area = new ImRect(ImGui.GetCursorScreenPos() + new Vector2(GradientEditor.StepHandleSize.X * 0.5f,0), 
                                  ImGui.GetCursorScreenPos() + size);
            var modified= GradientEditor.Draw(gradient, ImGui.GetWindowDrawList(), area);
            return modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }
        
        
        protected override void DrawReadOnlyControl(string name, ref Gradient value)
        {
        }
    }
}
