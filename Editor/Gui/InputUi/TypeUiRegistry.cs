using System;
using System.Collections.Generic;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.InputUi
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
        internal static Color ColorForPoints = new Color(0.625f, 0.450f, 0.554f, 1.000f);
        internal static Color ColorForString = new Color(0.468f, 0.586f, 0.320f, 1.000f);
        internal static Color ColorForTextures = new Color(0.853f, 0.313f, 0.855f, 1.000f);
        internal static Color ColorForCommands = new Color(0.132f, 0.722f, 0.762f, 1.000f);
    }
}