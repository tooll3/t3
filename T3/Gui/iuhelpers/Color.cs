using System.Numerics;
using ImGuiNET;

namespace t3.iuhelpers
{
    /// <summary>
    /// Simplifieds defining and converting colors from float -> uint
    /// </summary>
    struct Color
    {
        public Vector4 Values;

        /// <summary>
        /// Defines a gray color
        /// </summary>
        public Color(float gray)
        {
            Values = new Vector4(gray, gray, gray, 1);
        }

        public Color(float r, float g, float b, float a)
        {
            Values = new Vector4(r, g, b, a);
        }

        public Color(int r, int g, int b, int a = 255)
        {
            Values = new Vector4(r, g, b, a) / 255f;
        }

        public Color(Vector4 values)
        {
            Values = values;
        }

        public readonly static Color White = new Color(1, 1, 1, 1);
        public readonly static Color Black = new Color(0, 0, 0, 1);
        public readonly static Color Red = new Color(1, 0.3f, 0.2f, 1);
        public readonly static Color Blue = new Color(0.3f, 0.4f, 1, 1);
        public uint ToUint() { return ImGui.ColorConvertFloat4ToU32(Values); }
        // public static uint ToUint(float r, float g, float b, float a = 1) { return ImGui.GetColorU32(new Vector4(r, g, b, a)); }
        // public static uint ToUint(int r, int g, int b, int a = 255) { var sc = 1 / 255f; return ImGui.GetColorU32(new Vector4(r * sc, g * sc, b * sc, a * sc)); }
    }
}