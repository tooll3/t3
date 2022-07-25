using System;
using Core.Resource;
using ImGuiNET;
using T3.Core;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

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
        
        public static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
        public static readonly Color TransparentBlack = new Color(0f, 0f, 0f, 0f);
        public static readonly  Color White = new Color(1f, 1f, 1f, 1f);
        public static readonly Color Gray = new Color(0.6f, 0.6f, 0.6f, 1);
        public static readonly Color DarkGray = new Color(0.3f, 0.3f, 0.3f, 1);
        public static readonly Color Black = new Color(0, 0, 0, 1f);
        public static readonly Color Red = new Color(1f, 0.2f, 0.2f, 1f);
        public static readonly Color Green = new Color(0.2f, 0.9f, 0.2f, 1f);
        public static readonly Color Blue = new Color(0.4f, 0.5f, 1f, 1);
        public static readonly Color Orange = new Color(1f, 0.46f, 0f, 1f);

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
            Rgba.X = r * sc;
            Rgba.Y = g * sc;
            Rgba.Z = b * sc;
            Rgba.W = a * sc;
        }

        public Color(uint @uint)
        {
            Rgba = ImGui.ColorConvertU32ToFloat4(@uint);
        }

        public Color(Vector4 color)
        {
            Rgba = color;
        }

        public override string ToString()
        {
            return Rgba.ToString();
        }

        static public Color FromHSV(float h, float s, float v, float a = 1.0f)
        {
            ImGui.ColorConvertHSVtoRGB(h, s, v, out float r, out float g, out float b);
            return new Color(r, g, b, a);
        }

        static public Color FromString(string hex)
        {
            System.Drawing.Color systemColor = System.Drawing.ColorTranslator.FromHtml(hex);
            return new Color(systemColor.R, systemColor.G, systemColor.B, systemColor.A);
        }
        
        public string ToHTML()
        {
            var drawingColor = System.Drawing.Color.FromArgb((int)(A*255).Clamp(0,255), 
                                                             (int)(R*255).Clamp(0,255), 
                                                             (int)(G*255).Clamp(0,255), 
                                                             (int)(B*255).Clamp(0,255));
            return System.Drawing.ColorTranslator.ToHtml(drawingColor);
        }

        public static implicit operator uint(Color color)
        {
            return ImGui.ColorConvertFloat4ToU32(color.Rgba);
        }
        
        public Vector4 Lab 
            {
                get => OkLab.RgbAToOkLab(Rgba);
                set => Rgba = OkLab.OkLabToRgba(value);
            }
        
        public static implicit operator Color(uint @uint)
        {
            return new Color(ImGui.ColorConvertU32ToFloat4(@uint));
        }

        public static implicit operator Vector4(Color color)
        {
            return color.Rgba;
        }

        public static Color operator *(Color c, float f)
        {
            c.Rgba.W *= f;
            return c;
        }

        public static Color Mix(Color c1, Color c2, float t)
        {
            return new Color(
                             c1.Rgba.X + (c2.Rgba.X - c1.Rgba.X) * t,
                             c1.Rgba.Y + (c2.Rgba.Y - c1.Rgba.Y) * t,
                             c1.Rgba.Z + (c2.Rgba.Z - c1.Rgba.Z) * t,
                             c1.Rgba.W + (c2.Rgba.W - c1.Rgba.W) * t
                            );
        }

        public static Color MixOkLab(Color c1, Color c2, float t)
        {
            var labMix= MathUtils.Lerp(c1.Lab, c2.Lab, t);
            return new Color(OkLab.OkLabToRgba(labMix));
        }
        
        public static Color GetStyleColor(ImGuiCol color)
        {
            unsafe
            {
                var c = ImGui.GetStyleColorVec4(color);
                return new Color(c->X, c->Y, c->Z, c->W);
            }
        }

        /// <summary>
        /// This is a variation of the normal HSV function in that it returns a desaturated "white" colors brightness above 0.5   
        /// </summary>
        public static Color ColorFromHsl(float h, float s, float l, float a = 1)
        {
            float r, g, b, m, c, x;

            h /= 60;
            if (h < 0) h = 6 - (-h % 6);
            h %= 6;

            s = Math.Max(0, Math.Min(1, s));
            l = Math.Max(0, Math.Min(1, l));

            c = (1 - Math.Abs((2 * l) - 1)) * s;
            x = c * (1 - Math.Abs((h % 2) - 1));

            if (h < 1)
            {
                r = c;
                g = x;
                b = 0;
            }
            else if (h < 2)
            {
                r = x;
                g = c;
                b = 0;
            }
            else if (h < 3)
            {
                r = 0;
                g = c;
                b = x;
            }
            else if (h < 4)
            {
                r = 0;
                g = x;
                b = c;
            }
            else if (h < 5)
            {
                r = x;
                g = 0;
                b = c;
            }
            else
            {
                r = c;
                g = 0;
                b = x;
            }

            m = l - c / 2;

            return new Color(r + m, g + m, b + m, a);
        }

        public Vector3 AsHsl
        {
            get
            {
                float r = Rgba.X;
                float g = Rgba.Y;
                float b = Rgba.Z;

                float tmp = (r < g) ? r : g;
                float min = (tmp < b) ? tmp : b;

                tmp = (r > g) ? r : g;
                float max = (tmp > b) ? tmp : b;

                float delta = max - min;
                float lum = (min + max) / 2.0f;
                float sat = 0;
                if (lum > 0.0f && lum < 1.0f)
                {
                    sat = delta / ((lum < 0.5f) ? (2.0f * lum) : (2.0f - 2.0f * lum));
                }

                float hue = 0.0f;
                if (delta > 0.0f)
                {
                    if (max == r && max != g)
                        hue += (g - b) / delta;
                    if (max == g && max != b)
                        hue += (2.0f + (b - r) / delta);
                    if (max == b && max != r)
                        hue += (4.0f + (r - g) / delta);
                    hue *= 60.0f;
                }

                return new Vector3(hue, sat, lum);
            }
        }

        public readonly Color Fade(float f)
        {
            return new Color(Rgba.X, Rgba.Y, Rgba.Z, Rgba.W * f);
        }

        public float R { get => Rgba.X; set => Rgba.X = value; }
        public float G { get => Rgba.Y; set => Rgba.Y = value; }
        public float B { get => Rgba.Z; set => Rgba.Z = value; }
        public float A { get => Rgba.W; set => Rgba.W = value; }

        /**
         * Normalized from [0..1]
         */
        public float Hue
        { 
            get
            {
                ImGui.ColorConvertRGBtoHSV(R,G,B, out float h, out var s, out var v );
                return h;
            } 
            set
            {
                ImGui.ColorConvertRGBtoHSV(R,G,B, out float h, out var s, out var v );
                h = value;
                ImGui.ColorConvertHSVtoRGB(h, s, v, out var r, out var g, out var b);
                R = r;
                G = g;
                B = b;
            }
        }

        
        public float Saturation
        { 
            get
            {
                ImGui.ColorConvertRGBtoHSV(R,G,B, out float h, out var s, out var v );
                return s;
            } 
            set
            {
                ImGui.ColorConvertRGBtoHSV(R,G,B, out float h, out var s, out var v );
                s = value;
                ImGui.ColorConvertHSVtoRGB(h, s, v, out var r, out var g, out var b);
                R = r;
                G = g;
                B = b;
            }
        }
        
        
        public float V
        { 
            get
            {
                ImGui.ColorConvertRGBtoHSV(R,G,B, out float h, out var s, out var v );
                return v;
            } 
            set
            {
                ImGui.ColorConvertRGBtoHSV(R,G,B, out float h, out var s, out var v );
                v = value;
                ImGui.ColorConvertHSVtoRGB(h, s, v, out var r, out var g, out var b);
                R = r;
                G = g;
                B = b;
            }
        }
        
        
        public SharpDX.Color AsSharpDx => new SharpDX.Color(Rgba.X, Rgba.Y, Rgba.Z, Rgba.Z);
    }
}