using ImGuiNET;
using T3.Core.Operator;

namespace T3.Editor.Gui.InputUi.SimpleInputUis
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

        protected override InputEditStateFlags DrawEditControl(string name, Symbol.Child.Input input, ref T value, bool readOnly)
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