
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
        }
    }
}