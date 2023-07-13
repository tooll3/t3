using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Styling;

/// <summary>
/// User interface switching and adjusting color themes...
/// </summary>
public static class ColorThemeEditor
{
    
    private static void InitializeDefault()
    {
        if (_defaultTheme != null)
            return;

        _defaultTheme = new ThemeHandling.ColorTheme();

        var fields = typeof(UiColors).GetFields();
        foreach (var f in fields)
        {
            if (f.GetValue(Dummy) is not Color color)
                continue;

            _defaultTheme.Colors[f.Name] = color;
        }

        T3Style.Apply();
    }


    public static void DrawEditor()
    {
        InitializeDefault();
        _currentTheme ??= _defaultTheme.Clone();
        var fields = typeof(UiColors).GetFields();

        DevHelpers.Draw(fields);

        //bool found;
        if (ImGui.BeginCombo("##SelectTheme", UserSettings.Config.ColorThemeName, ImGuiComboFlags.HeightLarge))
        {
            foreach (var theme in ThemeHandling.Themes)
            {
                var isSelected = theme.Name == UserSettings.Config.ColorThemeName;
                if (!ImGui.Selectable($"{theme.Name}", isSelected, ImGuiSelectableFlags.DontClosePopups))
                    continue;
                
                UserSettings.Config.ColorThemeName = theme.Name;
                UserSettings.Save();
                ThemeHandling.ApplyTheme(theme);
                _currentTheme = theme;
                _defaultTheme = _currentTheme.Clone();
                T3Style.Apply();
            }
            ImGui.EndCombo();
        }

        FormInputs.AddStringInput("Name", ref _currentTheme.Name);
        FormInputs.AddStringInput("Author", ref _currentTheme.Author);

        if (CustomComponents.DisablableButton("Save", _somethingChanged))
        {
            ThemeHandling.SaveTheme(_currentTheme);
            UserSettings.Config.ColorThemeName = _currentTheme.Name;
            var currentFromName = ThemeHandling.Themes.FirstOrDefault(t => t.Name == UserSettings.Config.ColorThemeName);
            if (currentFromName == null)
            {
                Log.Error("Saving theme failed");
                return;
            }
            
            _currentTheme = currentFromName;
            _defaultTheme = currentFromName.Clone();
        }
        
        _somethingChanged = false;
        foreach (var f in fields)
        {
            var isChanged = false;
            ImGui.PushID(f.Name);
            if (f.GetValue(_currentTheme) is not Color color)
                continue;

            ImGui.AlignTextToFramePadding();

            if (!_defaultTheme.Colors.TryGetValue(f.Name, out var defaultColor))
            {
                defaultColor = color;
                isChanged = true;
            }
            else
            {
                if (color != defaultColor)
                    isChanged = true;
            }

            _somethingChanged |= isChanged;

            string hint = null;
            foreach (var ca in f.GetCustomAttributes(true))
            {
                if (ca is not T3Style.HintAttribute hintAttribute)
                    continue;

                hint = hintAttribute.Description;
            }

            var vec4Color = color.Rgba;
            ImGui.PushStyleColor(ImGuiCol.Text, isChanged ? UiColors.Text.Rgba : UiColors.TextMuted);

            if (ColorEditButton.Draw(ref vec4Color, new Vector2(24, 24)).HasFlag(InputEditStateFlags.Modified))
            {
                SetColor(f, vec4Color);
                T3Style.Apply();
            }

            ImGui.SameLine(0, 10);
            ImGui.Text(Regex.Replace(f.Name, "(\\B[A-Z])", " $1"));
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                SetColor(f, defaultColor);
                T3Style.Apply();
            }

            if (!string.IsNullOrEmpty(hint) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(hint);
            }

            if (isChanged)
            {
                ImGui.SameLine();
                if (CustomComponents.IconButton(Icon.Revert, new Vector2(16, 16)))
                {
                    SetColor(f, defaultColor);
                }
            }

            ImGui.PopID();
        }
    }

    private static void SetColor(FieldInfo f, Vector4 vec4Color)
    {
        f.SetValue(Dummy, new Color(vec4Color));
        _currentTheme.Colors[f.Name] = vec4Color;
    }
    

    private static class DevHelpers
    {
        public static void Draw(FieldInfo[] fields)
        {
            // Debug utils for finding unused colors...
            if (FormInputs.AddCheckBox("Animate all colors", ref _animatedAllColors))
            {
                if (!_animatedAllColors)
                {
                    _currentTheme ??= _defaultTheme.Clone();
                    ApplyBlinkToAll(fields, 0);
                }
            }

            var flashValues = _flashColor.Rgba;
            if (ColorEditButton.Draw(ref flashValues, new Vector2(16, 16)).HasFlag(InputEditStateFlags.Modified))
            {
                _flashColor.Rgba = flashValues;
                ApplyBlinkToAll(fields, flashValues.W);
            }

            if (_animatedAllColors)
            {
                var blinkAmount = MathF.Sin((float)ImGui.GetTime() * 10f) * 0.25f + 0.25f;
                ApplyBlinkToAll(fields, blinkAmount);
            }
        }

        private static void ApplyBlinkToAll(FieldInfo[] fields, float blinkAmount)
        {
            foreach (var f in fields)
            {
                if (!_defaultTheme.Colors.TryGetValue(f.Name, out var defaultColorValues))
                    continue;

                var blinkingColor = Color.Mix(new Color(defaultColorValues),
                                              _flashColor, blinkAmount);
                SetColor(f, blinkingColor);
            }

            T3Style.Apply();
        }

        private static bool _animatedAllColors;
        private static Color _flashColor = new Color(1, 1, 1, 0f);
    }
    
    
    private static bool _somethingChanged;
    private static ThemeHandling.ColorTheme _currentTheme;
    private static ThemeHandling.ColorTheme _defaultTheme;
    public static readonly object Dummy = new();

}


public static class ThemeHandling
{
    
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
        
        var filepath = Path.Combine(ThemeFolder, theme.Name + ".json");
        Utilities.SaveJson(theme, filepath);
        ScanAndLoadThemes();
    }

    public static void ScanAndLoadThemes()
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
    
    public static List<ThemeHandling.ColorTheme> Themes = new();
    private const string ThemeFolder = @".t3\Themes\";
}