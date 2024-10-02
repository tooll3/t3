using System;
using System.Collections.Generic;

namespace T3.Editor.Gui.InputUi
{
    public static class TypeUiRegistry
    {
        public static Dictionary<Type, ITypeUiProperties> Entries { get; } = new();

        public static ITypeUiProperties GetPropertiesForType(Type type)
        {
            var t = _fallBackTypeUiProperties;
            if (type != null)
                Entries.TryGetValue(type, out t);
            return t;
        }

        private static readonly ITypeUiProperties _fallBackTypeUiProperties = new FallBackUiProperties();
    }
}