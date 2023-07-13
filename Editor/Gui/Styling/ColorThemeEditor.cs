using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;

namespace T3.Editor.Gui.Styling;

/// <summary>
/// User interface switching and adjusting color themes...
/// </summary>
public static class ColorThemeEditor
{
    public static List<ColorTheme> Themes;

    /// <summary>
    /// Default theme
    /// </summary>
    ///
    private static  ColorTheme _defaultTheme;

    private static uint _clickedId;
    private static bool _animatedAllColors;
    private static Color _flashColor = new Color(1, 1, 1, 0f);

    private static void InitializeDefault()
    {
        if (_defaultTheme != null)
            return;

        _defaultTheme = new ColorTheme();
        
        var fields = typeof(UiColors).GetFields();
        foreach (var f in fields)
        {
            if (f.GetValue(CurrentTheme) is not Color color)
                continue;

            _defaultTheme.Colors[f.Name] = color;
        }
        
        T3Style.Apply();
    }
    
    public static void DrawEditor()
    {
        InitializeDefault();
        CurrentTheme ??= _defaultTheme.Clone();
        var fields = typeof(UiColors).GetFields();

        if (FormInputs.AddCheckBox("Animate all colors", ref _animatedAllColors))
        {
            if (!_animatedAllColors)
            {
                CurrentTheme ??= _defaultTheme.Clone();
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

        FormInputs.AddStringInput("Name", ref CurrentTheme.Title);
        FormInputs.AddStringInput("Author", ref CurrentTheme.Author);


        foreach (var f in fields)
        {
            var wasChanged = false;
            ImGui.PushID(f.Name);
            if (f.GetValue(CurrentTheme) is not Color color)
                continue;

            ImGui.AlignTextToFramePadding();
            
            if (!_defaultTheme.Colors.TryGetValue(f.Name, out var defaultColor))
            {
                defaultColor = color;
                wasChanged = true;
            }
            else
            {
                if (color != defaultColor)
                    wasChanged = true;
            }
            
            string hint = null;
            foreach (var ca in f.GetCustomAttributes(true))
            {
                if (ca is not T3Style.HintAttribute hintAttribute)
                    continue;

                hint = hintAttribute.Description;
            }

            var vec4Color = color.Rgba;
            ImGui.PushStyleColor(ImGuiCol.Text, wasChanged ? UiColors.Text.Rgba : UiColors.TextMuted);

            var labelWasClicked = _clickedId == ImGui.GetID(string.Empty);
            if (ColorEditButton.Draw(ref vec4Color, new Vector2(24, 24), labelWasClicked).HasFlag(InputEditStateFlags.Modified))
            {
                SetColor(f, vec4Color);
                _clickedId = 0;
                T3Style.Apply();
            }
            
            ImGui.SameLine(0,10);
            ImGui.Text(Regex.Replace(f.Name, "(\\B[A-Z])", " $1"));
            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _clickedId = ImGui.GetID(string.Empty);
            }
            if (!string.IsNullOrEmpty(hint) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(hint);
            }

            if (wasChanged)
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

    private static void SetColor(FieldInfo f, Vector4 vec4Color)
    {
        f.SetValue(CurrentTheme, new Color(vec4Color));
        CurrentTheme.Colors[f.Name] = vec4Color;
    }

    /// <summary>
    /// Applies the colors to T3StyleColors
    /// </summary>
    /// <param name="theme"></param>
    public static void ApplyTheme(ColorTheme theme)
    {
    }

    public static void SaveTheme(ColorTheme theme, string filepath)
    {
    }

    public static void ScanAndLoadThemes()
    {
    }

    private const string ThemeFolder = @".t3\Themes\";

    public class ColorTheme
    {
        public string Title;
        public string Author;
        public Dictionary<string, Vector4> Colors = new();

        public ColorTheme Clone()
        {
            return new ColorTheme()
                       {
                           Title = Title,
                           Author = Author,
                           Colors = Colors.ToDictionary(entry => entry.Key,
                                                        entry => entry.Value),
                       };
        }
    }

    public static ColorTheme CurrentTheme;
}