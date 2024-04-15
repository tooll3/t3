namespace T3.SystemUi;

public interface IUiContentDrawer<in TRenderDevice> : IUiContentDrawer
{
    public void Initialize(TRenderDevice device, int width, int height, object imguiContextLockObj, out IntPtr context);
}

public interface IUiContentDrawer : IDisposable
{
    public bool CreateDeviceObjects();
    void InitializeScaling();
    void RenderCallback();
}