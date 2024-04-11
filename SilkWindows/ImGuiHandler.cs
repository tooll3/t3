using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using T3.SystemUi;

namespace SilkWindows;

internal sealed class ImGuiHandler
{
    private unsafe void InitializeStyle()
    {
        if (_previousContext.HasValue)
        {
            var myContext = ImGui.GetCurrentContext();
            ImGuiStyle copiedStyle = default;
            
            // first we switch to the previous imgui context
            ImGui.SetCurrentContext(_previousContext.Value);
            
            // we copy the style from the previous context
            Unsafe.Copy(destination: ref copiedStyle, source: ImGui.GetStyle().NativePtr);
            
            // we switch back to our own context
            ImGui.SetCurrentContext(myContext);
            
            // we apply the copied style to the current context
            Unsafe.Copy(ImGui.GetStyle().NativePtr, ref copiedStyle);
        }
        
        var colorVector = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg];
        var colorVecByteValue = colorVector * 255;
        ClearColor = Color.FromArgb((int)colorVecByteValue.X, (int)colorVecByteValue.Y, (int)colorVecByteValue.Z);
        
        // do we need or want to do this?
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        
        if (!_fontPack.HasValue)
        {
            _fontObj = new ImFonts([]);
            return;
        }
        
        var fontPack = _fontPack.Value;
        
        var io = ImGui.GetIO();
        var fontAtlasPtr = io.Fonts;
        var fonts = new ImFontPtr[4];
        fonts[0] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Small.Path, fontPack.Small.PixelSize);
        fonts[1] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Regular.Path, fontPack.Regular.PixelSize);
        fonts[2] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Bold.Path, fontPack.Bold.PixelSize);
        fonts[3] = fontAtlasPtr.AddFontFromFileTTF(fontPack.Large.Path, fontPack.Large.PixelSize);
        
        if (!fontAtlasPtr.Build())
        {
            Console.WriteLine("Failed to build font atlas");
        }
        
        _fontObj = new ImFonts(fonts);
        _drawer.Init();
    }
    
    private ImFonts? _fontObj;
    private readonly FontPack? _fontPack;
    private readonly string _mainWindowId;
    private readonly string _childWindowId;
    private static int _windowCounter = 999;
    private readonly ImGuiController _imguiController;
    private readonly IImguiDrawer _drawer;
    private readonly string _windowTitle;
    private readonly IntPtr? _previousContext;
    
    public ImGuiHandler(IInputContext inputContext, IView window, GL graphicsContext, IImguiDrawer drawer, string title, FontPack? fontPack)
    {
        _windowTitle = title;
        _mainWindowId = $"{title}##{Interlocked.Increment(ref _windowCounter)}";
        _childWindowId = $"{title}Child##{Interlocked.Increment(ref _windowCounter)}";
        _drawer = drawer;
        _fontPack = fontPack;
        var previousContext = ImGui.GetCurrentContext(); // was previously done in constructor of WindowHandler
        _previousContext = previousContext == IntPtr.Zero ? null : previousContext;
        _imguiController = new ImGuiController(gl: graphicsContext, window, inputContext, null, InitializeStyle);
    }
    
    public void Draw(Vector2 windowSize, double deltaTime)
    {
        _imguiController.Update((float)deltaTime);
        
        ImGui.SetNextWindowSize(windowSize);
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | 
                                             ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;
        
        ImGui.Begin(_mainWindowId, windowFlags);
        
        ImGui.BeginChild(_childWindowId, Vector2.Zero, false);
        _drawer.OnRender(_windowTitle, deltaTime, _fontObj!);
        ImGui.EndChild();
        
        ImGui.End();
        
        _imguiController.Render();
    }
    
    public void DisposeOfImguiContext()
    {
        //RestorePreviousImguiContext();
        _imguiController.Dispose(); // was previously done in Dispose of WindowHandler after the window stuff was disposed
    }
    
    public void RestoreContext()
    {
        if(_previousContext != null)
            ImGui.SetCurrentContext(_previousContext.Value);
    }
    
    public Color ClearColor { get; private set; } = Color.Black;
}