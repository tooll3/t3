using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.InputUi
{
    public class StructuredListInputUi : InputValueUi<StructuredList>
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
        
        protected override InputEditStateFlags DrawEditControl(string name, ref StructuredList slist)
        {
            if (slist == null)
            {
                // value was null!
                ImGui.TextUnformatted(name + " is null?!");
                return InputEditStateFlags.Nothing;
            }
            
            return DrawEditor(slist);
        }

        // TODO: Implement proper edit flags and Undo
        private static InputEditStateFlags DrawEditor(StructuredList slist)
        {
            // var size = new Vector2(ImGui.GetContentRegionAvail().X - GradientEditor.StepHandleSize.X, 
            //                        ImGui.GetFrameHeight());
            //
            // var area = new ImRect(ImGui.GetCursorScreenPos() + new Vector2(GradientEditor.StepHandleSize.X * 0.5f,0), 
            //                       ImGui.GetCursorScreenPos() + size);
            //
            // //var modified= GradientEditor.Draw(slist, ImGui.GetWindowDrawList(), area);
            var modified = TableView.TableList.Draw(slist);
            return modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }
        
        
        protected override void DrawReadOnlyControl(string name, ref StructuredList slist)
        {
            if (slist == null)
            {
                ImGui.TextUnformatted("NULL?");
                return;
            }
            //ImGui.TextUnformatted($"{value.Type.Name}[{value.GetCount()}]");
            ImGui.NewLine();
            
                        var modified = TableView.TableList.Draw(slist);
                        //return modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;            var modified = TableView.TableList.Draw(slist);
                        //return modified ? InputEditStateFlags.Modified : InputEditStateFlags.Nothing;
        }
    }
}