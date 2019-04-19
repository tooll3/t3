using ImGuiNET;
using System.Numerics;

namespace T3.Gui
{
    //static class TColors
    //{
    //    public readonly static Vector4 White = new Vector4(1, 1, 1, 1);
    //    public readonly static Vector4 Black = new Vector4(0, 0, 0, 1);
    //    public readonly static TColor White2 = new TColor();
    //    public static uint ToUint(float r, float g, float b, float a = 1) { return ImGui.GetColorU32(new Vector4(r, g, b, a)); }
    //    public static uint ToUint(int r, int g, int b, int a = 255) { var sc = 1 / 255f; return ImGui.GetColorU32(new Vector4(r * sc, g * sc, b * sc, a * sc)); }
    //}

    /// <summary>
    /// A helpers class that mirrors <see cref="ImColor"/> implementation for C# without
    /// having to deal with points. It also implements some automatic conversions into uint and Vector4.
    /// It also provides a selection of predefined colors.
    /// </summary>
    public struct Color
    {
        public Vector4 Rgba;
        public static Color White = new Color(1f, 1f, 1f, 1f);
        public static Color Black = new Color(0, 0, 0, 1f);
        public static Color TRed = new Color(1f, 0.2f, 0.2f, 1f);
        public static Color TGreen = new Color(0.2f, 0.9f, 0.2f);
        public static Color TBlue = new Color(0.2f, 0.9f, 0.2f, 1);

        /// <summary>
        /// Creates white transparent color
        /// </summary>
        public Color(float alpha)
        {
            Rgba = new Vector4(1, 1, 1, alpha);
        }

        public Color(float r, float g, float b, float a = 1.0f)
        {
            Rgba.X = r;
            Rgba.Y = g;
            Rgba.Z = b;
            Rgba.W = a;
        }


        public Color(int r, int g, int b, int a = 255)
        {
            float sc = 1.0f / 255.0f;
            Rgba.X = (float)r * sc;
            Rgba.Y = (float)g * sc;
            Rgba.Z = (float)b * sc;
            Rgba.W = (float)a * sc;
        }


        public Color(uint uint_)
        {
            Rgba = ImGui.ColorConvertU32ToFloat4(uint_);
        }



        public Color(Vector4 col)
        {
            Rgba = col;
        }


        static Color FromHSV(float h, float s, float v, float a = 1.0f)
        {
            ImGui.ColorConvertHSVtoRGB(h, s, v, out float r, out float g, out float b);
            return new Color(r, g, b, a);
        }


        public static implicit operator uint(Color v)
        {
            return ImGui.ColorConvertFloat4ToU32(v.Rgba);
        }

        public static implicit operator Color(uint ui)
        {
            return new Color(ImGui.ColorConvertU32ToFloat4(ui));
        }


        public static implicit operator Vector4(Color v)
        {
            return v.Rgba;
        }
    };
}
