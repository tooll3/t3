using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.InputUi
{
    public readonly struct UiProperties(Func<Color> getColor)
    {
        public Color Color => getColor();

        internal static readonly UiProperties Value = new(() => UiColors.ColorForValues);
        internal static readonly UiProperties Default = Value;
        internal static readonly UiProperties String = new(() => UiColors.ColorForString);
        internal static readonly UiProperties Texture = new(() => UiColors.ColorForTextures);
        internal static readonly UiProperties Command = new(() => UiColors.ColorForCommands);
        internal static readonly UiProperties Shader = new(() => UiColors.ColorForDX11);
        internal static readonly UiProperties GpuData = new(() =>UiColors.ColorForGpuData);
    }
}