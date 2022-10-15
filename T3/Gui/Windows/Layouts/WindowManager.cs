using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.Exploration;
using T3.Gui.Windows.Output;
using T3.Gui.Windows.Variations;

namespace T3.Gui.Windows.Layouts
{
    public static class WindowManager
    {
        public static void Draw()
        {
            //Initialize();
            if (!_hasBeenInitialized)
                return;

            LayoutHandling.ProcessKeyboardShortcuts();

            if (KeyboardBinding.Triggered(UserActions.ToggleFullScreenGraph))
            {
                UserSettings.Config.ShowGraphOverContent = !UserSettings.Config.ShowGraphOverContent;
                ApplyGraphOverContentModeChange();
            }

            if (KeyboardBinding.Triggered(UserActions.ToggleVariationsWindow))
            {
                ToggleWindowTypeVisibility<VariationsWindow>();
            }

            UpdateAppWindowSize();

            foreach (var windowType in _windows)
            {
                windowType.Draw();
            }

            if (_demoWindowVisible)
                ImGui.ShowDemoWindow(ref _demoWindowVisible);

            if (_metricsWindowVisible)
                ImGui.ShowMetricsWindow(ref _metricsWindowVisible);
        }

        public static void Initialize()
        {
            _windows = new List<Window>()
                           {
                               new GraphWindow(),
                               new SettingsWindow(),
                               new ParameterWindow(),
                               new ExplorationWindow(),
                               new VariationsWindow(),
                               new OutputWindow(),
                               new ConsoleLogWindow(),
                               new SymbolLibrary(),
                               new LegacyVariationsWindow(),
                               new RenderSequenceWindow(),
                               new RenderVideoWindow(),
                           };

            // Wait first frame for ImGUI to initialize
            if (ImGui.GetTime() > 1 && _hasBeenInitialized)
                return;

            //LayoutHandling.LoadAndApplyLayout(UserSettings.Config.WindowLayoutIndex);
            
            if (UserSettings.Config.ShowGraphOverContent)
            {
                HideAllWindowBesidesMainGraph();
                SetGraphWindowAsBackground();
            }

            _appWindowSize = ImGui.GetIO().DisplaySize;
            _hasBeenInitialized = true;
        }

        public static void DrawWindowMenuContent()
        {
            foreach (var window in _windows)
            {
                window.DrawMenuItemToggle();
            }

            if (ImGui.MenuItem("2nd Render Window", "", ShowSecondaryRenderWindow))
                ShowSecondaryRenderWindow = !ShowSecondaryRenderWindow;

            ImGui.Separator();

            if (ImGui.MenuItem("ImGUI Demo", "", _demoWindowVisible))
                _demoWindowVisible = !_demoWindowVisible;

            if (ImGui.MenuItem("ImGUI Metrics", "", _metricsWindowVisible))
                _metricsWindowVisible = !_metricsWindowVisible;

            LayoutHandling.DrawMainMenuItems();
        }

        public static void ApplyGraphOverContentModeChange()
        {
            if (UserSettings.Config.ShowGraphOverContent)
            {
                HideAllWindowBesidesMainGraph();
                SetGraphWindowAsBackground();
                UserSettings.Config.FullScreen = true;
            }
            else
            {
                SetGraphWindowToNormal();
                LayoutHandling.LoadAndApplyLayout(UserSettings.Config.WindowLayoutIndex);
            }
        }

        public static void SetGraphWindowToNormal()
        {
            var graphWindow1 = GraphWindow.GetPrimaryGraphWindow();
            if (graphWindow1 == null)
                return;
            graphWindow1.WindowFlags &= ~(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove |
                                          ImGuiWindowFlags.NoResize);
            graphWindow1.ApplySizeAndPosition();
        }

        public static Vector2 GetRelativePositionFromPixel(Vector2 pixel)
        {
            return pixel / _appWindowSize;
        }

        public static Vector2 GetPixelPositionFromRelative(Vector2 fraction)
        {
            return fraction * _appWindowSize;
        }

        public static IEnumerable<Window> GetAllWindows()
        {
            foreach (var window in _windows)
            {
                if (window.AllowMultipleInstances)
                {
                    foreach (var windowInstance in window.GetInstances())
                    {
                        yield return windowInstance;
                    }
                }
                else
                {
                    yield return window;
                }
            }
        }

        public static bool IsAnyInstanceVisible<T>() where T : Window
        {
            return GetAllWindows().OfType<T>().Any(w => w.Config.Visible);
        }

        private static void SetGraphWindowAsBackground()
        {
            var graphWindow1 = GraphWindow.GetPrimaryGraphWindow();
            if (graphWindow1 == null)
                return;

            var yPadding = UserSettings.Config.ShowGraphOverContent ? 0 : MainMenuBarHeight;
            var pos = GetRelativePositionFromPixel(new Vector2(0, yPadding));

            graphWindow1.Config.Position = pos;
            graphWindow1.Config.Size = new Vector2(1, 1 - pos.Y);

            graphWindow1.WindowFlags |= ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove |
                                        ImGuiWindowFlags.NoResize;
            graphWindow1.ApplySizeAndPosition();
        }

        private static void HideAllWindowBesidesMainGraph()
        {
            var graphWindowIsMain = true;

            foreach (var windowType in _windows)
            {
                foreach (var w in windowType.GetInstances())
                {
                    if (w is GraphWindow && graphWindowIsMain)
                    {
                        graphWindowIsMain = false;
                    }
                    else
                    {
                        w.Config.Visible = false;
                    }
                }
            }
        }

        private static void UpdateAppWindowSize()
        {
            var newSize = ImGui.GetIO().DisplaySize;
            if (newSize == _appWindowSize)
                return;

            _appWindowSize = newSize;

            LayoutHandling.UpdateAfterResize(newSize);
        }

        private static void ToggleWindowTypeVisibility<T>() where T : Window
        {
            var instances = GetAllWindows().OfType<T>().ToList();
            if (instances.Count != 1)
                return;

            instances[0].Config.Visible = !instances[0].Config.Visible;
        }

        private static Vector2 _appWindowSize;
        private const float MainMenuBarHeight = 25;
        private static List<Window> _windows;
        private static bool _demoWindowVisible;
        private static bool _metricsWindowVisible;
        public static bool ShowSecondaryRenderWindow { get; private set; }
        public static bool IsWindowMinimized => _appWindowSize == Vector2.Zero;
        private static bool _hasBeenInitialized;
    }
}