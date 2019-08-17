using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace T3.Gui
{
    /// <summary>
    /// Defines global style constants for rendering the ui
    /// </summary>
    public static class T3Style
    {
        public static float GridSize = 25;
        public static float Height = GridSize;

        public static bool ApplyOverRides = true;

        public static Vector2 FramePadding = new Vector2(7, 4);
        public static Vector2 ItemSpacing = new Vector2(1, 1);
        public static Vector2 ItemInnerSpacing = new Vector2(3, 2);
        public static float GrabMinSize = 2;
        public static float FrameBorderSize = 0;
        public static float WindowRounding = 3;
        public static float ChildRounding = 1;
        public static float ScrollbarRounding = 3;
        public static float FrameRounding = 1f;

        public static Color ConnectedParameterColor = new Color(0.6f, 0.6f, 1f, 1f);


        public static void Init()
        {
            var style = ImGui.GetStyle();
            _colors = new Vector4[style.Colors.Count];
            for (var i = 0; i < style.Colors.Count; i++)
            {
                _colors[i] = style.Colors[i];
            }

            _colors[(int)ImGuiCol.Text] = new Vector4(1, 1, 1, 0.85f);
            _colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.328f, 0.328f, 0.328f, 1.000f);
            _colors[(int)ImGuiCol.Border] = new Vector4(0, 0.00f, 0.00f, 0.97f);
            _colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            _colors[(int)ImGuiCol.FrameBg] = new Vector4(0.13f, 0.13f, 0.13f, 0.80f);
            _colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.38f, 0.38f, 0.38f, 0.40f);
            _colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.00f, 0.55f, 0.8f, 1.00f);
            _colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.12f, 0.12f, 0.12f, 0.53f);
            _colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 0.33f);
            _colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.00f, 0.00f, 0.00f, 0.25f);
            _colors[(int)ImGuiCol.Button] = new Vector4(0.15f, 0.15f, 0.15f, 1f);
        }


        public static void Apply()
        {



            if (!ApplyOverRides)
                return;

            var style = ImGui.GetStyle();

            style.FramePadding = FramePadding;
            style.ItemSpacing = ItemSpacing;
            style.ItemInnerSpacing = ItemInnerSpacing;
            style.GrabMinSize = GrabMinSize;
            style.FrameBorderSize = FrameBorderSize;
            style.WindowRounding = WindowRounding;
            style.ChildRounding = ChildRounding;
            style.ScrollbarRounding = ScrollbarRounding;
            style.FrameRounding = FrameRounding;

            for (var i = 0; i < style.Colors.Count; i++)
            {
                style.Colors[i] = _colors[i];
            }
        }


        private static Vector4[] _colors;

        public static void Draw()
        {
            ImGui.Button("text");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            ImGui.Checkbox("Apply Override", ref ApplyOverRides);
            ImGui.DragFloat2("FramePadding", ref FramePadding);
            ImGui.DragFloat2("ItemSpacing", ref ItemSpacing);
            ImGui.DragFloat2("ItemInnerSpacing", ref ItemInnerSpacing);

            ImGui.DragFloat("GrabMinSize", ref GrabMinSize);
            ImGui.DragFloat("FrameBorderSize", ref FrameBorderSize);
            ImGui.DragFloat("WindowRounding", ref WindowRounding);
            ImGui.DragFloat("ChildRounding", ref ChildRounding);
            ImGui.DragFloat("ScrollbarRounding", ref ScrollbarRounding);
            ImGui.DragFloat("FrameRounding", ref FrameRounding);
            ImGui.Spacing();

            for (var index = 0; index < _colors.Length; index++)
            {
                var x = (ImGuiCol)index;
                ImGui.ColorEdit4("" + x, ref _colors[index]);
            }
        }
    }
}
