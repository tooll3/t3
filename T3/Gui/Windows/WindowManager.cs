using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.Exploration;
using T3.Gui.Windows.Output;
using T3.Gui.Windows.Variations;

namespace T3.Gui.Windows
{
    public class WindowManager
    {
        public WindowManager()
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
                           };
        }

        private readonly UserActions[] _loadLayoutActions =
            {
                UserActions.LoadLayout0,
                UserActions.LoadLayout1,
                UserActions.LoadLayout2,
                UserActions.LoadLayout3,
                UserActions.LoadLayout4,
                UserActions.LoadLayout5,
                UserActions.LoadLayout6,
                UserActions.LoadLayout7,
                UserActions.LoadLayout8,
                UserActions.LoadLayout9,
            };

        private readonly UserActions[] _saveLayoutActions =
            {
                UserActions.SaveLayout0,
                UserActions.SaveLayout1,
                UserActions.SaveLayout2,
                UserActions.SaveLayout3,
                UserActions.SaveLayout4,
                UserActions.SaveLayout5,
                UserActions.SaveLayout6,
                UserActions.SaveLayout7,
                UserActions.SaveLayout8,
                UserActions.SaveLayout9,
            };

        public void Draw()
        {
            Initialize();
            if (!_hasBeenInitialized)
                return;
            
            // Process Keyboard shortcuts
            for (var i = 0; i < _saveLayoutActions.Length; i++)
            {
                if (KeyboardBinding.Triggered(_saveLayoutActions[i]))
                    SaveLayout(i);

                if (KeyboardBinding.Triggered(_loadLayoutActions[i]))
                    LoadAndApplyLayout(i);
            }
            
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

        private void Initialize()
        {
            // Wait first frame for ImGUI to initialize
            if (ImGui.GetTime() > 1 && _hasBeenInitialized)
                return;

            if (File.Exists(GetLayoutFilename(UserSettings.Config.WindowLayoutIndex)))
            {
                LoadAndApplyLayout(UserSettings.Config.WindowLayoutIndex);
            }
            
            if (UserSettings.Config.ShowGraphOverContent)
            {
                HideAllWindowBesidesMainGraph();
                SetGraphWindowAsBackground();
            }
            
            _appWindowSize = ImGui.GetIO().DisplaySize;
            _hasBeenInitialized = true;
        }

        public void DrawWindowMenuContent()
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

            if (ImGui.MenuItem("Save layout", ""))
                SaveLayout(0);

            if (ImGui.BeginMenu("Load layout"))
            {
                for (int i = 0; i < 10; i++)
                {
                    if (ImGui.MenuItem("Layout " + (i + 1), "F" + (i + 1), false, enabled: DoesLayoutExists(i)))
                    {
                        LoadAndApplyLayout(i);
                    }
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Save layouts"))
            {
                for (int i = 0; i < 10; i++)
                {
                    if (ImGui.MenuItem("Layout " + (i + 1), "Ctrl+F" + (i + 1)))
                    {
                        SaveLayout(i);
                    }
                }

                ImGui.EndMenu();
            }
        }

        public void ApplyGraphOverContentModeChange()
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
                LoadAndApplyLayout(UserSettings.Config.WindowLayoutIndex);
            }
        }
        
        private void HideAllWindowBesidesMainGraph()
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

        private void SetGraphWindowToNormal()
        {
            var graphWindow1 = GraphWindow.GetPrimaryGraphWindow();
            if (graphWindow1 == null)
                return;
            graphWindow1.WindowFlags &= ~(ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
            graphWindow1.ApplySizeAndPosition();            
        }
        
        private void SetGraphWindowAsBackground()
        {
            var graphWindow1 = GraphWindow.GetPrimaryGraphWindow();
            if (graphWindow1 == null)
                return;
            
            var yPadding = UserSettings.Config.ShowGraphOverContent ? 0 : MainMenuBarHeight;
            var pos = GetRelativePositionFromPixel(new Vector2(0, yPadding));
            
            graphWindow1.Config.Position = pos;
            graphWindow1.Config.Size = new Vector2(1, 1 - pos.Y);
            
            graphWindow1.WindowFlags |= ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
            graphWindow1.ApplySizeAndPosition();
        }
        

        private void SaveLayout(int index)
        {
            var allWindowConfigs = GetAllWindows().Select(window => window.Config).ToList();

            if (!Directory.Exists(".t3"))
                Directory.CreateDirectory(".t3");

            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            using (var file = File.CreateText(GetLayoutFilename(index)))
            {
                serializer.Serialize(file, allWindowConfigs);
            }

            UserSettings.Config.WindowLayoutIndex = index;
        }

        private void LoadAndApplyLayout(int index)
        {
            var filename = GetLayoutFilename(index);
            if (!File.Exists(filename))
            {
                Log.Warning($"Layout {filename} doesn't exist yet");
                return;
            }

            var jsonBlob = File.ReadAllText(filename);
            var serializer = JsonSerializer.Create();
            var fileTextReader = new StringReader(jsonBlob);
            if (!(serializer.Deserialize(fileTextReader, typeof(List<Window.WindowConfig>))
                      is List<Window.WindowConfig> configurations))
            {
                Log.Error("Can't load layout");
                return;
            }

            UserSettings.Config.ShowGraphOverContent = false;
            SetGraphWindowToNormal();
            
            ApplyConfigurations(configurations);
            UserSettings.Config.WindowLayoutIndex = index;
        }

        private void ApplyConfigurations(List<Window.WindowConfig> configurations)
        {
            foreach (var config in configurations)
            {
                var matchingWindow = GetAllWindows().FirstOrDefault(window => window.Config.Title == config.Title);
                if (matchingWindow == null)
                {
                    if (config.Title.StartsWith("Graph#"))
                    {
                        if (GraphWindow.CanOpenAnotherWindow())
                        {
                            matchingWindow = new GraphWindow();
                            matchingWindow.Config = config;
                        }
                    }
                    else if (config.Title.StartsWith("Output#"))
                    {
                        matchingWindow = new OutputWindow();
                        matchingWindow.Config = config;
                    }
                    else if (config.Title.StartsWith("Parameters#"))
                    {
                        matchingWindow = new ParameterWindow();
                        matchingWindow.Config = config;
                    }

                    // else
                    // {
                    //     Log.Error($"Can't find type of window '{config.Title}'");
                    // }
                }
                else
                {
                    matchingWindow.Config = config;
                }
            }

            // Close Windows without configurations
            foreach (var w in GetAllWindows())
            {
                var hasConfig = configurations.Any(config => config.Title == w.Config.Title);
                if (!hasConfig)
                {
                    w.Config.Visible = false;
                }
            }

            ApplyLayout();
        }

        private static string GetLayoutFilename(int index)
        {
            return Path.Combine(ConfigFolderName, string.Format(LayoutFileNameFormat, index));
        }

        private static bool DoesLayoutExists(int index)
        {
            return File.Exists(GetLayoutFilename(index));
        }

        private void ApplyLayout()
        {
            foreach (var window in GetAllWindows())
            {
                window.ApplySizeAndPosition();
            }
        }

        public static Vector2 GetRelativePositionFromPixel(Vector2 pixel)
        {
            return pixel / _appWindowSize;
        }

        public static Vector2 GetPixelPositionFromRelative(Vector2 fraction)
        {
            return fraction * _appWindowSize;
        }

        private static Vector2 _appWindowSize;

        private IEnumerable<Window> GetAllWindows()
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

        private void UpdateAppWindowSize()
        {
            var newSize = ImGui.GetIO().DisplaySize;
            if (newSize == _appWindowSize)
                return;

            var allWindowConfigs = GetAllWindows().Select(window => window.Config).ToList();
            _appWindowSize = newSize;
            if (newSize == Vector2.Zero)
                return;
            
            ApplyConfigurations(allWindowConfigs);
        }

        public bool IsAnyInstanceVisible<T>() where T : Window
        {
            return GetAllWindows().OfType<T>().Any(w => w.Config.Visible);
        }

        public void ToggleWindowTypeVisibility<T>() where T : Window
        {
            var instances = GetAllWindows().OfType<T>().ToList();
            if (instances.Count != 1)
                return;

            instances[0].Config.Visible = !instances[0].Config.Visible;
        }
        
        private const string LayoutFileNameFormat = "layout{0}.json";
        private const string ConfigFolderName = ".t3";
        private const float MainMenuBarHeight = 25;

        private readonly List<Window> _windows;
        private bool _demoWindowVisible;
        private bool _metricsWindowVisible;
        public static bool ShowSecondaryRenderWindow { get; private set; }
        public static bool IsWindowMinimized => _appWindowSize == Vector2.Zero;

        private bool _hasBeenInitialized;
    }
}