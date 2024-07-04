namespace T3.Editor.Gui.InputUi
{
    public static class TypeUiRegistry
    {
        private static readonly Dictionary<Type, ITypeUiProperties> Entries = new();

        public static ITypeUiProperties GetPropertiesForType(Type type)
        {
            return Entries.TryGetValue(type, out var properties) ? properties : _fallBackTypeUiProperties;
        }
        
        public static bool TryGetPropertiesForType(Type type, out ITypeUiProperties properties)
        {
            return Entries.TryGetValue(type, out properties);
        }
        
        internal static void SetProperties(Type type, ITypeUiProperties properties)
        {
            Entries[type] = properties ?? _fallBackTypeUiProperties;
        }

        private static readonly ITypeUiProperties _fallBackTypeUiProperties = new ValueUiProperties();
    }
}