namespace T3.Editor.UiModel.InputsAndTypes;

public static class TypeUiRegistry
{
    private static readonly Dictionary<Type, UiProperties> _entries = new();

    public static UiProperties GetPropertiesForType(Type type)
    {
        return type != null && _entries.TryGetValue(type, out var properties) ? properties : UiProperties.Default;
    }

    internal static bool TryGetPropertiesForType(Type type, out UiProperties properties)
    {
        return _entries.TryGetValue(type, out properties);
    }
        
    internal static void SetProperties(Type type, UiProperties properties)
    {
        _entries[type] = properties;
    }
}