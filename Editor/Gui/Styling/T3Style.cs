using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;

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
        public string GroupTitle;
        public string Description;
    }

    public const float ToolBarHeight = 25;
    public static readonly Vector2 WindowChildPadding = new(5,5);

    public static void Apply()
    {
        var style = ImGui.GetStyle();
        style.Colors[(int)ImGuiCol.Text] = UiColors.Text;
        style.Colors[(int)ImGuiCol.TextDisabled] = UiColors.TextDisabled;
        style.Colors[(int)ImGuiCol.Button] = UiColors.BackgroundButton;
        style.Colors[(int)ImGuiCol.ButtonHovered] = UiColors.BackgroundHover;
        style.Colors[(int)ImGuiCol.Border] =  UiColors.PopupBorder;
        style.Colors[(int)ImGuiCol.BorderShadow] = UiColors.BackgroundGaps;
        style.Colors[(int)ImGuiCol.FrameBg] = UiColors.BackgroundInputField;
        
        style.Colors[(int)ImGuiCol.FrameBgHovered] = UiColors.BackgroundInputFieldHover;
        style.Colors[(int)ImGuiCol.FrameBgActive] = UiColors.BackgroundInputFieldActive;
        
        style.Colors[(int)ImGuiCol.ScrollbarBg] = UiColors.ScrollbarBackground;
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = UiColors.ScrollbarHandle;
        style.Colors[(int)ImGuiCol.ResizeGrip] = UiColors.WindowResizeHandle;
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = Color.Transparent;
        style.Colors[(int)ImGuiCol.MenuBarBg] = UiColors.BackgroundGaps;
        style.Colors[(int)ImGuiCol.Separator] = UiColors.BackgroundGaps;
        style.Colors[(int)ImGuiCol.SeparatorHovered] = UiColors.BackgroundActive;
        style.Colors[(int)ImGuiCol.ButtonActive] = UiColors.BackgroundActive;
        
        style.Colors[(int)ImGuiCol.TabUnfocused] = UiColors.BackgroundTabInActive;
        style.Colors[(int)ImGuiCol.WindowBg] = UiColors.BackgroundGaps; // Only shines through at window edges
        style.Colors[(int)ImGuiCol.ChildBg] = UiColors.WindowBackground; // Graph see through strength
        style.Colors[(int)ImGuiCol.PopupBg] = UiColors.BackgroundPopup;
        //style.Colors[(int)ImGuiCol.] =  UiColors.BackgroundPopup;

        style.Colors[(int)ImGuiCol.CheckMark] = UiColors.CheckMark;
        style.Colors[(int)ImGuiCol.TabActive] = UiColors.BackgroundTabActive;
        style.Colors[(int)ImGuiCol.Tab] = UiColors.BackgroundTabInActive;
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = UiColors.BackgroundTabActive;
        style.Colors[(int)ImGuiCol.TabUnfocused] = UiColors.BackgroundTabInActive;
        style.Colors[(int)ImGuiCol.TitleBgActive] = UiColors.BackgroundGaps;
        style.Colors[(int)ImGuiCol.TitleBg] = UiColors.BackgroundGaps;
                
        style.WindowPadding = Vector2.Zero;
        style.FramePadding = new Vector2(7, 4);
        style.ItemSpacing = new Vector2(1, 1.49f);
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

    static public bool ColorsNeedUpdate;
}