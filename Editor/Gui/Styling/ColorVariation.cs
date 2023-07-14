using ImGuiNET;
// ReSharper disable MemberCanBePrivate.Global

namespace T3.Editor.Gui.Styling;

/// <summary>
/// Defines how a color hue should be modified (e.g. muted for disabled fields)
/// </summary>
public class ColorVariation
{
    public float Brightness = 1;
    public float Saturation = 1;
    public float Opacity = 1;

    public ColorVariation()
    {
    }
    
    internal ColorVariation(float brightnessFactor = 1, float saturationFactor = 1, float opacityFactor = 1)
    {
        Saturation = saturationFactor;
        Brightness = brightnessFactor;
        Opacity = opacityFactor;
    }
    
    public Color Apply(Color originalColor)
    {
        ImGui.ColorConvertRGBtoHSV(
                                   originalColor.Rgba.X,
                                   originalColor.Rgba.Y,
                                   originalColor.Rgba.Z,
                                   out var h, out var s, out var v);

        return Color.FromHSV(
                             h,
                             s * Saturation,
                             v * Brightness,
                             originalColor.Rgba.W * Opacity);
    }

    public ColorVariation Clone()
    {
        return new ColorVariation()
                   {
                       Saturation = Saturation,
                       Brightness = Brightness,
                       Opacity = Opacity,
                   };
    }
}