using ImGuiNET;

namespace SilkWindows;

public sealed class ImFonts(ImFontPtr[] fonts)
{
    private readonly bool _hasFonts = fonts.Length > 3;
    public ImFontPtr Small => _hasFonts ? fonts[0] : ImGui.GetIO().Fonts.Fonts[0];
    public ImFontPtr Regular => _hasFonts ? fonts[1] : ImGui.GetIO().Fonts.Fonts[0];
    public ImFontPtr Bold => _hasFonts ? fonts[2] : ImGui.GetIO().Fonts.Fonts[0];
    public ImFontPtr Large => _hasFonts ? fonts[3] : ImGui.GetIO().Fonts.Fonts[0];
}