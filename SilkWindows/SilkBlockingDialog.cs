using T3.SystemUi;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Window = Silk.NET.SDL.Window;

namespace SilkWindows;

//https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.1%20-%20Hello%20Window/Program.cs
public class SilkBlockingDialog : IPopUpWindows
{
    public void Show(string text, string title)
    {
        Show(text, title, str => str,"Ok");
    }

    public float UiScale { get; set; }

    public void SetFonts(FontPack fontPack)
    {
        _fontPack = fontPack;
    }

    public T Show<T>(string text, string title, Func<T, string>? toString, params T[]? buttons)
    {
        var options = BlockingPopupOptions;
        if (buttons == null || buttons.Length == 0)
        {
            buttons = [];
        }

        toString ??= item => item!.ToString()!;
        
        var messageBox = new MessageBox<T>(text, buttons, toString);
        var windowHandler = new WindowHandler(BlockingPopupOptions, messageBox, title, _fontPack);
        windowHandler.RunBlocking();
        return messageBox.Result;
    }

    public void Show(string message)
    {
        Show(message, "Notice");
    }
    
    private FontPack? _fontPack;

    private static readonly WindowOptions BlockingPopupOptions = new()
                                                                   {
                                                                       IsEventDriven = true,
                                                                       ShouldSwapAutomatically = true,
                                                                       IsVisible = true,
                                                                       Position = new Vector2D<int>(600, 600),
                                                                       Size = new Vector2D<int>(400, 320),
                                                                       FramesPerSecond = 60,
                                                                       UpdatesPerSecond = 60,
                                                                       PreferredDepthBufferBits = 0,
                                                                       PreferredStencilBufferBits = 0,
                                                                       PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8),
                                                                       Samples = 0,
                                                                       VSync = true,
                                                                       TopMost = true,
                                                                       WindowBorder = WindowBorder.Resizable
                                                                   };
}