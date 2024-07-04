using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.InputUi
{
    public sealed class ValueUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForValues;
    }
    
    public sealed class StringUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForString;
    }

    public sealed class TextureUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForTextures;
    }

    
    public sealed class CommandUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForCommands;
    }
    
    /// <summary>
    /// Internal implementation - things that are below the tech level of normal artists.
    /// </summary>
    public sealed class ShaderUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForDX11;
    }
    
    public sealed class GpuUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForGpuData;
    }
}