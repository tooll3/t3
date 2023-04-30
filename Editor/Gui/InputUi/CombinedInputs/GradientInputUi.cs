using System.Numerics;
using System.Runtime.Serialization;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
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
        
        protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref Gradient gradient)
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
            
            var modified1= GradientEditor.Draw(ref gradient, drawList, area, cloneIfModified: input.IsDefault);

            var modified= modified1 ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
            if (input.IsDefault && (modified & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing)
            {
                Log.Debug("no longer default");
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
