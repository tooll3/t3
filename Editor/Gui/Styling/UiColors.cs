// ReSharper disable RedundantArgumentDefaultValue

using System.ComponentModel;

namespace T3.Editor.Gui.Styling;

public static class UiColors
{
    // Text
    public static Color ForegroundFull = new(1f);
    public static Color Text = new(0.9f);
    public static Color TextMuted = new(0.5f);
    public static Color TextDisabled = new(0.2f);
    
    
    public static Color CheckMark = new(1,1,1,0.8f);

    // Backgrounds
    [T3Style.Hint(Description = "This is the primary background used for canvases and windows.")]
    public static Color Background = new(0.1f, 0.1f, 0.1f, 1);
    
    [T3Style.Hint(Description = "The opaque background. It's faded to with alpha.")]
    public static Color BackgroundFull = new(0f, 0f, 0f, 1f);
    
    [T3Style.Hint(Description = "Buttons and form inputs.")]
    public static Color BackgroundButton = Color.FromString("#CC282828");
    public static Color BackgroundHover = new(43, 65, 80, 255);
    
    [T3Style.Hint(Description = "Highlight color for pressed for activated ui elements.")]
    public static Color BackgroundActive = Color.FromString("#4592FF");
    
    [T3Style.Hint(Description = "The overlay grid on for the graph canvas.")]
    public static  Color CanvasGrid = new(0, 0, 0, 0.15f);
    
    public static Color BackgroundTabActive = Color.FromString("#3A3A3A");
    public static Color BackgroundTabInActive = Color.FromString("#CC282828");
    public static Color BackgroundInputField = new(0, 0, 0, 0f);
    public static Color BackgroundInputFieldHover = new(0.1f, 0.1f, 0.1f, 0f);
    public static Color BackgroundInputFieldActive = new(0, 0, 0, 0f);
    
    public static Color Gray = new(0.6f, 0.6f, 0.6f, 1);
    public static Color WindowResizeHandle = new (0.00f, 0.00f, 0.00f, 0.25f);
    
    public static Color StatusAutomated = new(0.6f, 0.6f, 1f, 1f);
    public static Color StatusAttention = new(203, 19, 113, 255);
    public static Color StatusWarning = new(203, 19, 113, 255);
    public static Color StatusError = new(203, 19, 113, 255);
    public static Color StatusAnimated = new(1f, 0.46f, 0f, 1f);
    public static Color Selection = new(1f, 1f, 1f, 1f);

    // Widget
    [T3Style.Hint(Description = "Fill color for something")]
    public static Color WidgetValueText = new(1, 1, 1, 0.5f);
    
    public static Color WidgetTitle = new(0.65f);
    public static Color WidgetValueTextHover = new(1, 1, 1, 1.2f);
    public static Color WidgetLine = new(1, 1, 1, 0.3f);
    public static Color WidgetLineHover = new(1, 1, 1, 0.7f);
    public static Color WidgetAxis = new(0, 0, 0, 0.3f);
    public static Color WidgetActiveLine = StatusAnimated;
    
    [T3Style.Hint(Description = "The opposite of text color. Mostly applied faded for shading effects.")]
    public static Color WidgetBackgroundStrong = new(0f, 0f, 0f, 1f);
    public static Color WidgetHighlight = new(1f, 1f, 1f, 1f);
    public static Color WidgetSlider = new(0.15f);


    public static Color MiniMapItems = new(1f, 1f, 1f, 1f);
}