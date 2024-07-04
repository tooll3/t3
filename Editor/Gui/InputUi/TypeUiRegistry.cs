namespace T3.Editor.Gui.InputUi
{
    public static class TypeUiRegistry
    {
        private static readonly Dictionary<Type, UiProperties> Entries = new();

        public static UiProperties GetPropertiesForType(Type type)
        {
            return Entries.TryGetValue(type, out var properties) ? properties : UiProperties.Default;
        }
        
        public static bool TryGetPropertiesForType(Type type, out UiProperties properties)
        {
            return Entries.TryGetValue(type, out properties);
        }
        
        internal static void SetProperties(Type type, UiProperties properties)
        {
            Entries[type] = properties;
        }
    }
}