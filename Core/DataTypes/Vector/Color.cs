using System;
using T3.Core.Utils;
// ReSharper disable UnusedMember.Global

namespace T3.Core.DataTypes.Vector;

// todo - make this readonly?
[Serializable]
public struct Color
{
    public float R { readonly get => Rgba.X; set => Rgba.X = value; }
    public float G { readonly get => Rgba.Y; set => Rgba.Y = value; }
    public float B { readonly get => Rgba.Z; set => Rgba.Z = value; }
    public float A { readonly get => Rgba.W; set => Rgba.W = value; }

    public Vector4 Rgba;

    #region Constructors
    /// <summary>
    /// Creates white transparent color
    /// </summary>
    public Color(float alpha)
    {
        Rgba = new Vector4(1, 1, 1, alpha);
    }

    public Color(float r, float g, float b, float a = 1) => Rgba = new Vector4(r, g, b, a);

    public Color(Vector4 rgba) => Rgba = rgba;

    public Color(Vector3 value) => Rgba = new Vector4(value, 1);

    public Color(int r, int g, int b, int a = 255)
    {
        var mult = 1f / 255f;
        Rgba = new Vector4(r * mult, g * mult, b * mult, a * mult);
    }

    public Color(byte r, byte g, byte b, byte a = 255)
    {
        var mult = 1f / 255f;
        Rgba = new Vector4(r * mult, g * mult, b * mult, a * mult);
    }

    public Color(Byte4 value) : this(value.X, value.Y, value.Z, value.W)
    {
    }

    public Color(uint color) : this(new Byte4(color))
    {
    }

    public Color(int value) : this(new Byte4(value))
    {
    }
    
    #endregion Constructors

    public readonly Byte4 ToByte4() => new((byte)(R * 255f), (byte)(G * 255f), (byte)(B * 255f), (byte)(A * 255f));

    public readonly int AsInt() => ToByte4().Int;
    public readonly uint AsUInt() => ToByte4().UInt;
    public int AsIntBgra() => Bgra.ToByte4().Int;
    public uint AsUIntBgra() => Bgra.ToByte4().UInt;

    public static explicit operator Byte4(Color color) => color.ToByte4();
    public static implicit operator Vector4(Color color) => color.Rgba;
    public static implicit operator Color(Vector4 vector) => new(vector);
    public static implicit operator uint(Color value) => value.AsUInt();

    public static Color operator *(Color color, float factor) => new(color.Rgba * factor);
    public static Color operator *(float factor, Color color) => new(color.Rgba * factor);
    public static Color operator *(Color color1, Color color2) => new(color1.Rgba * color2.Rgba);

    public static Color operator +(Color color1, Color color2) => new(color1.Rgba + color2.Rgba);
    public static Color operator -(Color color1, Color color2) => new(color1.Rgba - color2.Rgba);
    public static Color operator -(Color color1, Vector4 color2) => new(color1.Rgba - color2);
    public static Color operator -(Vector4 color1, Color color2) => new(color1 - color2.Rgba);

    public static Color operator /(Color color, float divisor) => new(color.Rgba / divisor);
    public static Color operator /(Color color1, Color color2) => new(color1.Rgba / color2.Rgba);

    public static Color operator *(Color color, Vector4 vector) => new(color.Rgba * vector);
    public static Vector4 operator *(Vector4 vector, Color color) => color.Rgba * vector;

    public float Hue
    {
        get
        {
            GetHSV(out var h, out _, out _);
            return h;
        }
        set
        {
            GetHSV(out _, out var s, out var v);
            Rgba = FromHSV(value, s, v, A).Rgba;
        }
    }

    public float Saturation
    {
        get
        {
            GetHSV(out _, out var s, out _);
            return s;
        }
        set
        {
            GetHSV(out var h, out _, out var v);
            Rgba = FromHSV(h, value, v, A).Rgba;
        }
    }

    public float V
    {
        get
        {
            GetHSV(out _, out _, out var v);
            return v;
        }
        set
        {
            GetHSV(out var h, out var s, out _);
            Rgba = FromHSV(h, s, value, A).Rgba;
        }
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
        var labMix = MathUtils.Lerp(c1.Lab, c2.Lab, t);
        return new Color(OkLab.OkLabToRgba(labMix));
    }

    public Vector4 Lab { get => OkLab.RgbAToOkLab(Rgba); set => Rgba = OkLab.OkLabToRgba(value); }

    /// <remark>
    /// Avoid using these colors because they don't support theming.
    /// </remark>
    public static readonly Color Transparent = new(1f, 1f, 1f, 0f);

    public static readonly Color TransparentBlack = new(0f, 0f, 0f, 0f);
    public static readonly Color White = new(1f, 1f, 1f);
    public static readonly Color Black = new(0, 0, 0, 1f);
    public static readonly Color Red = new(1f, 0.2f, 0.2f);
    public static readonly Color Green = new(0.2f, 0.9f, 0.2f);
    public static readonly Color Blue = new(0.4f, 0.5f, 1f);
    public static readonly Color Orange = new(1, 0.5f, 0);
    public static readonly Color Yellow = new(1, 1, 0);
    public static readonly Color Cyan = new(0, 1, 1);
    public static readonly Color Magenta = new(1, 0, 1);
    public static readonly Color Gray = new(0.5f, 0.5f, 0.5f);
    public static readonly Color LightGray = new(0.75f, 0.75f, 0.75f);
    public static readonly Color DarkGray = new(0.25f, 0.25f, 0.25f);
    public static readonly Color Pink = new(1, 0.5f, 0.5f);
    public static readonly Color Brown = new(0.5f, 0.25f, 0);
    public static readonly Color Purple = new(0.5f, 0, 0.5f);
    public static readonly Color Olive = new(0.5f, 0.5f, 0);
    public static readonly Color Teal = new(0, 0.5f, 0.5f);
    public static readonly Color Navy = new(0, 0, 0.5f);

    public static Color FromString(string hex)
    {
        System.Drawing.Color systemColor = System.Drawing.ColorTranslator.FromHtml(hex);
        return new Color(systemColor.R, systemColor.G, systemColor.B, systemColor.A);
    }

    public string ToHTML()
    {
        var drawingColor = System.Drawing.Color.FromArgb((int)(A * 255).Clamp(0, 255),
                                                         (int)(R * 255).Clamp(0, 255),
                                                         (int)(G * 255).Clamp(0, 255),
                                                         (int)(B * 255).Clamp(0, 255));
        return System.Drawing.ColorTranslator.ToHtml(drawingColor);
    }

    public Color Inverted(bool preserveAlpha)
    {
        var inverted = Vector4.Abs(Vector4.One - Rgba);
        if (preserveAlpha)
            inverted.W = A;
        return new Color(inverted);
    }

    public Color Bgra => new(B, G, R, A);

    public Color PremultipliedAlpha
    {
        get
        {
            var premultiplied = Rgba;
            premultiplied *= A;
            premultiplied.W = A;
            return new Color(premultiplied);
        }
    }

    public Color UnmultipliedAlpha
    {
        get
        {
            var division = (Rgba / A).NanToZero();
            var unmultiplied = Vector4.Min(division, Vector4.One);
            unmultiplied.W = Rgba.W; // keep alpha
            return new Color(unmultiplied);
        }
    }

    public static Color Blend(Color firstColor, Color secondColor, BlendMode blendMode, bool premultiplyAlpha = false)
    {
        Color resultColor;

        if (premultiplyAlpha)
        {
            firstColor = firstColor.PremultipliedAlpha;
            secondColor = secondColor.PremultipliedAlpha;
        }

        switch (blendMode)
        {
            case BlendMode.Normal:
                resultColor = secondColor;
                break;

            case BlendMode.Add:
                resultColor = firstColor + secondColor;
                break;

            case BlendMode.Subtract:
                resultColor = firstColor - secondColor;
                break;

            case BlendMode.Multiply:
                resultColor = firstColor * secondColor;
                break;

            case BlendMode.Screen:
                resultColor = Vector4.One - firstColor.Inverted(false) * secondColor.Inverted(false);
                break;

            case BlendMode.Overlay:
            {
                var darkened = 2 * firstColor * secondColor;
                var lightened = new Color(Vector4.One) - 2 * firstColor.Inverted(false) * secondColor.Inverted(false);

                resultColor = new Color(r: firstColor.R < 0.5f ? darkened.R : lightened.R,
                                        g: firstColor.G < 0.5f ? darkened.G : lightened.G,
                                        b: firstColor.B < 0.5f ? darkened.B : lightened.B);
                break;
            }
            case BlendMode.Darken:
                resultColor = new Color(Vector4.Min(firstColor.Rgba, secondColor.Rgba));
                break;

            case BlendMode.Lighten:
                resultColor = new Color(Vector4.Max(firstColor.Rgba, secondColor.Rgba));
                break;

            case BlendMode.ColorDodge:
            {
                // nans are created by dividing zero by zero
                var division = (secondColor.Rgba / firstColor.Inverted(false).Rgba).NanToZero();
                var divisionClamped = Vector4.Min(division, Vector4.One);
                resultColor = new Color(divisionClamped);
                break;
            }

            case BlendMode.ColorBurn:
            {
                // nans are created by dividing zero by zero
                var division = (secondColor.Inverted(false).Rgba / firstColor.Rgba).NanToZero();
                var divisionClamped = Vector4.Min(division, Vector4.One);
                resultColor = new Color(Vector4.One - divisionClamped);
                break;
            }

            case BlendMode.HardLight:
            {
                var invertedMultiplied = new Color(Vector4.One) - firstColor.Inverted(false) * secondColor.Inverted(false) * 2f;
                var normalMultiplied = firstColor * secondColor * 2f;

                resultColor = new Color(r: secondColor.R < 0.5f ? normalMultiplied.R : invertedMultiplied.R,
                                        g: secondColor.G < 0.5f ? normalMultiplied.G : invertedMultiplied.G,
                                        b: secondColor.B < 0.5f ? normalMultiplied.B : invertedMultiplied.B,
                                        a: secondColor.A < 0.5f ? normalMultiplied.A : invertedMultiplied.A);
                break;
            }

            case BlendMode.SoftLight:
            {
                var doubleDiff1 = (Color)Vector4.Abs(secondColor.Rgba * 2f - Vector4.One);

                var sqEvaluation = firstColor * secondColor * 2f + firstColor * firstColor * doubleDiff1;
                var sqrtEvaluation = (Color)Vector4.SquareRoot(firstColor.Rgba) * doubleDiff1 + firstColor * 2f * secondColor.Inverted(false);

                resultColor = new
                    Color(r: secondColor.R < 0.5f ? sqEvaluation.R : sqrtEvaluation.R,
                          g: secondColor.G < 0.5f ? sqEvaluation.G : sqrtEvaluation.G,
                          b: secondColor.B < 0.5f ? sqEvaluation.B : sqrtEvaluation.B,
                          a: secondColor.A < 0.5f ? sqEvaluation.A : sqrtEvaluation.A);
                break;
            }

            case BlendMode.Difference:
                resultColor = new(Vector4.Abs(firstColor.Rgba - secondColor.Rgba));
                break;

            case BlendMode.Exclusion:
                resultColor = firstColor + secondColor - 2 * firstColor * secondColor;
                break;

            default:
                throw new ArgumentOutOfRangeException(paramName: nameof(blendMode), actualValue: blendMode, message: "Unknown blend mode");
        }

        if (premultiplyAlpha)
        {
            resultColor = resultColor.UnmultipliedAlpha;
        }

        // clamp to valid color range
        resultColor = new Color(Vector4.Clamp(resultColor.Rgba, Vector4.Zero, Vector4.One));
        return resultColor;
    }

    public static Color FromHSV(float h, float s, float v, float a = 1.0f)
    {
        if (s == 0.0f)
        {
            return new Color(v,v,v,a); // gray
        }        
        
        float r = 0, g = 0, b = 0;

        h %= 1f;

        int i = (int)(h * 6);
        var f = h * 6 - i;
        var p = v * Math.Max((1 - s), 0);
        var q = v * Math.Max((1 - f * s), 0);
        var t = v * Math.Max((1 - (1 - f) * s), 0);

        switch (i % 6)
        {
            case 0:
                r = v;
                g = t;
                b = p;
                break;
            case 1:
                r = q;
                g = v;
                b = p;
                break;
            case 2:
                r = p;
                g = v;
                b = t;
                break;
            case 3:
                r = p;
                g = q;
                b = v;
                break;
            case 4:
                r = t;
                g = p;
                b = v;
                break;
            case 5:
                r = v;
                g = p;
                b = q;
                break;
        }

        return new Color(r, g, b, a);
    }

    /// <summary>
    /// Returns HSV from 0-1
    /// </summary>
    public void GetHSV(out float h, out float s, out float v)
    {
        // Should this be normalized? what if the colors are out of bounds?
        float r = R, g = G, b = B;
        
        float min = Math.Min(Math.Min(r, g), b);
        float max = Math.Max(Math.Max(r, g), b);
        float delta = max - min;

        float hueShift;
        const float threshold = 0.00001f;

        if (max - r <= threshold)
        {
            hueShift = (g - b) / delta;
        }
        else if (max - g <= threshold)
        {
            hueShift = 2f + (b - r) / delta;
        }
        else // max == B
        {
            hueShift = 4f + (r - g) / delta;
        }

        hueShift = hueShift.NanToZero();

        // hue
        h = (60 * hueShift + 360) % 360 / 360f;

        // Saturation
        s = (delta / max).NanToZero();

        // Value
        v = max;
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
                // todo: safer floating point comparison?
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

    public readonly Color Fade(float f) => new(Rgba.X, Rgba.Y, Rgba.Z, (Rgba.W * f).Clamp(0,1));

    public override string ToString()
    {
        return $"[{Rgba.X:0.00}, {Rgba.Y:0.00}, {Rgba.Z:0.00}, {Rgba.W:0.00}]";
    }
}

public enum BlendMode
{
    Normal,
    Add,
    Subtract,
    Multiply,
    Screen,
    Overlay,
    Darken,
    Lighten,
    ColorDodge,
    ColorBurn,
    HardLight,
    SoftLight,
    Difference,
    Exclusion
}