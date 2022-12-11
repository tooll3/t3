using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Editor.Gui.InputUi;

namespace T3.Editor.Gui.Styling
{
    /// <summary>
    /// Provides <see cref="Variation"/>s of <see cref="Color"/>s that are used to 
    /// represent different UI-elements (e.g. <see cref="ConnectionLine"/>  and Operators) for a <see cref="Type"/>.
    /// </summary>
    public static class ColorVariations
    {
        public static readonly Variation Highlight = new Variation("Highlight", 1.2f, 1.2f, 1);
        public static readonly Variation Muted = new Variation("Muted", 0.7f, 0.50f, 0.6f);
        public static readonly Variation ConnectionLines = new Variation("Connection Lines", 1, 0.8f, 0.8f);
        public static readonly Variation Operator = new Variation("Operator", 0.7f, 0.5f, 1);
        public static readonly Variation OperatorIdle = new Variation("Operator", 1f, 0.5f, 1);
        public static readonly Variation OperatorHover = new Variation("Operator Hover", 1, 0.75f, 1);
        
        public static readonly Variation OutputNodes = new Variation("Output Nodes", 0.7f, 0.35f, 0.4f);
        
        public static readonly Variation OperatorInputZone = new Variation("Operator Input Zone", 0.7f, 0.15f, 0.7f);
        public static readonly Variation OperatorLabel = new Variation("Operator Label", 0.4f, 1.3f, 1);

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
            public Color Apply(Color originalColor)
            {
                ImGui.ColorConvertRGBtoHSV(
                    originalColor.Rgba.X,
                    originalColor.Rgba.Y,
                    originalColor.Rgba.Z,
                    out var h, out var s, out var v);

                return Color.FromHSV(
                    h,
                    s * _saturation,
                    v * _brightness,
                    originalColor.Rgba.W * _opacity);
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
                ImGui.ColorButton("x", Apply(_sampleColor).Rgba);
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

        private static Color _sampleColor = Color.Green;
        private static List<Variation> _colorVariations = new List<Variation>()
        {
            ConnectionLines,
            Operator,
            OperatorHover,
            OutputNodes,
            Highlight,
            Muted,
            OperatorInputZone,
            OperatorLabel,
        };
    }
}
