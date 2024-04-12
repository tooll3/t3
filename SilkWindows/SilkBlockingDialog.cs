using System.Numerics;
using T3.SystemUi;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using SilkWindows.Implementations;

namespace SilkWindows;

//https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.1%20-%20Hello%20Window/Program.cs
public sealed class SilkBlockingDialog : IPopUpWindows
{
    public void ShowMessageBox(string text, string title)
    {
        ShowMessageBox(text, title, str => str,"Ok");
    }

    public void SetFonts(FontPack fontPack)
    {
        _fontPack = fontPack;
    }

    public T? ShowMessageBox<T>(string text, string title, Func<T, string>? toString, params T[]? buttons)
    {
        return Show(title, new MessageBox<T>(text, buttons, toString), DefaultSimpleOptions);
    }
    
    public TData? Show<TData>(string title, IImguiDrawer<TData> drawer, in SimpleWindowOptions? options = null)
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

    public void ShowMessageBox(string message)
    {
        ShowMessageBox(message, "Notice");
    }
    
    private FontPack? _fontPack;
    
    private static readonly SimpleWindowOptions DefaultSimpleOptions = new( new Vector2(400, 320), 60, true, true, true);
    
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