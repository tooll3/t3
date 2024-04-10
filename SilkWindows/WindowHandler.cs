#undef WH_DEBUG_FLOW
#define WH_DEBUG_IMGUI
using System.Drawing;
using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using T3.SystemUi;

namespace SilkWindows;

internal sealed partial class WindowHandler
{
    public WindowHandler(WindowOptions options, IImguiDrawer drawer, string title, FontPack? fontPack)
    {
        _windowOptions = options;
        _windowOptions.API = GraphicsAPI.Default;
        _drawer = drawer;
        _mainWindowId = $"{title}##{Interlocked.Increment(ref _windowCounter)}";
        _childWindowId = $"{title}Child##{Interlocked.Increment(ref _windowCounter)}";
        _windowTitle = title;
        _fontPack = fontPack;
    }
    
    public void RunBlocking()
    {
        _window = Window.Create(_windowOptions);
        _window.Title = _windowTitle;
        
        //window.FileDrop
        SubscribeToWindow();
        var previousContext = ImGui.GetCurrentContext();
        _window.Run(); // blocking method
        _window.Dispose();
        ImGui.SetCurrentContext(previousContext);
        _window = null!;
    }
    
    private void SubscribeToWindow()
    {
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnWindowResize;
        _window.Update += OnWindowUpdate;
        _window.FocusChanged += OnFocusChanged;
        _window.Closing += OnClose;
    }
    
    private void UnsubscribeFromWindow()
    {
        _window.Load -= OnLoad;
        _window.Render -= OnRender;
        _window.FramebufferResize -= OnWindowResize;
        _window.Update -= OnWindowUpdate;
        _window.FocusChanged -= OnFocusChanged;
        _window.Closing -= OnClose;
    }
    
    private void OnFocusChanged(bool isFocused)
    {
        if (!isFocused && _windowOptions.TopMost)
        {
            // todo: force re-focus once silk.NET supports that ?
        }
    }
    
    private void OnRender(double deltaTime)
    {
        #if WH_DEBUG_FLOW
        Console.WriteLine("Starting render");
        #endif
        
        var clearColor = _clearColor.HasValue ? _clearColor!.Value : Color.Black;
        _graphicsContext!.ClearColor(clearColor);
        _graphicsContext.Clear(ClearBufferMask.ColorBufferBit);
        _imguiController!.Update((float)deltaTime);
        
        RestoreImguiTheme();
        
        var windowSize = _window!.Size.ToVector2();
        ImGui.SetNextWindowSize(windowSize);
        ImGui.SetNextWindowPos(new Vector2(0, 0));
        
        const ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | 
                                             ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;
        
        ImGui.Begin(_mainWindowId, windowFlags);
        
        ImGui.BeginChild(_childWindowId, Vector2.Zero, false);
        _drawer.OnRender(_windowTitle, deltaTime, _fontObj);
        ImGui.EndChild();
        
        ImGui.End();
        
        RevertImguiTheme();
        
        _imguiController.Render();
    }
    
    private void OnLoad()
    {
        _inputContext = _window!.CreateInput();
        _graphicsContext = _window.CreateOpenGL();
        CheckExistingImguiContext();
        
        _imguiController = new ImGuiController(gl: _graphicsContext, _window, _inputContext, null, InitializeFonts);
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
    }
    
    private void OnClose()
    {
        _drawer.OnClose();
        _closed = true;
    }
    
    private void OnWindowUpdate(double deltaSeconds)
    {
        if (_closed)
        {
            Dispose();
            return;
        }
        
        #if WH_DEBUG_FLOW
        Console.WriteLine("Starting window update");
        #endif
        
        _drawer.OnWindowUpdate(deltaSeconds, out var shouldClose);
        
        if (shouldClose)
        {
            _window!.Close();
        }
        
        #if WH_DEBUG_FLOW
        Console.WriteLine("Finished window update");
        #endif
    }
    
    private void Dispose()
    {
        UnsubscribeFromWindow();
        _window.Dispose();
        _graphicsContext?.Dispose();
        _inputContext?.Dispose();
        _imguiController?.Dispose();
        
        _graphicsContext = null!;
        _inputContext = null!;
        _imguiController = null!;
        _window = null;
    }
    
    private void OnWindowResize(Vector2D<int> size)
    {
        _graphicsContext!.Viewport(size);
    }
    
    private Color? _clearColor;
    private bool _closed;
    private readonly IImguiDrawer _drawer;
    private ImGuiController? _imguiController;
    private IWindow? _window;
    private IInputContext? _inputContext;
    private readonly WindowOptions _windowOptions;
    private GL? _graphicsContext;
    
    private readonly string _windowTitle;
    private readonly string _mainWindowId;
    private readonly string _childWindowId;
    private static int _windowCounter = 999;
}