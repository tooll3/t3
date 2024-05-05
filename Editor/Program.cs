using ImGuiNET;
using SharpDX.Direct3D11;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using SilkWindows;
using T3.Core.Compilation;
using T3.Core.IO;
using T3.Core.Resource;
using T3.Core.SystemUi;
using T3.Core.UserData;
using T3.Editor.App;
using T3.Editor.Compilation;
using T3.Editor.Gui;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Camera;
using T3.Editor.Gui.Interaction.StartupCheck;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.SystemUi;
using T3.MsForms;
using T3.SystemUi;

namespace T3.Editor
{
    internal static class Program
    {
        public static IUiContentDrawer UiContentContentDrawer;
        public static Device Device { get; private set; }

        public static Version Version => RuntimeAssemblies.Version;
        private static string? _versionText;
        public static string VersionText
        {
            get
            {
                if (_versionText == null)
                {
                    _versionText = Version.ToBasicVersionString();
                    #if DEBUG
                    _versionText += " Debug";
                    #endif
                }

                return _versionText;
            }
        }

        [STAThread]
        private static void Main()
        {
            // Not calling this first will cause exceptions...
            Console.WriteLine("Starting T3 Editor");
            Console.WriteLine("Creating EditorUi");
            EditorUi.Instance = new MsFormsEditor();
            
            var windowProvider = new SilkWindowProvider();
            var imguiContextLock = windowProvider.ContextLock;
            ImGuiWindowService.Instance = windowProvider;
            BlockingWindow.Instance = windowProvider;

            Console.WriteLine("Creating DX11ShaderCompiler");
            ShaderCompiler.Instance = new DX11ShaderCompiler();

            Console.WriteLine("Validating startup location");
            StartupValidation.ValidateNotRunningFromSystemFolder();

            Console.WriteLine("Enabling DPI aware scaling");
            EditorUi.Instance.EnableDpiAwareScaling();

            var startupStopWatch = new Stopwatch();
            startupStopWatch.Start();

            CrashReporting.InitializeCrashReporting();

            Console.WriteLine("Creating SplashScreen");
            ISplashScreen splashScreen = new SplashScreen.SplashScreen();

            var path = Path.Combine(SharedResources.Directory, "t3-editor", "images", "t3-SplashScreen.png");
            splashScreen.Show(path);

            Console.WriteLine("Initializing logging");
            Log.AddWriter(splashScreen);
            Log.AddWriter(new ConsoleWriter());
            Log.AddWriter(FileWriter.CreateDefault(UserData.SettingsFolder, out var logPath));
            Log.AddWriter(StatusErrorLine);
            Log.AddWriter(ConsoleLogWindow);
            
            Log.Debug($"Starting {VersionText}");
            
            CrashReporting.LogPath = logPath;
            //if (IsStandAlone)
            {
                //StartupValidation.ValidateCurrentStandAloneExecutable();
            }
            //else
            {
                //StartupValidation.CheckInstallation();
            }

            StartUp.FlagBeginStartupSequence();

            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            ShaderCompiler.ShaderCacheSubdirectory = $"Editor_{VersionText}";
            
            var userSettings = new UserSettings(saveOnQuit: true);
            var projectSettings = new ProjectSettings(saveOnQuit: true);

            Log.Debug($"About to initialize ProgramWindows");
            ProgramWindows.InitializeMainWindow(VersionText, out var device);

            Device = device;

            if (ShaderCompiler.Instance is not DX11ShaderCompiler shaderCompiler)
                throw new Exception("ShaderCompiler is not DX11ShaderCompiler");

            shaderCompiler.Device = device;

            Log.Debug($"About to initialize UiContentContentDrawer");
            var contentDrawer = new WindowsUiContentDrawer();
            UiContentContentDrawer = contentDrawer;
            contentDrawer.Initialize(device, ProgramWindows.Main.Width, ProgramWindows.Main.Height, imguiContextLock, out var context);

            Log.Debug($"About to initialize Camera Interaction");
            var spaceMouse = new SpaceMouse(ProgramWindows.Main.HwndHandle);
            CameraInteraction.ManipulationDevices = new ICameraManipulator[] { spaceMouse };
            ProgramWindows.SetInteractionDevices(spaceMouse);

            Log.Debug($"About to initialize Resource Manager");
            ResourceManager.Instance().Init(device);
            SharedResources.Initialize();

            Log.Debug($"About to initialize T3 UI");

            // Initialize UI and load complete symbol model
            if (!ProjectSetup.TryInitialize(out var uiException))
            {
                Log.Error(uiException.Message + "\n\n" + uiException.StackTrace);
                var innerException = uiException.InnerException?.Message.Replace("\\r", "\r") ?? string.Empty;
                BlockingWindow.Instance.ShowMessageBox($"Loading Operators failed:\n\n{uiException.Message}\n{innerException}\n\n" +
                                            $"This is liked caused by a corrupted operator file." +
                                            $"\nPlease try restarting and restore backup.",
                                            @"Error", "Ok");
                EditorUi.Instance.ExitApplication();
            }

            SymbolAnalysis.UpdateUsagesOnly();
            
            UiContentContentDrawer.InitializeScaling();
            UiContentUpdate.CheckScaling();
            
            // Setup file watching the operator source
            T3Ui.InitializeEnvironment();
            
            Log.RemoveWriter(splashScreen);
            splashScreen.Close();
            splashScreen.Dispose();

            // Initialize optional Viewer Windows
            ProgramWindows.InitializeSecondaryViewerWindow("T3 Viewer", 640, 360);

            StartUp.FlagStartupSequenceComplete();

            startupStopWatch.Stop();
            Log.Debug($"Startup took {startupStopWatch.ElapsedMilliseconds}ms.");

            UiContentUpdate.StartMeasureFrame();

            T3Style.Apply();
            
            // ReSharper disable once AccessToDisposedClosure
            ProgramWindows.Main.RunRenderLoop(UiContentContentDrawer.RenderCallback);
            IsShuttingDown = true;

            try
            {
                ProjectSetup.DisposePackages();
                UiContentContentDrawer.Dispose();
            }
            catch (Exception e)
            {
                BlockingWindow.Instance.ShowMessageBox("Exception during shutdown: \n" + e);
            }

            // Release all resources
            try
            {
                ProgramWindows.Release();
            }
            catch (Exception e)
            {
                Log.Warning("Exception freeing resources: " + e.Message);
            }

            Log.Debug("Shutdown complete");
        }

        // Main loop
        public static readonly StatusErrorLine StatusErrorLine = new();
        public static readonly ConsoleLogWindow ConsoleLogWindow = new();
        public static string RequestImGuiLayoutUpdate;
        public static bool IsShuttingDown;
    }
}