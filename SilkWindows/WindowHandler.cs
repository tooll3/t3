using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using T3.SystemUi;

namespace SilkWindows;

internal sealed class WindowHandler
{
    public WindowHandler(WindowOptions options, IImguiDrawer drawer, string title, FontPack? fontPack)
    {
        var windowOptions = options;
        windowOptions.API = GraphicsAPI.Default;
        _windowOptions = windowOptions;
        
        _drawer = drawer;
        _windowTitle = title;
        _fontPack = fontPack;
    }
    
    public void RunUntilClosed()
    {
        var window = Window.Create(_windowOptions);
        window.Title = _windowTitle;
        _window = window;
        SubscribeToWindow();
        _window.Run();
        UnsubscribeFromWindow();
        Dispose();
        _imguiHandler?.RestoreContext();
        
        void Dispose()
        {
            _window.Dispose();
            
            _graphicsContext?.Dispose();
            _inputContext?.Dispose();
            
            _graphicsContext = null;
            _inputContext = null;
        }
        
        void SubscribeToWindow()
        {
            //window.FileDrop
            _window.Load += OnLoad;
            _window.Render += RenderWindowContents;
            _window.FramebufferResize += OnWindowResize;
            _window.Update += OnWindowUpdate;
            _window.FocusChanged += OnFocusChanged;
            _window.Closing += OnClose;
            _window.FileDrop += OnFileDrop;
        }
        
        void UnsubscribeFromWindow()
        {
            _window.Load -= OnLoad;
            _window.Render -= RenderWindowContents;
            _window.FramebufferResize -= OnWindowResize;
            _window.Update -= OnWindowUpdate;
            _window.FocusChanged -= OnFocusChanged;
            _window.Closing -= OnClose;
            _window.FileDrop -= OnFileDrop;
        }
    }
    
    private void OnFileDrop(string[] filePaths)
    {
        _drawer.OnFileDrop(filePaths);
    }
    
    private void OnFocusChanged(bool isFocused)
    {
        if (!isFocused && AlwaysOnTop)
        {
            // todo: force re-focus once silk.NET supports that ?
        }
        
        _drawer.OnWindowFocusChanged(isFocused);
    }
    
    private void RenderWindowContents(double deltaTime)
    {
        #if WH_DEBUG_FLOW
        Console.WriteLine("Starting render");
        #endif
        
        _graphicsContext!.ClearColor(_imguiHandler!.ClearColor);
        _graphicsContext.Clear(ClearBufferMask.ColorBufferBit);
        _imguiHandler!.Draw(_window.Size.ToVector2(), deltaTime);
    }
    
    private void OnLoad()
    {
        _inputContext = _window.CreateInput();
        _graphicsContext = _window.CreateOpenGL();
        _imguiHandler = new ImGuiHandler(_inputContext, _window, _graphicsContext, _drawer, _windowTitle, _fontPack);
    }
    
    private void OnClose()
    {
        _drawer.OnClose();
        _imguiHandler?.DisposeOfImguiContext();
    }
    
    private void OnWindowUpdate(double deltaSeconds)
    {
        _drawer.OnWindowUpdate(deltaSeconds, out var shouldClose);
        if (shouldClose)
            _window.Close();
    }
    
    private void OnWindowResize(Vector2D<int> size)
    {
        _graphicsContext!.Viewport(size);
    }
    
    private readonly IImguiDrawer _drawer;
    private IWindow _window;
    private IInputContext? _inputContext;
    private bool AlwaysOnTop => _windowOptions.TopMost;
    private readonly WindowOptions _windowOptions;
    private GL? _graphicsContext;
    private ImGuiHandler? _imguiHandler;
    private readonly FontPack? _fontPack;
    
    private readonly string _windowTitle;
}