namespace T3.Gui.InputUi
{
    public class FloatUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForValues;
    }

    public class PointListUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForPoints;
    }
    
    public class StringUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForString;
    }

    public class Size2UiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForValues;
    }

    public class IntUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForValues;
    }

    public class TextureUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForTextures;
    }

    
    public class CommandUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForCommands;
    }
    
    /// <summary>
    /// Internal implementation things that are below the tech level of normal artists.
    /// </summary>
    public class ShaderUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = new Color(0.681f, 0.034f, 0.283f, 1.000f);
    }

    public class FallBackUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = new Color(0.681f, 0.234f, 0.283f, 1.000f);
    }
}