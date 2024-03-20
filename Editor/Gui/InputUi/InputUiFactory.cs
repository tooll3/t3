namespace T3.Editor.Gui.InputUi
{
    public static class InputUiFactory
    {
        public static Dictionary<Type, Func<IInputUi>> Entries { get; } = new();
    }
}