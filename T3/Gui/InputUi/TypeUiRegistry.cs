using System;
using System.Collections.Generic;

namespace T3.Gui.InputUi
{
    public static class TypeUiRegistry
    {
        public static Dictionary<Type, ITypeUiProperties> Entries { get; } = new Dictionary<Type, ITypeUiProperties>();

        public static ITypeUiProperties GetPropertiesForType(Type type)
        {
            var t = FallBackTypeUiProperties;
            if (type != null)
                Entries.TryGetValue(type, out t);
            return t;
        }

        public static ITypeUiProperties FallBackTypeUiProperties = new FallBackUiProperties();

        internal static Color ColorForValues = new Color(0.525f, 0.550f, 0.554f, 1.000f);
        internal static Color ColorForString = new Color(0.468f, 0.586f, 0.320f, 1.000f);
        internal static Color ColorForTextures = new Color(0.803f, 0.313f, 0.785f, 1.000f);
    }
}