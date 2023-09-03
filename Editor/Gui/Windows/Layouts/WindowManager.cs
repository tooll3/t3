using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Editor.Gui.Graph;
using ImGuiNET;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Exploration;
using T3.Editor.Gui.Windows.Output;
using T3.Editor.Gui.Windows.RenderExport;
using T3.Editor.Gui.Windows.Variations;

namespace T3.Editor.Gui.Windows.Layouts
{
    public static class WindowManager
    {
        public static void Draw()
        {
            TryToInitialize(); // We have to keep initializing until window sizes are initialized
            if (!_hasBeenInitialized)
                return;

            LayoutHandling.ProcessKeyboardShortcuts();

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

        public static void TryToInitialize()
        {
            // Wait first frame for ImGUI to initialize
            if (ImGui.GetTime() > 1 && _hasBeenInitialized)
                return;
            
            _windows = new List<Window>()
                           {
                               new ParameterWindow(),
                               new GraphWindow(),
                               new OutputWindow(),
                               new VariationsWindow(),
                               new ExplorationWindow(),
                               new SymbolLibrary(),
                               new RenderSequenceWindow(),
                               new RenderVideoWindow(),
                               new UtilitiesWindow(),
                               Program.ConsoleLogWindow,
                               new IoViewWindow(),
                               new SettingsWindow(),
                           };            

            LayoutHandling.LoadAndApplyLayoutOrFocusMode(UserSettings.Config.WindowLayoutIndex);

            _appWindowSize = ImGui.GetIO().DisplaySize;
            _hasBeenInitialized = true;
        }

        public static void DrawWindowMenuContent()
        {
            foreach (var window in _windows)
            {
                window.DrawMenuItemToggle();
            }
            ImGui.Separator();
            
            if (ImGui.BeginMenu("Debug"))
            {
                if (ImGui.MenuItem("ImGUI Demo", "", _demoWindowVisible))
                    _demoWindowVisible = !_demoWindowVisible;

                if (ImGui.MenuItem("ImGUI Metrics", "", _metricsWindowVisible))
                    _metricsWindowVisible = !_metricsWindowVisible;
                
                ImGui.EndMenu();
            }
            //ImGui.EndMenu();
            
            ImGui.Separator();
            
            
            if (ImGui.MenuItem("2nd Render Window", "", ShowSecondaryRenderWindow))
                ShowSecondaryRenderWindow = !ShowSecondaryRenderWindow;
            
            ImGui.Separator();

            LayoutHandling.DrawMainMenuItems();
        }
        

        public static void SetGraphWindowToNormal()
        {
            var graphWindow1 = GraphWindow.GetPrimaryGraphWindow();
            if (graphWindow1 == null)
                return;
            graphWindow1.WindowFlags &= ~(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove |
                                          ImGuiWindowFlags.NoResize);
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
        
        public static void ToggleInstanceVisibility<T>() where T : Window
        {
            var foundFirst = false;
            var newVisibility = false;
            foreach (var w in GetAllWindows().OfType<T>())
            {
                if (!foundFirst)
                {
                    newVisibility = !w.Config.Visible;
                    foundFirst = true;
                }

                w.Config.Visible = newVisibility;
            }
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
        private static List<Window> _windows;
        private static bool _demoWindowVisible;
        private static bool _metricsWindowVisible;
        public static bool ShowSecondaryRenderWindow { get; private set; }
        private static bool _hasBeenInitialized;
    }
}