
using ImGuiNET;
using T3.Gui.InputUi;

namespace t3.Gui.InputUi.SimpleInputUis
{
    public class FallbackInputUi<T> : InputValueUi<T>
    {
        public override IInputUi Clone()
        {
            return new FallbackInputUi<T>
                   {
                       InputDefinition = InputDefinition,
                       Parent = Parent,
                       PosOnCanvas = PosOnCanvas,
                       Relevancy = Relevancy,
                       Size = Size
                   };
        }

        protected override InputEditStateFlags DrawEditControl(string name, ref T value)
        {
            ImGui.TextUnformatted(""); // Print an empty text to force layout to next line
            return InputEditStateFlags.Nothing;
        }

        protected override void DrawReadOnlyControl(string name, ref T value)
        {
            // ToDo: it would be great to print the name of the connected op here.
            ImGui.TextUnformatted(""); // Print an empty text to force layout to next line
        }
    }
}