using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace T3.Gui.TypeColors
{
    /// <summary>
    /// Provides <see cref="Variation"/>s of <see cref="Color"/>s that are used to 
    /// represent different UI-elements (e.g. <see cref="ConnectionLine"/>  and Operators) for a <see cref="Type"/>.
    /// </summary>
    public class ColorVariations
    {
        public static readonly Variation ConnectionLines = new Variation("Connection Lines");
        public static readonly Variation OperatorBackground = new Variation("Operators");
        public static readonly Variation OperatorBorders = new Variation("Operator Borders");
        public static readonly Variation OperatorLabel = new Variation("Operator Label");

        public static void DrawSettingsUi()
        {
            if (ImGui.TreeNode("Type colors"))
            {
                foreach (var pair in TypeUiRegistry.Entries)
                {
                    var color = pair.Value.Color;
                    if (ImGui.ColorEdit4(pair.Key.Name, ref color.Rgba))
                    {
                        pair.Value.Color = color;
                    }
                }
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Color Variations"))
            {
                ImGui.ColorEdit4("Sample Color", ref _sampleColor.Rgba, ImGuiColorEditFlags.DisplayHSV | ImGuiColorEditFlags.NoInputs);

                foreach (var cv in _colorVariations)
                {
                    ImGui.PushID(cv.GetHashCode());
                    cv.DrawEditUi();
                    ImGui.PopID();
                }
                ImGui.TreePop();
            }
        }

        /// <summary>
        /// Defines how a color hue should be modified (e.g. muted for disabled fields)
        /// </summary>
        public class Variation
        {
            public Color GetVariation(Color originalColor)
            {
                ImGui.ColorConvertRGBtoHSV(
                    originalColor.Rgba.X,
                    originalColor.Rgba.Y,
                    originalColor.Rgba.Z,
                    out var h, out var s, out var v);

                return Color.FromHSV(h, s * _saturation, v * _brightness, originalColor.Rgba.Z * _opacity);
            }

            internal Variation(string label, float saturationFactor = 1, float brightnessFactor = 1, float opacityFactor = 1)
            {
                _label = label;
                _saturation = saturationFactor;
                _brightness = brightnessFactor;
                _opacity = opacityFactor;
            }

            internal void DrawEditUi()
            {
                ImGui.ColorButton("x", GetVariation(_sampleColor).Rgba);
                ImGui.SameLine();

                var v = new Vector3(_saturation, _brightness, _opacity);
                if (ImGui.DragFloat3(_label, ref v, 0.01f))
                {
                    _saturation = v.X;
                    _brightness = v.Y;
                    _opacity = v.Z;
                }
            }

            private string _label;
            private float _saturation = 1;
            private float _brightness = 1;
            private float _opacity = 1;
        }

        private static Color _sampleColor = Color.TGreen;
        private static List<Variation> _colorVariations = new List<Variation>()
        {
            ConnectionLines,
            OperatorBackground,
            OperatorBorders,
            OperatorLabel,
        };
    }
}
