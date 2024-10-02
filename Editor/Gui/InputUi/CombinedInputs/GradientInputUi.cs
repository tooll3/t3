using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.InputUi.CombinedInputs
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
        
        protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref Gradient gradient, bool readOnly)
        {
            if (gradient == null)
            {
                ImGui.TextUnformatted(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }

            var size = new Vector2(ImGui.GetContentRegionAvail().X - GradientEditor.StepHandleSize.X, 
                                   ImGui.GetFrameHeight());
            var area = new ImRect(ImGui.GetCursorScreenPos() + new Vector2(GradientEditor.StepHandleSize.X * 0.5f,0), 
                                  ImGui.GetCursorScreenPos() + size);
            var drawList = ImGui.GetWindowDrawList();

            var cloneIfModified = input.IsDefault;
            var modified= GradientEditor.Draw(ref gradient, drawList, area, cloneIfModified);

            if (cloneIfModified && modified.HasFlag(InputEditStateFlags.Modified))
            {
                input.IsDefault = false;
            } 
            return modified;
        }

        // TODO: Implement proper edit flags and Undo

        protected override void DrawReadOnlyControl(string name, ref Gradient value)
        {
            ImGui.NewLine();
        }
    }
}
