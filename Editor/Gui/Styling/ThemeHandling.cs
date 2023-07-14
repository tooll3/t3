using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling;

public static class ThemeHandling
{
    /// <summary>
    /// Requires user settings to be loaded already.
    /// </summary>
    public static void Initialize()
    {
        InitializeFactoryDefault();
        LoadThemes();
        ApplyUserConfigTheme();
    }
    
    
    public static void SetThemeAsUserTheme(ColorTheme theme)
    {
        UserSettings.Config.ColorThemeName = theme.Name;
        UserSettings.Save();
        ApplyTheme(theme);

        T3Style.Apply();
    }
    
    public static void ApplyUserConfigTheme()
    {
        var selectedThemeName = UserSettings.Config.ColorThemeName;
        if (string.IsNullOrWhiteSpace(selectedThemeName))
        {
            ApplyTheme(_factoryTheme);
            return;
        }

        var userTheme = Themes.FirstOrDefault(t => t.Name == selectedThemeName);
        if (userTheme == null)
        {
            Log.Warning($"Couldn't load {selectedThemeName}");
            ApplyTheme(_factoryTheme);
            return;
        }
        ApplyTheme(userTheme);
    }
    
    /// <summary>
    /// Applies the colors to T3StyleColors
    /// </summary>
    /// <param name="theme"></param>
    public static void ApplyTheme(ColorTheme theme)
    {
        var fields = typeof(UiColors).GetFields();
        foreach (var f in fields)
        {
            if (f.GetValue(ColorThemeEditor.Dummy) is not Color)
                continue;

            if (!theme.Colors.TryGetValue(f.Name, out var colorValue))
                continue;
            
            f.SetValue(ColorThemeEditor.Dummy,  new Color(colorValue));
        }
        FrameStats.Current.UiColorsNeedUpdate = true;
        T3Style.Apply();
    }

    public static void SaveTheme(ColorTheme theme)
    {
        if (!Directory.Exists(ThemeFolder))
            Directory.CreateDirectory(ThemeFolder);
            
        theme.Name = theme.Name.Trim();
        if (string.IsNullOrEmpty(theme.Name))
        {
            theme.Name = "untitled";
        }

        var combine = GetThemeFilepath(theme);
        var filepath = combine;
        Utilities.SaveJson(theme, filepath);
        LoadThemes();
    }

    private static string GetThemeFilepath(ColorTheme theme)
    {
        return Path.Combine(ThemeFolder, theme.Name + ".json");
    }

    public static void DeleteTheme(ColorTheme theme)
    {
        var filepath = GetThemeFilepath(theme);
        if (!File.Exists(filepath))
        {
            Log.Warning($"{filepath} does not exist?");
            return;
        }
        
        File.Delete(filepath);
        ApplyTheme(_factoryTheme);
        LoadThemes();
        UserSettings.Config.ColorThemeName = null;
    }
    
    public static void LoadThemes()
    {
        Themes.Clear();
        if (!Directory.Exists(ThemeFolder))
            return;
        
        foreach (var x in Directory.GetFiles(ThemeFolder))
        {
            try
            {
                var t = Utilities.TryLoadingJson<ColorTheme>(x);
                Themes.Add(t);
                    
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load {x} : {e.Message}");
            }
        }
    }

    public class ColorTheme
    {
        public string Name = "untitled";
        public string Author = "unknown";
        public Dictionary<string, Vector4> Colors = new();

        public ColorTheme Clone()
        {
            return new ColorTheme()
                       {
                           Name = Name,
                           Author = Author,
                           Colors = Colors.ToDictionary(entry => entry.Key,
                                                        entry => entry.Value),
                       };
        }
    }
    
    public static List<ColorTheme> Themes = new();
    private const string ThemeFolder = @".t3\Themes\";

    private static void InitializeFactoryDefault()
    {
        _factoryTheme = new ThemeHandling.ColorTheme();
        var fields = typeof(UiColors).GetFields();
        foreach (var f in fields)
        {
            if (f.GetValue(ColorThemeEditor.Dummy) is not Color color)
                continue;

            _factoryTheme.Colors[f.Name] = color;
        }
    }

    public static ColorTheme _factoryTheme;


}