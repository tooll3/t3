using System.ComponentModel;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.IO;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Editor.Gui.Interaction.StartupCheck;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.SystemUi;
using Device = SharpDX.Direct3D11.Device;

namespace T3.Editor.App;

internal static class ProgramWindows
{
    public static AppWindow Main { get; private set; }
    public static AppWindow Viewer { get; private set; } // Required it distinguish 2nd render view in mouse handling   
    private static Device _device;
    private static DeviceContext _deviceContext;
    private static Factory _factory;

    internal static void SetMainWindowSize(int width, int height)
    {
        Main.SetSize(width, height);
        Main.SetBorderStyleSizable();
    }

    internal static void SetInteractionDevices(params object[] objects)
    {
        IWindowsFormsMessageHandler[] messageHandlers = objects.OfType<IWindowsFormsMessageHandler>().ToArray();
        ImGuiDx11RenderForm.InputMethods = messageHandlers;
    }

    public static void SetVertexShader(ShaderResource<VertexShader> resource) => _deviceContext.VertexShader.Set(resource.Shader);
    public static void SetPixelShader(ShaderResource<PixelShader> resource) => _deviceContext.PixelShader.Set(resource.Shader);

    internal static void HandleFullscreenToggle()
    {
        if (Main.IsFullScreen == UserSettings.Config.FullScreen)
            return;

        if (UserSettings.Config.FullScreen)
        {
            var screenCount = Screen.AllScreens.Length;
            Main.SetFullScreen(UserSettings.Config.FullScreenIndexMain < screenCount ? UserSettings.Config.FullScreenIndexMain : 0);
            Viewer.SetFullScreen(UserSettings.Config.FullScreenIndexViewer < screenCount ? UserSettings.Config.FullScreenIndexViewer : 0);
        }
        else
        {
            Main.SetSizeable();
            Viewer.SetSizeable();
        }
    }

    internal static void InitializeMainWindow(string version, out Device device)
    {
        Main = new("T3 " + version, disableClose: false);
        device = null;

        try
        {
            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware,
                                       DeviceCreationFlags.Debug,
                                       Main.SwapChainDescription,
                                       out device,
                                       out var swapchain);

            _device = device;
            _deviceContext = device.ImmediateContext;
            _factory = swapchain.GetParent<Factory>();

            foreach (var a in _factory.Adapters)
            {
                Log.Debug($"using {a.Description.Description}");
                break;
            }

            Main.SetDevice(device, _deviceContext, swapchain);

            Main.InitializeWindow(FormWindowState.Maximized, OnCloseMainWindow, true);

            // Ignore all windows events
            _factory.MakeWindowAssociation(Main.HwndHandle, WindowAssociationFlags.IgnoreAll);
        }
        catch (Exception e)
        {
            if (e.Message.Contains("DXGI_ERROR_SDK_COMPONENT_MISSING"))
            {
                var result =
                    BlockingWindow.Instance.ShowMessageBox("You need to install Windows Graphics diagnostics tools.\n\nClick Ok to download this Windows component directly from Microsoft.",
                                                          "Windows component missing", "Ok", "Cancel");
                if (result == "Ok")
                {
                    CoreUi.Instance
                          .OpenWithDefaultApplication("https://learn.microsoft.com/en-us/windows/uwp/gaming/use-the-directx-runtime-and-visual-studio-graphics-diagnostic-features");
                }
            }
            else
            {
                BlockingWindow.Instance.ShowMessageBox("We are sorry but your graphics hardware might not be capable of running Tooll3\n\n" + e.Message, "Oh noooo",
                                                 "Ok... /:");
            }

            Environment.Exit(0);
        }
    }

    internal static void InitializeSecondaryViewerWindow(string name, int width, int height)
    {
        Viewer = new(name, disableClose: true);
        Viewer.SetDevice(_device, _deviceContext);
        Viewer.SetSize(width, height);
        Viewer.SetSizeable();
        Viewer.InitViewSwapChain(_factory);
        Viewer.InitializeWindow(FormWindowState.Normal, null, false);
        Viewer.Show();
    }

    private static void OnCloseMainWindow(object sender, CancelEventArgs args)
    {
        if (EditableSymbolProject.IsSaving)
        {
            args.Cancel = true;
            Log.Debug($"Cancel closing because save-operation is in progress.");
        }
        else
        {
            Log.Debug("Shutting down");
        }
    }

    public static void Release()
    {
        Main.Release();
        Viewer.Release();
        _device.ImmediateContext.ClearState();
        _deviceContext.Flush();
        _device.Dispose();
        _deviceContext.Dispose();
        _factory.Dispose();
    }

    public static void SetRasterizerState(RasterizerState viewWindowRasterizerState)
    {
        _deviceContext.Rasterizer.State = viewWindowRasterizerState;
    }

    public static void SetPixelShaderSRV(ShaderResourceView viewWindowBackgroundSrv)
    {
        _deviceContext.PixelShader.SetShaderResource(0, viewWindowBackgroundSrv);
    }

    public static void CopyToSecondaryRenderOutput()
    {
        _deviceContext.Draw(3, 0);
        _deviceContext.PixelShader.SetShaderResource(0, null);
    }

    public static void RefreshViewport()
    {
        _deviceContext.Rasterizer.SetViewport(new Viewport(0, 0, Main.Width, Main.Height, 0.0f, 1.0f));
        _deviceContext.OutputMerger.SetTargets(Main.RenderTargetView);
    }

    public static void Present(bool useVSync, bool showSecondaryRenderWindow)
    {
        try
        {
            Main.SwapChain.Present(useVSync ? 1 : 0, PresentFlags.None);

            if (showSecondaryRenderWindow)
                Viewer.SwapChain.Present(useVSync ? 1 : 0, PresentFlags.None);
        }
        catch (SharpDX.SharpDXException e)
        {
            string description;
            var result = _device.DeviceRemovedReason;
            if (result == Result.Abort)
            {
                description = Result.Abort.Description;
            }
            else if (result == Result.AccessDenied)
            {
                description = Result.AccessDenied.Description;
            }
            else if (result == Result.Fail)
            {
                description = Result.Fail.Description;
            }
            else if (result == Result.Handle)
            {
                description = Result.Handle.Description;
            }
            else if (result == Result.InvalidArg)
            {
                description = Result.InvalidArg.Description;
            }
            else if (result == Result.NoInterface)
            {
                description = Result.NoInterface.Description;
            }
            else if (result == Result.NotImplemented)
            {
                description = Result.NotImplemented.Description;
            }
            else if (result == Result.OutOfMemory)
            {
                description = Result.OutOfMemory.Description;
            }
            else if (result == Result.InvalidPointer)
            {
                description = Result.InvalidPointer.Description;
            }
            else if (result == Result.UnexpectedFailure)
            {
                description = Result.UnexpectedFailure.Description;
            }
            else if (result == Result.WaitAbandoned)
            {
                description = Result.WaitAbandoned.Description;
            }
            else if (result == Result.WaitTimeout)
            {
                description = Result.WaitTimeout.Description;
            }
            else if (result == Result.Pending)
            {
                description = Result.Pending.Description;
            }
            else
            {
                description = "unknown reason";
            }

            var resultCode = result.ToString();
            throw (new ApplicationException($"Graphics card suspended ({resultCode}: {description}): {e.Message}"));
        }
    }
}