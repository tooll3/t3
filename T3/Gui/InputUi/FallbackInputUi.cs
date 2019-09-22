
using ImGuiNET;

namespace T3.Gui.InputUi
{
    public class FallbackInputUi<T> : InputValueUi<T>
    {
        protected override InputEditState DrawEditControl(string name, ref T value)
        {
            return InputEditState.Nothing;
        }

        protected override void DrawValueDisplay(string name, ref T value)
        {
            // ToDo: it would be greate to print the name of the connected op here.
            ImGui.Text(""); // Print an empty text to force layout to next line
        }
    }
}