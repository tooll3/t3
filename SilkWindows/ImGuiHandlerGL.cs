using System.Drawing;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace SilkWindows;

// ReSharper disable once InconsistentNaming
internal sealed class ImGuiHandlerGL : IImguiImplementation
{
    public static ImGuiHandlerGL CreateDefault() => new(WindowOptions.Default);
    
    public ImGuiHandlerGL(WindowOptions options)
    {
        var graphicsApi = options.API.API;
        if (graphicsApi != ContextAPI.OpenGL && graphicsApi != ContextAPI.OpenGLES)
            throw new ArgumentException("This handler is for OpenGL only");
        
        WindowOptions = options;
    }
    
    public WindowOptions WindowOptions { get; }
    
    public void ClearFrame(in Color color)
    {
        _graphicsContext!.ClearColor(color);
        _graphicsContext.Clear(ClearBufferMask.ColorBufferBit);
    }
    
    public void StartUpdate(float deltaTime)
    {
        _imguiController!.Update(deltaTime);
    }
    
    public void Render()
    {
        _imguiController!.Render();
    }
    
    public void Dispose()
    {
        _imguiController!.Dispose();
    }
    
    public IntPtr InitializeControllerContext()
    {
        _imguiController = new ImGuiController(gl: _graphicsContext, _window, _inputContext, null);
        return _imguiController.Context;
    }
    
    public NativeAPI? InitializeGraphicsAndInputContexts(IWindow window)
    {
        _window = window;
        _graphicsContext = window.CreateOpenGL();
        _inputContext = window.CreateInput();
        return _graphicsContext;
    }
    
    public void ResizeGraphicsContext(Vector2D<int> size)
    {
        _graphicsContext!.Viewport(size);
    }
    
    private GL? _graphicsContext;
    private IWindow? _window;
    private IInputContext? _inputContext;
    private ImGuiController? _imguiController;
}