using System.Numerics;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace SilkWindows;

public sealed class WindowHelper
{
    public static void RunWindow(IImguiImplementation imguiImpl, IImguiDrawer drawer, FontPack? fontPack, object imguiContextLockObj)
    {
        var windowHelper = new WindowHelper(imguiImpl.WindowOptions, imguiImpl, drawer, fontPack, imguiContextLockObj);
        windowHelper.RunUntilClosed();
    }
    
    private WindowHelper(WindowOptions options, IImguiImplementation imguiImpl, IImguiDrawer drawer, FontPack? fontPack, object imguiContextLockObj)
    {
        _windowOptions = options;
        
        _drawer = drawer;
        _imguiImpl = imguiImpl;
        _fontPack = fontPack;
        _imguiContextLock = imguiContextLockObj;
    }
    
    public void RunUntilClosed()
    {
        var window = Window.Create(_windowOptions);
        _window = window;
        SubscribeToWindow();
        _window.Run();
        UnsubscribeFromWindow();
        Dispose();
        
        Console.WriteLine($"Disposed of window {_windowOptions.Title} ({_drawer.GetType()})");
        
        return;
        
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
            // todo: force re-focus once silk.NET supports that ? wayland may not allow it anyway..
        }
        
        _drawer.OnWindowFocusChanged(isFocused);
    }
    
    private void RenderWindowContents(double deltaTime)
    {
        #if WH_DEBUG_FLOW
        Console.WriteLine("Starting render");
        #endif
        
        lock (_imguiContextLock)
        {
            var windowSize = _window.Size;
            _imguiHandler!.Draw(new Vector2(windowSize.X, windowSize.Y), deltaTime);
        }
    }
    
    private void OnLoad()
    {
        _inputContext = _window.CreateInput();
        _graphicsContext = _imguiImpl.InitializeGraphicsAndInputContexts(_window);
        lock (_imguiContextLock)
        {
            _imguiHandler = new ImGuiHandler(_imguiImpl, _drawer , _fontPack, _imguiContextLock);
        }
        Console.WriteLine("Created imgui context");
    }
    
    private void OnClose()
    {
        lock (_imguiContextLock)
        {
            _drawer.OnClose();
            _imguiHandler?.DisposeOfImguiContext();
        }
        
        Console.WriteLine("Closed and disposed of imgui context");
    }
    
    private void OnWindowUpdate(double deltaSeconds)
    {
        _drawer.OnWindowUpdate(deltaSeconds, out var shouldClose);
        if (shouldClose)
        {
            _window.Close();
        }
    }
    
    private void OnWindowResize(Vector2D<int> size)
    {
        _imguiImpl.ResizeGraphicsContext(size);
    }
    
    private readonly object _imguiContextLock;
    private readonly IImguiDrawer _drawer;
    private readonly IImguiImplementation _imguiImpl;
    private IWindow _window;
    private IInputContext? _inputContext;
    private bool AlwaysOnTop => _windowOptions.TopMost;
    private readonly WindowOptions _windowOptions;
    private NativeAPI? _graphicsContext;
    private ImGuiHandler? _imguiHandler;
    private readonly FontPack? _fontPack;
}