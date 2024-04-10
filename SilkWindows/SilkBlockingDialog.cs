using T3.SystemUi;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using SilkWindows.Implementations;

namespace SilkWindows;

//https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.1%20-%20Hello%20Window/Program.cs
public class SilkBlockingDialog : IPopUpWindows
{
    public void Show(string text, string title)
    {
        Show(text, title, str => str,"Ok");
    }

    public void SetFonts(FontPack fontPack)
    {
        _fontPack = fontPack;
    }

    public T Show<T>(string text, string title, Func<T, string>? toString, params T[]? buttons)
    {
        return Show<MessageBox<T>, T>(title, new MessageBox<T>(text, buttons, toString));
    }
    
    public TData Show<TDrawer, TData>(string title, TDrawer drawer, in SimpleWindowOptions? options = null) where TDrawer : IImguiDrawer<TData>
    {
        var fullOptions = DefaultOptions;
        if (options.HasValue)
        {
            var val = options.Value;
            fullOptions.Size = val.Size.ToVector2DInt();
            fullOptions.FramesPerSecond = val.Fps;
            fullOptions.VSync = val.Vsync;
            fullOptions.WindowBorder = val.IsResizable ? WindowBorder.Resizable : WindowBorder.Fixed;
            fullOptions.TopMost = val.AlwaysOnTop;
        }
        
        var windowHandler = new WindowHandler(fullOptions, drawer, title, _fontPack);
        windowHandler.RunUntilClosed();
        return drawer.Result;
    }

    public void Show(string message)
    {
        Show(message, "Notice");
    }
    
    private FontPack? _fontPack;
    
    private static readonly WindowOptions DefaultOptions = new()
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