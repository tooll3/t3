using System.Drawing;
using System.Numerics;
using ImGuiNET;
using T3.SystemUi;

namespace SilkWindows;

internal sealed partial class WindowHandler
{
    private void InitializeFonts()
    {
        if (!_fontPack.HasValue)
        {
            _fontObj = new Fonts([]);
            return;
        }
        
        var fontPack = _fontPack.Value;
        
        var io = ImGui.GetIO();
        var fontAtlasPtr = io.Fonts;
        var fonts = new ImFontPtr[4];
        fonts[0] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Small.Path, fontPack.Small.PixelSize);
        fonts[1] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Regular.Path, fontPack.Regular.PixelSize);
        fonts[2] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Bold.Path, fontPack.Bold.PixelSize);
        fonts[3] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Large.Path, fontPack.Large.PixelSize);
        
        if (!fontAtlasPtr.Build())
        {
            Console.WriteLine("Failed to build font atlas");
        }
        
        _fontObj = new Fonts(fonts);
    }
    
    public sealed class Fonts(ImFontPtr[] fonts)
    {
        private readonly bool _hasFonts = fonts.Length > 3;
        public ImFontPtr Small => _hasFonts ? fonts[0] : ImGui.GetIO().Fonts.Fonts[0];
        public ImFontPtr Regular => _hasFonts ? fonts[1] : ImGui.GetIO().Fonts.Fonts[0];
        public ImFontPtr Bold => _hasFonts ? fonts[2] : ImGui.GetIO().Fonts.Fonts[0];
        public ImFontPtr Large => _hasFonts ? fonts[3] : ImGui.GetIO().Fonts.Fonts[0];
    }
    
    private sealed class BorderInfo(ImGuiStylePtr style)
    {
        public readonly float ChildSize = style.ChildBorderSize;
        public readonly float FrameSize = style.FrameBorderSize;
        public readonly float WindowSize = style.WindowBorderSize;
        public readonly float SeparatorTextSize = style.SeparatorTextBorderSize;
    }
    
    private Fonts? _fontObj;
    private readonly FontPack? _fontPack;
    private Vector4[] _previousImguiColors = [];
    private BorderInfo? _borderInfo;
    
    private void CheckExistingImguiContext()
    {
        var previousContext = ImGui.GetCurrentContext();
        if (previousContext != IntPtr.Zero)
        {
            var style = ImGui.GetStyle();
            var colors = style.Colors;
            _previousImguiColors = new Vector4[colors.Count];
            for (int i = 0; i < ImGui.GetStyle().Colors.Count; i++)
            {
                _previousImguiColors[i] = colors[i];
            }
            
            _borderInfo = new BorderInfo(style);
        }
    }
    
    private void RestoreImguiTheme()
    {
        // apply color theme
        for (var index = 0; index < _previousImguiColors.Length; index++)
        {
            var color = _previousImguiColors[index];
            ImGui.PushStyleColor((ImGuiCol)index, color);
        }
        
        if (!_clearColor.HasValue)
        {
            var colorVector = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
            var colorVecByteValue = colorVector * 255;
            _clearColor = Color.FromArgb((int)colorVecByteValue.X, (int)colorVecByteValue.Y, (int)colorVecByteValue.Z);
        }
        
        if (_borderInfo != null)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, _borderInfo!.ChildSize);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, _borderInfo.WindowSize);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, _borderInfo.FrameSize);
            ImGui.PushStyleVar(ImGuiStyleVar.SeparatorTextBorderSize, _borderInfo.SeparatorTextSize);
        }
        ImGui.PushFont(_fontObj!.Regular);
    }
    
    private void RevertImguiTheme()
    {
        ImGui.PopFont();
        ImGui.PopStyleColor(_previousImguiColors.Length);
        if (_borderInfo != null)
        {
            ImGui.PopStyleVar(4);
        }
    }
}