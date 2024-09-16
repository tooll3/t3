using ImGuiNET;

namespace T3.Editor.Gui.InputUi.SingleControl
{
    public class FloatListInputUi : SingleControlInputUi<List<float>>
    {
        public override IInputUi Clone()
        {
            return new FloatListInputUi
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy
                   };
        }

        protected override bool DrawSingleEditControl(string name, ref List<float> list)
        {
            string outputString;
            lock (list)
            {
                outputString = list == null ? "NULL" :  string.Join(", ", list);
            }
            ImGui.TextUnformatted($"{outputString}");
            return false;
        }

        protected override void DrawReadOnlyControl(string name, ref List<float> list)
        {
            string outputString;
            lock (list)
            {
                outputString = (list == null) ? "NULL" :  string.Join(", ", list);
            }
            ImGui.TextUnformatted($"{outputString}");
        }
    }
}