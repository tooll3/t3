namespace T3.Editor.Gui.InputUi
{
    internal static class InputUiFactory
    {
        public static readonly Dictionary<Type, Func<IInputUi>> Entries = new();
    }
}