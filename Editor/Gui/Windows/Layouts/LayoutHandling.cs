using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Output;

#nullable enable

namespace T3.Editor.Gui.Windows.Layouts
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
                    LoadAndApplyLayoutOrFocusMode(i);
            }
        }

        public static void DrawMainMenuItems()
        {
            if (ImGui.BeginMenu("Load layout"))
            {
                for (int i = 0; i < 10; i++)
                {
                    if (ImGui.MenuItem("Layout " + (i + 1), "F" + (i + 1), false, enabled: DoesLayoutExists(i)))
                    {
                        LoadAndApplyLayoutOrFocusMode(i);
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

            if (ImGui.MenuItem("Save current layout", ""))
                SaveLayout(0);
        }

        public static void UpdateAfterResize(Vector2 newSize)
        {
            if (newSize == Vector2.Zero)
                return;

            ApplyLayout(new Layout
                            {
                                WindowConfigs = WindowManager
                                               .GetAllWindows()
                                               .Select(window => window.Config)
                                               .Where(config => config != null)
                                               .ToList()
                            });
        }

        private static void ApplyLayout(Layout layout)
        {
            // First update windows settings
            foreach (var config in layout.WindowConfigs)
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
                }
                else
                {
                    matchingWindow.Config = config;
                }
            }

            // Close Windows without configurations
            foreach (var w in WindowManager.GetAllWindows())
            {
                var hasConfig = layout.WindowConfigs.Any(config => config.Title == w.Config.Title);
                if (!hasConfig)
                {
                    w.Config.Visible = false;
                }
            }

            // apply ImGui settings
            if (!string.IsNullOrEmpty(layout.ImGuiSettings))
            {
                Program.RequestImGuiLayoutUpdate = layout.ImGuiSettings;
            }
            //ImGui.LoadIniSettingsFromMemory(layout.ImGuiSettings);

            // Than apply size and positions
            // foreach (var window1 in WindowManager.GetAllWindows())
            // {
            //     window1.ApplySizeAndPosition();
            // }
        }

        private static void SaveLayout(int index)
        {
            if (!Directory.Exists(LayoutPath))
                Directory.CreateDirectory(LayoutPath);

            var serializer = JsonSerializer.Create();
            serializer.Formatting = Formatting.Indented;

            using var file = File.CreateText(GetLayoutFilename(index));
            var layout = new Layout
                             {
                                 WindowConfigs = WindowManager.GetAllWindows().Select(window => window.Config).ToList(),
                                 ImGuiSettings = ImGui.SaveIniSettingsToMemory(),
                             };

            serializer.Serialize(file, layout);
            UserSettings.Config.WindowLayoutIndex = index;
        }

        public static void LoadAndApplyLayoutOrFocusMode(int index)
        {
            var isFocusMode = index == 11;

            var filename = GetLayoutFilename(index);
            if (!File.Exists(filename))
            {
                Log.Warning($"Layout {filename} doesn't exist yet");
                return;
            }

            var jsonBlob = File.ReadAllText(filename);
            var serializer = JsonSerializer.Create();
            var fileTextReader = new StringReader(jsonBlob);
            if (!(serializer.Deserialize(fileTextReader, typeof(Layout))
                      is Layout layout))
            {
                Log.Error("Can't load layout");
                return;
            }

            WindowManager.SetGraphWindowToNormal();

            ApplyLayout(layout);
            if (!isFocusMode)
            {
                UserSettings.Config.WindowLayoutIndex = index;
            }

            UserSettings.Config.FocusMode = isFocusMode;
        }

        private static string GetLayoutFilename(int index)
        {
            return Path.Combine(LayoutPath, string.Format(LayoutFileNameFormat, index));
        }

        private static bool DoesLayoutExists(int index)
        {
            return File.Exists(GetLayoutFilename(index));
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
        private class Layout
        {
            public List<Window.WindowConfig> WindowConfigs = new();
            public string? ImGuiSettings;
        }

        private const string LayoutFileNameFormat = "layout{0}.json";
        public const string LayoutPath = @".t3\layouts\";
    }
}