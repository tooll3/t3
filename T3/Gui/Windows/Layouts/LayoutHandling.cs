using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.Output;

namespace T3.Gui.Windows.Layouts
{
    /// <summary>
    /// Manages visibility and layout of windows including...
    /// - switching between Layouts
    /// - toggling visibility from main menu
    /// - Graph over content mode
    /// </summary>    
    public static class LayoutHandling
    {
        public static void ProcessKeyboardShortcuts()
        {
            // Process Keyboard shortcuts
            for (var i = 0; i < _saveLayoutActions.Length; i++)
            {
                if (KeyboardBinding.Triggered(_saveLayoutActions[i]))
                    SaveLayout(i);

                if (KeyboardBinding.Triggered(_loadLayoutActions[i]))
                    LoadAndApplyLayout(i);
            }
        }

        public static void DrawMainMenuItems()
        {
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

        private static void SaveLayout(int index)
        {
            var allWindowConfigs = WindowManager.GetAllWindows().Select(window => window.Config).ToList();

            if (!Directory.Exists(LayoutPath))
                Directory.CreateDirectory(LayoutPath);

            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;
            using (var file = File.CreateText(GetLayoutFilename(index)))
            {
                serializer.Serialize(file, allWindowConfigs);
            }

            UserSettings.Config.WindowLayoutIndex = index;
        }

        public static void LoadAndApplyLayout(int index)
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
            WindowManager.SetGraphWindowToNormal();

            ApplyConfigurations(configurations);
            UserSettings.Config.WindowLayoutIndex = index;
        }

        public static void ApplyConfigurations(List<Window.WindowConfig> configurations)
        {
            foreach (var config in configurations)
            {
                var matchingWindow = WindowManager.GetAllWindows().FirstOrDefault(window => window.Config.Title == config.Title);
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
            foreach (var w in WindowManager.GetAllWindows())
            {
                var hasConfig = configurations.Any(config => config.Title == w.Config.Title);
                if (!hasConfig)
                {
                    w.Config.Visible = false;
                }
            }

            ApplyLayout();
        }

        public static string GetLayoutFilename(int index)
        {
            return Path.Combine(LayoutPath, string.Format(LayoutFileNameFormat, index));
        }

        private static bool DoesLayoutExists(int index)
        {
            return File.Exists(GetLayoutFilename(index));
        }

        private static void ApplyLayout()
        {
            foreach (var window in WindowManager.GetAllWindows())
            {
                window.ApplySizeAndPosition();
            }
        }

        private static readonly UserActions[] _loadLayoutActions =
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

        private static readonly UserActions[] _saveLayoutActions =
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

        /// <summary>
        /// Defines a layout that can be then serialized to file  
        /// </summary>
        class LayoutDefinition
        {
            public string Title;
            public List<Window.WindowConfig> WindowConfigs;
            public string ImGuiSettings;
            public bool IsGraphOverContent;
        }

        private const string LayoutFileNameFormat = "layout{0}.json";
        public const string LayoutPath = @".t3\layouts\";
    }
}