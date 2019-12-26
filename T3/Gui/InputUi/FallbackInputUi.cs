
using ImGuiNET;

namespace T3.Gui.InputUi
{
    public class FallbackInputUi<T> : InputValueUi<T>
    {
        protected override InputEditStateFlags DrawEditControl(string name, ref T value)
        {
            ImGui.Text(""); // Print an empty text to force layout to next line
            return InputEditStateFlags.Nothing;
        }

        protected override void DrawValueDisplay(string name, ref T value)
        {
            // ToDo: it would be great to print the name of the connected op here.
            ImGui.Text(""); // Print an empty text to force layout to next line
        }
    }
}