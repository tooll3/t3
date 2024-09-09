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

            Main.InitializeWindow(FormWindowState.Maximized, HandleKeyDown, HandleKeyUp, OnCloseMainWindow);

            // Ignore all windows events
            _factory.MakeWindowAssociation(Main.HwndHandle, WindowAssociationFlags.IgnoreAll);
        }
        catch (Exception e)
        {
            if (e.Message.Contains("DXGI_ERROR_SDK_COMPONENT_MISSING"))
            {
                var result =
                    EditorUi.Instance
                            .ShowMessageBox("You need to install Windows Graphics diagnostics tools.\n\nClick Ok to download this Windows component directly from Microsoft.",
                                            "Windows component missing", PopUpButtons.OkCancel);
                if (result == PopUpResult.Ok)
                {
                    StartupValidation
                       .OpenUrl("https://learn.microsoft.com/en-us/windows/uwp/gaming/use-the-directx-runtime-and-visual-studio-graphics-diagnostic-features");
                }
            }
            else
            {
                EditorUi.Instance.ShowMessageBox("We are sorry but your graphics hardware might not be capable of running Tooll2\n\n" + e.Message, "Oh noooo",
                                                 PopUpButtons.Ok);
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

    public static void SetPixelShaderSRV(ShaderResourceView viewWindowBackgroundSrv)
    {
        _deviceContext.PixelShader.SetShaderResource(0, viewWindowBackgroundSrv);
    }

    public static void DrawTextureToSecondaryRenderOutput()
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

    public static void RebuildUiCopyTextureIfRequired()
    {
        var needsRebuild = _uiCopyTexture == null ||
                           _uiCopyTexture.Description.Width != Main.SwapChain.Description.ModeDescription.Width ||
                           _uiCopyTexture.Description.Height != Main.SwapChain.Description.ModeDescription.Height;

        if (!needsRebuild)
            return;
        
        // Create a shader resource-compatible texture
        var textureDesc = new Texture2DDescription
                              {
                                  Width = Main.SwapChain.Description.ModeDescription.Width,
                                  Height = Main.SwapChain.Description.ModeDescription.Height,
                                  MipLevels = 1,
                                  ArraySize = 1,
                                  Format = Main.SwapChain.Description.ModeDescription.Format,
                                  SampleDescription = new SampleDescription(1, 0),
                                  Usage = ResourceUsage.Default,
                                  BindFlags = BindFlags.ShaderResource,
                                  CpuAccessFlags = CpuAccessFlags.None,
                                  OptionFlags = ResourceOptionFlags.None
                              };

        if (_uiCopyTexture is { IsDisposed: false })
            _uiCopyTexture.Dispose();
        
        _uiCopyTexture = new Texture2D(_device, textureDesc);
        
        if (UiCopyTextureSrv is { IsDisposed: false })
            UiCopyTextureSrv.Dispose();
        
        UiCopyTextureSrv = new ShaderResourceView(_device, _uiCopyTexture);
    }

    /// <summary>
    /// For things like presentations, demos or certain live performance situations it
    /// can be desired to share also T3's UI content on a second display.
    ///  
    /// On Windows duplicating a display is extremely expensive. This work around
    /// copies the last frame into a texture which is then presented on the second display.
    /// </summary>
    public static void CopyUiContentToShareTexture()
    {
        if (_uiCopyTexture == null || _uiCopyTexture.IsDisposed)
        {
            Log.Warning("Can't use undefined uiCopyTexture");
            return;
        }
        
        _deviceContext.CopyResource(Main.BackBufferTexture, _uiCopyTexture);
    }
    
    private static  Texture2D _uiCopyTexture;
    public static  ShaderResourceView UiCopyTextureSrv { get; private set; }


}