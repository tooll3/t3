using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.InputUi
{
    public class ValueUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForValues;
    }

    public class PointListUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForValues;
    }
    
    public class StringUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForString;
    }

    public class TextureUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForTextures;
    }

    
    public class CommandUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForCommands;
    }
    
    /// <summary>
    /// Internal implementation - things that are below the tech level of normal artists.
    /// </summary>
    public class ShaderUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForDX11;
    }

    public class FallBackUiProperties : ITypeUiProperties
    {
        public Color Color => UiColors.ColorForGpuData;
    }
}