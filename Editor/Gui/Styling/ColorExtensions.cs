using ImGuiNET;
using T3.Core.DataTypes.Vector;

namespace T3.Editor.Gui.Styling
{
    public static class ColorExtensions
    {
        public static Color GetStyleColor(this ImGuiCol color)
        {
            unsafe
            {
                var c = ImGui.GetStyleColorVec4(color);
                return new Color(c->X, c->Y, c->Z, c->W);
            }
        }
    }
}