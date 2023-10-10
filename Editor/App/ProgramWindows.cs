using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Editor.Gui;
using T3.Editor.Gui.Interaction.StartupCheck;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
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

    public static void SetVertexShader(VertexShaderResource resource) => _deviceContext.VertexShader.Set(resource.VertexShader);
    public static void SetPixelShader(PixelShaderResource resource) => _deviceContext.PixelShader.Set(resource.PixelShader);

    internal static void HandleFullscreenToggle()
    {
        if (Main.IsFullScreen == UserSettings.Config.FullScreen)
            return;

        if (UserSettings.Config.FullScreen)
        {
            var ScreenCount = Screen.AllScreens.Length;
            Main.SetFullScreen(UserSettings.Config.FullScreenIndexMain < ScreenCount ? UserSettings.Config.FullScreenIndexMain : 0);
            Viewer.SetFullScreen(UserSettings.Config.FullScreenIndexViewer < ScreenCount ? UserSettings.Config.FullScreenIndexViewer : 0);
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

            Main.SetDevice(device, _deviceContext, swapchain);

            Main.InitializeWindow(FormWindowState.Maximized, HandleKeyDown, HandleKeyUp, OnCloseMainWindow);

            // Ignore all windows events
            _factory.MakeWindowAssociation(Main.HwndHandle, WindowAssociationFlags.IgnoreAll);
        }
        catch (Exception e)
        {
            if (e.Message.Contains("DXGI_ERROR_SDK_COMPONENT_MISSING"))
            {
                var result = EditorUi.Instance.ShowMessageBox("You need to install Windows Graphics diagnostics tools.\n\nClick Ok to download this Windows component directly from Microsoft.", "Windows component missing", PopUpButtons.OkCancel);
                if (result == PopUpResult.Ok)
                {
                    StartupValidation.OpenUrl("https://learn.microsoft.com/en-us/windows/uwp/gaming/use-the-directx-runtime-and-visual-studio-graphics-diagnostic-features");
                }
            }
            else
            {
                EditorUi.Instance.ShowMessageBox("We are sorry but your graphics hardware might not be capable of running Tooll2\n\n" +e.Message, "Oh noooo", PopUpButtons.Ok);
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
        Viewer.InitializeWindow(FormWindowState.Normal, null, null, null);
        Viewer.Show();
    }

    private static void OnCloseMainWindow(object sender, CancelEventArgs args)
    {
        if (T3Ui.UiSymbolData.IsSaving)
        {
            args.Cancel = true;
            Log.Debug($"Cancel closing because save-operation is in progress.");
        }
        else
        {
            Log.Debug("Shutting down");
        }
    }

    private static void HandleKeyDown(object sender, KeyEventArgs e)
    {
        var keyIndex = (int)e.KeyCode;
        if (keyIndex >= KeyHandler.PressedKeys.Length)
        {
            Log.Warning($"Ignoring out of range key code {e.KeyCode} with index {keyIndex}");
        }
        else
        {
            KeyHandler.PressedKeys[keyIndex] = true;
        }
    }

    private static void HandleKeyUp(object sender, KeyEventArgs e)
    {
        var keyIndex = (int)e.KeyCode;
        if (keyIndex < KeyHandler.PressedKeys.Length)
        {
            KeyHandler.PressedKeys[keyIndex] = false;
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

    public static void SetPixelShaderResource(ShaderResourceView viewWindowBackgroundSrv)
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
        Main.SwapChain.Present(useVSync ? 1 : 0, PresentFlags.None);

        if (showSecondaryRenderWindow)
            Viewer.SwapChain.Present(useVSync ? 1 : 0, PresentFlags.None);
    }
}