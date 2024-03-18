using ImGuiNET;
using SharpDX.Direct3D11;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using T3.Core.Compilation;
using T3.Core.IO;
using T3.Core.Logging;
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
        public static IUiContentDrawer<SharpDX.Direct3D11.Device, ImDrawDataPtr> UiContentContentDrawer;
        public static Device Device { get; private set; }

        public const string Version = "3.9.1";

        /// <summary>
        /// Generate a release string with 
        /// </summary>
        public static string GetReleaseVersion(bool indicateDebugBuild = true)
        {
            var isDebug = "";
            #if DEBUG
            if (indicateDebugBuild)
            {
                isDebug = " Debug";
            }
            #endif

            return $"v{Version}{isDebug}";
        }

        [STAThread]
        private static void Main()
        {
            // Not calling this first will cause exceptions...
            Console.WriteLine("Starting T3 Editor");
            Console.WriteLine("Creating EditorUi");
            EditorUi.Instance = new MsFormsEditor();
            CoreUi.Instance = EditorUi.Instance;

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
            Log.Debug($"Starting {Version}");
            
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
            
            var userSettings = new UserSettings(saveOnQuit: true);
            var projectSettings = new ProjectSettings(saveOnQuit: true);

            Log.Debug($"About to initialize ProgramWindows");
            ProgramWindows.InitializeMainWindow(GetReleaseVersion(), out var device);

            Device = device;

            if (ShaderCompiler.Instance is not DX11ShaderCompiler shaderCompiler)
                throw new Exception("ShaderCompiler is not DX11ShaderCompiler");

            shaderCompiler.Device = device;

            Log.Debug($"About to initialize UiContentContentDrawer");
            UiContentContentDrawer = new WindowsUiContentDrawer();
            UiContentContentDrawer.Initialize(device, ProgramWindows.Main.Width, ProgramWindows.Main.Height);

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
                EditorUi.Instance
                        .ShowMessageBox($"Loading Operators failed:\n\n{uiException.Message}\n{innerException}\n\n" +
                                        $"This is liked caused by a corrupted operator file." +
                                        $"\nPlease try restarting and restore backup.",
                                        @"Error", PopUpButtons.Ok);
                EditorUi.Instance.ExitApplication();
            }

            SymbolAnalysis.UpdateUsagesOnly();

            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleFonts;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleViewports;

            UiContentUpdate.GenerateFontsWithScaleFactor(UserSettings.Config.UiScaleFactor);

            // Setup file watching the operator source
            T3Ui.InitializeEnvironment();

            unsafe
            {
                // Disable ImGui ini file settings
                ImGui.GetIO().NativePtr->IniFilename = null;
            }

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

            ProgramWindows.Main.RunRenderLoop(UiContentUpdate.RenderCallback);
            IsShuttingDown = true;

            try
            {
                ProjectSetup.DisposePackages();
                UiContentContentDrawer.Dispose();
            }
            catch (Exception e)
            {
                Log.Warning("Exception during shutdown: " + e.Message);
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