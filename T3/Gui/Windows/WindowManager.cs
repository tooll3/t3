using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;
using T3.Gui.Graph;

namespace T3.Gui.Windows
{
    public class WindowManager
    {
        public WindowManager()
        {
            _windows = new List<Window>()
                       {
                           new GraphWindow(),
                           new ParameterWindow(),
                           new OutputWindow(),
                           new ConsoleLogWindow(),
                           new SettingsWindow(),
                       };
        }

        public void Draw()
        {
            if(KeyboardBinding.Triggered(UserActions.LoadLayout0))
                LoadLayout(0);

            if(KeyboardBinding.Triggered(UserActions.LoadLayout1))
                LoadLayout(1);

            if(KeyboardBinding.Triggered(UserActions.LoadLayout2))
                LoadLayout(2);

            if(KeyboardBinding.Triggered(UserActions.SaveLayout0))
                SaveLayout(0);

            if(KeyboardBinding.Triggered(UserActions.SaveLayout1))
                SaveLayout(1);

            if(KeyboardBinding.Triggered(UserActions.SaveLayout2))
                SaveLayout(2);

            foreach (var windowType in _windows)
            {
                windowType.Draw();
            }

            if (_demoWindowVisible)
                ImGui.ShowDemoWindow(ref _demoWindowVisible);

            if (_metricsWindowVisible)
                ImGui.ShowMetricsWindow(ref _metricsWindowVisible);
        }

        public void DrawWindowsMenu()
        {
            if (ImGui.BeginMenu("Windows"))
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

                if (ImGui.MenuItem("Load layout", ""))
                    LoadLayout(0);

                ImGui.EndMenu();
            }
        }

        private void SaveLayout(int index)
        {
            var allWindowConfigs = GetAllWindows().Select(window => window.Config).ToList();

            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var writer = new StringWriter();
            serializer.Serialize(writer, allWindowConfigs);

            var file = File.CreateText(string.Format(LayoutFileNameFormat, index));
            file.Write(writer.ToString());
            file.Close();
        }

        private void LoadLayout(int index)
        {
            var filename = string.Format(LayoutFileNameFormat, index);
            if (!File.Exists(filename))
            {
                Log.Warning($"Layout {filename} doesn't exist yet");
                return;
            }
            
            var jsonBlob = File.ReadAllText(filename);
            var serializer = Newtonsoft.Json.JsonSerializer.Create();
            var fileTextReader = new StringReader(jsonBlob);
            if (!(serializer.Deserialize(fileTextReader, typeof(List<Window.WindowConfig>))
                      is List<Window.WindowConfig> configurations))
            {
                Log.Error("Can't load layout");
                return;
            }

            foreach (var config in configurations)
            {
                var matchingWindow = GetAllWindows().FirstOrDefault(window => window.Config.Title == config.Title);
                if (matchingWindow== null)
                {
                    Log.Error($"Can't find window '{config.Title}'");
                }
                else
                {
                    matchingWindow.Config = config;
                }
            }
            ApplyLayout();
        }

        
        private void ApplyLayout()
        {
            foreach (var window in GetAllWindows())
            {
                ImGui.SetWindowPos(window.Config.Title, window.Config.Position);
                ImGui.SetWindowSize(window.Config.Title, window.Config.Size);
            }
        }

        
        private IEnumerable<Window> GetAllWindows()
        {
            foreach (var window in _windows)
            {
                if (window.AllowMultipleInstances)
                {
                    foreach (var windowInstance in window.WindowInstances)
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

        //private const string LayoutFilePath = "layout.json";
        private const string LayoutFileNameFormat= "layout{0}.json";

        private readonly List<Window> _windows;
        private bool _demoWindowVisible;
        private bool _metricsWindowVisible;
        public static bool ShowSecondaryRenderWindow { get; private set; }
    }
}