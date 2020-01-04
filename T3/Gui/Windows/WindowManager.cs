using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using SharpDX.WIC;
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
                           new SymbolTree(),
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
            for (var i = 0; i < _saveLayoutActions.Length; i++)
            {
                if (KeyboardBinding.Triggered(_saveLayoutActions[i]))
                    SaveLayout(i);

                if (KeyboardBinding.Triggered(_loadLayoutActions[i]))
                    LoadLayout(i);
            }

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

                if (ImGui.BeginMenu("Load layout"))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (ImGui.MenuItem("Layout " + (i + 1), "F" + (i + 1), false, enabled: DoesLayoutExists(i)))
                        {
                            LoadLayout(i);
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

                ImGui.EndMenu();
            }
        }

        private void SaveLayout(int index)
        {
            var allWindowConfigs = GetAllWindows().Select(window => window.Config).ToList();

            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            using (var file = File.CreateText(GetLayoutFilename(index)))
            {
                serializer.Serialize(file, allWindowConfigs);
            }
        }

        private void LoadLayout(int index)
        {
            var filename = GetLayoutFilename(index);
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
                if (matchingWindow == null)
                {
                    if (config.Title.StartsWith("Graph#"))
                    {
                        matchingWindow = new GraphWindow();
                        matchingWindow.Config = config;
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
                    else
                    {
                        Log.Error($"Can't find type of window '{config.Title}'");
                    }
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
                ImGui.SetWindowPos(window.Config.Title,  GetPixelPositionFromRelative(window.Config.Position));
                ImGui.SetWindowSize(window.Config.Title, GetPixelPositionFromRelative(window.Config.Size));
            }
        }

        public static Vector2 GetRelativePositionFromPixel(Vector2 pixel)
        {
            var io = ImGui.GetIO();
            return new Vector2(
                               pixel.X / io.DisplaySize.X,
                               pixel.Y / io.DisplaySize.Y
                              );
        }

        public static Vector2 GetPixelPositionFromRelative(Vector2 fraction)
        {
            var io = ImGui.GetIO();
            return new Vector2(
                               fraction.X * io.DisplaySize.X,
                               fraction.Y * io.DisplaySize.Y
                              );
        }

        
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

        //private const string LayoutFilePath = "layout.json";
        private const string LayoutFileNameFormat = "layout{0}.json";
        private const string ConfigFolderName = ".t3";

        private readonly List<Window> _windows;
        private bool _demoWindowVisible;
        private bool _metricsWindowVisible;
        public static bool ShowSecondaryRenderWindow { get; private set; }
    }
}