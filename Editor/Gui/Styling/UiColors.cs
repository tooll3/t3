// ReSharper disable RedundantArgumentDefaultValue

using System.Numerics;

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
    public static Color ChildBackground = new(0.1f, 0.1f, 0.1f, 1);
    
    [T3Style.Hint(Description = "The opaque background. It's faded to with alpha.")]
    public static Color BackgroundFull = new(0f, 0f, 0f, 1f);
    
    [T3Style.Hint(Description = "Buttons and form inputs.")]
    public static Color BackgroundButton = new(0.16f,0.16f,0.16f,0.8f);
    public static Color BackgroundHover = new(0.26f,0.26f,0.26f,0.8f);
    
    [T3Style.Hint(Description = "Highlight color for pressed for activated ui elements.")]
    public static Color BackgroundActive = Color.FromString("#4592FF");
    
    [T3Style.Hint(Description = "The overlay grid on for the graph canvas.")]
    public static  Color CanvasGrid = new(0, 0, 0, 0.15f);
    
    public static Color BackgroundTabActive = Color.FromString("#3A3A3A");
    public static Color BackgroundTabInActive = Color.FromString("#CC282828");
    public static Color BackgroundInputField = Color.FromString("#222222");
    public static Color BackgroundInputFieldHover = new(0.1f, 0.1f, 0.1f, 1f);
    public static Color BackgroundInputFieldActive = new(0f, 0f, 0f, 1f);
    
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
    
    public static Color ColorForValues = new(0.525f, 0.550f, 0.554f, 1.000f);
    public static Color ColorForString = new(0.468f, 0.586f, 0.320f, 1.000f);
    public static Color ColorForTextures = new (0.625f, 0, 0.43f, 1.000f);
    public static Color ColorForDX11 = new(0.853f, 0.313f, 0.855f, 1.000f);
    public static Color ColorForCommands = new(0.132f, 0.722f, 0.762f, 1.000f);
    public static Color ColorForGpuData = new Color(0.681f, 0.034f, 0.283f, 1.000f);
    public static Color ScrollbarBackground =  new Color(0.12f, 0.12f, 0.12f, 0.53f);
    public static Color ScrollbarHandle =  new Color(0.31f, 0.31f, 0.31f, 0.33f);
    public static Color WindowBackground = new Color(0.1f, 0.1f, 0.1f, 0.98f);
    public static Color BackgroundPopup = new Color(0.1f, 0.1f, 0.1f, 0.98f);
    public static Color ModalWindowDimBg = new Color(0.1f, 0.1f, 0.1f, 0.1f);
    
    [T3Style.Hint(Description = "Background for menu, gaps and separators")]
    public static Color BackgroundGaps = new Color(0.1f, 0.1f, 0.1f, 0.1f);

    public static Color WindowBorder = new Color(0, 0, 0, 1f);

}