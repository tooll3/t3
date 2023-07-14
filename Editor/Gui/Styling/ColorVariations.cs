using System;

namespace T3.Editor.Gui.Styling;

/// <summary>
/// Provides <see cref="ColorVariation"/>s of <see cref="Color"/>s that are used to 
/// represent different UI-elements (e.g. <see cref="ConnectionLine"/>  and Operators) for a <see cref="Type"/>.
/// </summary>
public static class ColorVariations
{
    public static ColorVariation OperatorBackground = new(brightnessFactor: 0.5f, saturationFactor: 0.7f, opacityFactor: 1);
    public static ColorVariation OperatorLabel = new(brightnessFactor: 1.3f, saturationFactor: 0.4f, opacityFactor: 1);
    public static ColorVariation OperatorBackgroundIdle = new(brightnessFactor: 0.5f, saturationFactor: 1f, opacityFactor: 1);
    public static ColorVariation OperatorBackgroundHover = new(brightnessFactor: 0.75f, saturationFactor: 1, opacityFactor: 1);
    public static ColorVariation OutputNodes = new(brightnessFactor: 0.35f, saturationFactor: 0.7f, opacityFactor: 0.4f);
    public static ColorVariation Highlight = new(brightnessFactor: 1.2f, saturationFactor: 1.2f, opacityFactor: 1);
    public static ColorVariation ConnectionLines = new(brightnessFactor: 0.8f, saturationFactor: 1, opacityFactor: 0.8f);
    public static ColorVariation OperatorInputZone = new(brightnessFactor: 0.15f, saturationFactor: 0.7f, opacityFactor: 0.7f);
    public static ColorVariation OperatorOutline = new(brightnessFactor: 0.1f, saturationFactor: 0.7f, opacityFactor: 0.5f);

    // public static void DrawSettingsUi()
    // {
    //     if (ImGui.TreeNode("Type colors"))
    //     {
    //         foreach (var pair in TypeUiRegistry.Entries)
    //         {
    //             var color = pair.Value.Color;
    //             if (ImGui.ColorEdit4(pair.Key.Name, ref color.Rgba))
    //             {
    //                 //pair.Value.Color = color;
    //             }
    //         }
    //         ImGui.TreePop();
    //     }
    //
    //     if (ImGui.TreeNode("Color Variations"))
    //     {
    //         ImGui.ColorEdit4("Sample Color", ref _sampleColor.Rgba, ImGuiColorEditFlags.DisplayHSV | ImGuiColorEditFlags.NoInputs);
    //
    //         ImGui.Text("SatFactor  /  Brightness Factor / Alpha Factor");
    //         foreach (var cv in _colorVariations)
    //         {
    //             ImGui.PushID(cv.GetHashCode());
    //             cv.DrawEditUi();
    //             ImGui.PopID();
    //         }
    //         ImGui.TreePop();
    //     }
    // }

    // private static Color _sampleColor = Color.Green;
    // private static readonly List<Variation> _colorVariations = new()
    //                                                                {
    //                                                                    ConnectionLines,
    //                                                                    Operator,
    //                                                                    OperatorHover,
    //                                                                    OutputNodes,
    //                                                                    Highlight,
    //                                                                    Muted,
    //                                                                    OperatorInputZone,
    //                                                                    OperatorLabel,
    //                                                                };
}