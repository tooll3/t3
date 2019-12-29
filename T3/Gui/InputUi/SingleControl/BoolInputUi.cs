using ImGuiNET;

namespace T3.Gui.InputUi.SingleControl
{
    public class BoolInputUi : SingleControlInputUi<bool>
    {
        public override IInputUi Clone()
        {
            return new BoolInputUi()
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
        }

        protected override bool DrawSingleEditControl(string name, ref bool value)
        {
            return ImGui.Checkbox("##boolParam", ref value);
        }

        protected override void DrawReadOnlyControl(string name, ref bool value)
        {
            ImGui.Text(value.ToString());
        }
    }
}