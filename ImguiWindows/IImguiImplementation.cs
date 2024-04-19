using System.Drawing;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace SilkWindows;

public interface IWindowImplementation
{
    public WindowOptions WindowOptions { get; }
    
    /// <summary>
    /// This function is called first and is used to set up your rendering context within your window, which was created
    /// with the <see cref="WindowOptions"/> property.
    /// </summary>
    /// <param name="window"></param>
    /// <returns></returns>
    NativeAPI? InitializeGraphicsAndInputContexts(IWindow window);
    
    /// <summary>
    /// This is called first in the rendering process - used to clear the frame with a specific color.
    /// </summary>
    /// <param name="color"></param>
    public void ClearFrame(in Color color);
    
    /// <summary>
    /// This is called to prime your implementation for rendering a new frame, after the frame has been cleared.
    /// </summary>
    /// <param name="deltaTime"></param>
    public void StartUpdate(float deltaTime);
    
    /// <summary>
    /// This is called after all draw commands have been issued, and is used to do the final rendering step of the Imgui frame.
    /// </summary>
    public void Render();
    
    /// <summary>
    /// Dispose of any resources that need to be disposed of.
    /// </summary>
    public void Dispose();
    
    /// <summary>
    /// Returns the imgui context pointer
    /// </summary>
    IntPtr InitializeControllerContext();
    
    void ResizeGraphicsContext(Vector2D<int> size);
}
public interface IImguiImplementation: IWindowImplementation
{
    public string MainWindowId => $"{WindowOptions.Title}##{Interlocked.Increment(ref _windowCounter)}";
    public string ChildWindowId => $"{WindowOptions.Title}##{Interlocked.Increment(ref _windowCounter)}";
    private static int _windowCounter = 999;
}