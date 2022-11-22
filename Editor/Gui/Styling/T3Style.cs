using System.Numerics;
using ImGuiNET;

namespace T3.Editor.Gui.Styling
{
    /// <summary>
    /// Defines global style constants and ImGUI overrides
    /// </summary>
    /// <remarks>
    /// Sadly we can't use the original ImGUI-Style struct because the .net wrapper only defines get-accessors.
    /// </remarks>
    public static class T3Style
    {
        public static class Colors
        {
            public static readonly Color ConnectedParameter = new Color(0.6f, 0.6f, 1f, 1f);
            public static readonly Color ValueLabel = new Color(1, 1, 1, 0.5f);
            public static readonly Color ValueLabelHover = new Color(1, 1, 1, 1.2f);
            public static readonly Color GraphLine = new Color(1, 1, 1, 0.3f);
            public static readonly Color GraphLineHover = new Color(1, 1, 1, 0.7f);
            public static readonly Color GraphAxis = new Color(0, 0, 0, 0.3f);

            public static readonly Color Button = Color.FromString("#CC282828");
            public static readonly Color ButtonHover = new Color(43, 65, 80, 255);
            
            public static readonly Color ButtonActive = Color.FromString("#4592FF");
            public static readonly Color DarkGray = Color.FromString("#131313");
            
            public static readonly Color WidgetSlider = new Color(0.15f);
            public static readonly Color TextWidgetTitle = new Color(0.65f);
            public static readonly Color TextMuted = new Color(0.5f);
            public static readonly Color TextDisabled = new Color(0.328f, 0.328f, 0.328f, 1.000f);
            public static readonly Color Warning = new Color(203, 19,113, 255);
            
            public static readonly Color WindowBackground = new Color(0.05f, 0.05f,0.05f, 1);
            public static readonly Color Background = new Color(0.1f, 0.1f, 0.1f, 0.98f);
            
            public static readonly Color GraphActiveLine = Color.Orange;
        }

        public const float ToolBarHeight = 25;

        public static void Apply()
        {
            var style = ImGui.GetStyle();
            style.Colors[(int)ImGuiCol.Text] = new Vector4(1, 1, 1, 0.85f);
            style.Colors[(int)ImGuiCol.TextDisabled] = Colors.TextDisabled;
            style.Colors[(int)ImGuiCol.Button] = Colors.Button;
            style.Colors[(int)ImGuiCol.ButtonHovered] = Colors.ButtonHover;
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0, 0.00f, 0.00f, 0.97f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.13f, 0.13f, 0.13f, 0.80f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.38f, 0.38f, 0.38f, 0.40f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.00f, 0.55f, 0.8f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.12f, 0.12f, 0.12f, 0.53f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 0.33f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.00f, 0.00f, 0.00f, 0.25f);
            style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1f,0.1f,0.1f, 0.98f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.1f,0.1f,0.1f, 0.1f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.0f,0.0f,0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.0f,0.0f,0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = Color.FromString("#FF00B2FF");
            style.Colors[(int)ImGuiCol.TabUnfocused] = Color.FromString("#FF1C1C1C");
            style.Colors[(int)ImGuiCol.TabActive] = Color.FromString("#FF505050");
            style.Colors[(int)ImGuiCol.Tab] = Color.FromString("#FF202020");
            style.Colors[(int)ImGuiCol.TabUnfocused] = Color.FromString("#FF151515");
            style.Colors[(int)ImGuiCol.TabUnfocusedActive] = Color.FromString("#FF202020");
            style.Colors[(int)ImGuiCol.TitleBgActive] = Color.FromString("#FF000000");
            style.Colors[(int)ImGuiCol.TitleBg] = Color.FromString("#FF000000");
                
            style.WindowPadding = Vector2.Zero;
            style.FramePadding = new Vector2(7, 4);
            style.ItemSpacing = new Vector2(1, 1);
            style.ItemInnerSpacing = new Vector2(3, 2);
            style.GrabMinSize = 2;
            style.FrameBorderSize = 0;
            style.WindowRounding = 0;
            style.ChildRounding = 0;
            style.ScrollbarRounding = 2;
            style.FrameRounding = 0f;
            style.DisplayWindowPadding = Vector2.Zero;
            style.DisplaySafeAreaPadding = Vector2.Zero;
            style.ChildBorderSize = 1;
            style.TabRounding = 2;
        }
    }
}
