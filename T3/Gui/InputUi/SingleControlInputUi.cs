using ImGuiNET;

namespace T3.Gui.InputUi
{
    public abstract class SingleControlInputUi<T> : InputValueUi<T>
    {
        public abstract bool DrawSingleEditControl(string name, ref T value);

        protected override InputEditState DrawEditControl(string name, ref T value)
        {
            bool valueModified = DrawSingleEditControl(name, ref value);

            InputEditState inputEditState = InputEditState.Nothing;
            inputEditState |= ImGui.IsItemClicked() ? InputEditState.Started : InputEditState.Nothing;
            inputEditState |= valueModified ? InputEditState.Modified : InputEditState.Nothing;
            inputEditState |= ImGui.IsItemDeactivatedAfterEdit() ? InputEditState.Finished : InputEditState.Nothing;

            return inputEditState;
        }
    }
}