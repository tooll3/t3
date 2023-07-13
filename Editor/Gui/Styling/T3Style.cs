using System.Numerics;
using ImGuiNET;

namespace T3.Editor.Gui.Styling;

/// <summary>
/// Defines global style constants and ImGUI overrides
/// </summary>
/// <remarks>
/// Sadly we can't use the original ImGUI-Style struct because the .net wrapper only defines get-accessors.
/// </remarks>
public static class T3Style
{
    public class HintAttribute : System.Attribute
    {
        public string Description;
    }

    public const float ToolBarHeight = 25;

    public static void Apply()
    {
        var style = ImGui.GetStyle();
        style.Colors[(int)ImGuiCol.Text] = UiColors.Text;
        style.Colors[(int)ImGuiCol.TextDisabled] = UiColors.TextDisabled;
        style.Colors[(int)ImGuiCol.Button] = UiColors.BackgroundButton;
        style.Colors[(int)ImGuiCol.ButtonHovered] = UiColors.BackgroundHover;
        style.Colors[(int)ImGuiCol.Border] = UiColors.BackgroundFull.Fade(0.9f);
        style.Colors[(int)ImGuiCol.BorderShadow] = UiColors.BackgroundFull;
        style.Colors[(int)ImGuiCol.FrameBg] = UiColors.BackgroundInputField;//new Vector4(0.13f, 0.13f, 0.13f, 0.80f);
        
        style.Colors[(int)ImGuiCol.FrameBgHovered] = UiColors.BackgroundInputFieldHover;
        style.Colors[(int)ImGuiCol.FrameBgActive] = UiColors.BackgroundInputFieldActive;
        
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.12f, 0.12f, 0.12f, 0.53f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 0.33f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = UiColors.WindowResizeHandle;
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1f,0.1f,0.1f, 0.98f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.1f,0.1f,0.1f, 0.1f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = UiColors.BackgroundFull;
        style.Colors[(int)ImGuiCol.Separator] = UiColors.BackgroundFull;
        style.Colors[(int)ImGuiCol.SeparatorHovered] = UiColors.BackgroundFull;
        style.Colors[(int)ImGuiCol.TabUnfocused] = UiColors.BackgroundTabInActive;
        style.Colors[(int)ImGuiCol.WindowBg] = UiColors.Background;

        style.Colors[(int)ImGuiCol.CheckMark] = UiColors.CheckMark;
        style.Colors[(int)ImGuiCol.TabActive] = UiColors.BackgroundTabActive;
        style.Colors[(int)ImGuiCol.Tab] = UiColors.BackgroundTabInActive;
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = UiColors.BackgroundTabActive;
        style.Colors[(int)ImGuiCol.TabUnfocused] = UiColors.BackgroundTabInActive;
        style.Colors[(int)ImGuiCol.TitleBgActive] = UiColors.BackgroundFull;
        style.Colors[(int)ImGuiCol.TitleBg] = UiColors.BackgroundFull;
                
        style.WindowPadding = Vector2.Zero;
        style.FramePadding = new Vector2(7, 4);
        style.ItemSpacing = new Vector2(1, 1);
        style.ItemInnerSpacing = new Vector2(3, 2);
        style.GrabMinSize = 10;
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