using System.Numerics;
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

        protected override InputEditStateFlags DrawEditControl(string name, SymbolChild.Input input, ref T value, bool readOnly)
        {
            ImGui.TextUnformatted(""); // Print an empty text to force layout to next line
            return InputEditStateFlags.Nothing;
        }

        protected override void DrawReadOnlyControl(string name, ref T value)
        {
            ImGui.Button($"{name}", new Vector2(-1,0));
            //ImGui.Text($"{name}"); // Print an empty text to force layout to next line
        }
    }
}