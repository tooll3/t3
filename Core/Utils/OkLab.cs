using System;
using System.Numerics;

namespace T3.Core.Utils;

/**
 * Helper methods to deal with OkLab color space. This can be useful for nicer blending of gradients
 */
public static class OkLab
{
    // From Linear to oklab
    public static Vector4 RgbAToOkLab(Vector4 c)
    {
        var l = 0.4122214708f * c.X + 0.5363325363f * c.Y + 0.0514459929f * c.Z;
        var m = 0.2119034982f * c.X + 0.6806995451f * c.Y + 0.1073969566f * c.Z;
        var s = 0.0883024619f * c.X + 0.2817188376f * c.Y + 0.6299787005f * c.Z;

        var l1 = MathF.Cbrt(l);
        var m1 = MathF.Cbrt(m);
        var s1 = MathF.Cbrt(s);

        return new Vector4(
                           0.2104542553f * l1 + 0.7936177850f * m1 - 0.0040720468f * s1,
                           1.9779984951f * l1 - 2.4285922050f * m1 + 0.4505937099f * s1,
                           0.0259040371f * l1 + 0.7827717662f * m1 - 0.8086757660f * s1,
                           c.W
                          );
    }

    // From OKLab to Linear sRGB
    public static Vector4 OkLabToRgba(Vector4 c)
    {
        var l1 = c.X + 0.3963377774f * c.Y + 0.2158037573f * c.Z;
        var m1 = c.X - 0.1055613458f * c.Y - 0.0638541728f * c.Z;
        var s1 = c.X - 0.0894841775f * c.Y - 1.2914855480f * c.Z;

        var l = l1 * l1 * l1;
        var m = m1 * m1 * m1;
        var s = s1 * s1 * s1;

        return new Vector4(
                           +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
                           -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
                           -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s,
                           c.W
                          );
    }

    public static Vector4 Degamma(Vector4 c)
    {
        const float gamma = 2.2f;
        return new Vector4(
                           MathF.Pow(c.X, gamma),
                           MathF.Pow(c.Y, gamma),
                           MathF.Pow(c.Z, gamma),
                           c.W
                          );
    }
        
    public static Vector4 ToGamma(Vector4 c)
    {
        const float gamma = 2.2f;
        return new Vector4(
                           MathF.Pow(c.X, 1.0f/gamma),
                           MathF.Pow(c.Y, 1.0f/gamma),
                           MathF.Pow(c.Z, 1.0f/gamma),
                           c.W
                          );
    }        
        
    public static Vector4 Mix(Vector4 c1, Vector4 c2, float t)
    {
        var c1Linear = Degamma(c1);
        var c2Linear = Degamma(c2);
                
        var labMix= MathUtils.Lerp( RgbAToOkLab(c1Linear), RgbAToOkLab(c2Linear), t);
        return ToGamma(OkLab.OkLabToRgba(labMix));
    }
}