namespace T3.Gui.InputUi
{
    public class FloatUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = TypeUiRegistry.ColorForValues;
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
        public Color Color { get; set; } = new Color(0.518f, 0.046f, 0.228f, 1.000f);
    }

    public class FallBackUiProperties : ITypeUiProperties
    {
        public Color Color { get; set; } = new Color(0.518f, 0.046f, 0.228f, 1.000f);
    }
}