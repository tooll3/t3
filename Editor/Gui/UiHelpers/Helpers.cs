using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.UiHelpers;

/// <summary>
/// A collection of helper and debug function for IMGUI development
/// </summary>
public static class THelpers
{
    public static ImDrawListPtr OutlinedRect(ref ImDrawListPtr drawList, Vector2 position, Vector2 size, uint fill, uint outline, float cornerRadius = 4)
    {
        drawList.AddRectFilled(position, position + size, fill, cornerRadius);
        drawList.AddRect(position, position + size, outline, cornerRadius);
        return drawList;
    }

    public static ImDrawListPtr OutlinedRect(ref ImDrawListPtr drawList, Vector2 position, Vector2 size, Color fill, Color outline, float cornerRadius = 4)
    {
        drawList.AddRectFilled(position, position + size, fill, cornerRadius);
        drawList.AddRect(position, position + size, outline, cornerRadius);
        return drawList;
    }

    /// <summary>
    /// Draws an overlay rectangle in screen space
    /// </summary>
    public static void DebugRect(Vector2 screenMin, Vector2 screenMax, string label = "")
    {
        if (string.IsNullOrEmpty(label))
            return;
            
        var overlayDrawlist = ImGui.GetForegroundDrawList();
        overlayDrawlist.AddRect(screenMin, screenMax, Color.Green);
        overlayDrawlist.AddText(new Vector2(screenMin.X, screenMax.Y), Color.Green, label);
    }

    public static void DebugRect(Vector2 screenMin, Vector2 screenMax, Color color, string label = "")
    {
        var overlayDrawlist = ImGui.GetForegroundDrawList();
        overlayDrawlist.AddRect(screenMin, screenMax, color);
        if (string.IsNullOrEmpty(label))
            return;
            
        overlayDrawlist.AddText(new Vector2(screenMin.X, screenMax.Y), color, label);
    }

    /// <summary>
    /// Draws an outline of the current (last) Imgui item
    /// </summary>
    public static void DebugItemRect(string label = "", uint color = 0xff20ff80)
    {
        if (T3Ui.ItemRegionsVisible)
            DebugRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), new Color(color), label);
    }
        

    public static ImRect GetContentRegionArea()
    {
        return new ImRect(ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos(),
                          ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos());
    }
        
        
        
    // This has to be called on open
    public static void DisableImGuiKeyboardNavigation()
    {
        // Keep navigation setting to restore after window gets closed
        _keepNavEnableKeyboard = (ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.NavEnableKeyboard) != ImGuiConfigFlags.None;
        ImGui.GetIO().ConfigFlags &= ~ImGuiConfigFlags.NavEnableKeyboard;
    }

    public static void RestoreImGuiKeyboardNavigation()
    {
        if (_keepNavEnableKeyboard)
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
    }
    private static bool _keepNavEnableKeyboard;

    public static string GetReadableRelativeTime(DateTime? timeOfLastBackup)
    {
        if (timeOfLastBackup == null)
            return "Unknown time";

        var timeSinceLastBack = DateTime.Now - timeOfLastBackup;
        var minutes = timeSinceLastBack.Value.TotalMinutes;
        if (minutes < 120)
        {
            return $"{minutes:0} minutes ago";
        }

        var hours = timeSinceLastBack.Value.TotalHours;
        if (hours < 30)
        {
            return $"{hours:0.0} hours ago";
        }

        var days = timeSinceLastBack.Value.TotalDays;
        return $"{days:0.0} days ago";
    }

    public static Color RandomColorForHash(int channelHash)
    {
        var foreGroundBrightness = UiColors.ForegroundFull.V;
        var randomHue = (Math.Abs(channelHash) % 357) / 360f;
        var randomSaturation = (channelHash % 13) / 13f / 3f + 0.4f;
        var randomChannelColor = Color.FromHSV(randomHue, randomSaturation, foreGroundBrightness, 1);
        return randomChannelColor;
    }

    /// <summary>
    /// Ensures the string being modified starts with the given string
    /// </summary>
    /// <param name="start">The desired start of the string</param>
    /// <param name="valueEnforced">the string being modified</param>
    /// <param name="replaceStart">If true, the start of the string will be replaced. If false, the start will be prepended</param>
    /// <returns>True if the string was modified by this method</returns>
    public static bool EnforceStringStart(string start, ref string valueEnforced, bool replaceStart)
    {
        if (valueEnforced.StartsWith(start))
            return false;

        if (replaceStart)
        {
            valueEnforced = valueEnforced.Length <= start.Length
                                ? start
                                : start + valueEnforced[start.Length..];
        }
        else
        {
            valueEnforced = start + valueEnforced;
        }

        return true;
    }
}

/// <summary>
/// 2D axis aligned bounding-box. It's a port of IMGUIs internal class.
/// FIXME: this should be replaced with a .net Rect-Class
/// </summary>
public struct ImRect
{
    public Vector2 Min; // Upper-left
    public Vector2 Max; // Lower-right

    public ImRect(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }

    public ImRect(Vector4 v)
    {
        Min = new Vector2(v.X, v.Y);
        Max = new Vector2(v.Z, v.W);
    }

    public ImRect(float x1, float y1, float x2, float y2)
    {
        Min = new Vector2(x1, y1);
        Max = new Vector2(x2, y2);
    }

    public Vector2 GetCenter()
    {
        return new Vector2((Min.X + Max.X) * 0.5f, (Min.Y + Max.Y) * 0.5f);
    }

    public Vector2 GetSize()
    {
        return new Vector2(Max.X - Min.X, Max.Y - Min.Y);
    }

    public readonly float GetWidth()
    {
        return Max.X - Min.X;
    }

    public readonly float GetHeight()
    {
        return Max.Y - Min.Y;
    }

    /// <summary>
    /// This is required before using <see cref="Contains(Vector2)"/>
    /// </summary>
    public ImRect MakePositive()
    {
        if (Min.X > Max.X)
        {
            var t = Min.X;
            Min.X = Max.X;
            Max.X = t;
        }

        if (Min.Y > Max.Y)
        {
            var t = Min.Y;
            Min.Y = Max.Y;
            Max.Y = t;
        }

        return this;
    }

    /// <summary>
    /// Top-left
    /// </summary>
    public Vector2 GetTL()
    {
        return Min;
    }

    /// <summary>
    /// Top right
    /// </summary>
    public Vector2 GetTR()
    {
        return new Vector2(Max.X, Min.Y);
    }

    /// <summary>
    /// Bottom left
    /// </summary>
    public Vector2 GetBL()
    {
        return new Vector2(Min.X, Max.Y);
    }

    /// <summary>
    /// Bottom right
    /// </summary>
    public Vector2 GetBR()
    {
        return Max;
    }

    /// <summary>
    /// This is required before using <see cref="Contains(Vector2)"/>
    /// </summary>
    /// <remarks>Please make sure to make the rectangle positive before testing</remarks>
    public bool Contains(Vector2 p)
    {
        return p.X >= Min.X && p.Y >= Min.Y && p.X < Max.X && p.Y < Max.Y;
    }

    public bool Contains(in ImRect r)
    {
        return r.Min.X >= Min.X && r.Min.Y >= Min.Y && r.Max.X <= Max.X && r.Max.Y <= Max.Y;
    }

    public bool Overlaps(in ImRect r)
    {
        return r.Min.Y < Max.Y && r.Max.Y > Min.Y && r.Min.X < Max.X && r.Max.X > Min.X;
    }

    public void Add(Vector2 p)
    {
        if (Min.X > p.X) Min.X = p.X;
        if (Min.Y > p.Y) Min.Y = p.Y;
        if (Max.X < p.X) Max.X = p.X;
        if (Max.Y < p.Y) Max.Y = p.Y;
    }

    public void Add(ImRect r)
    {
        if (Min.X > r.Min.X) Min.X = r.Min.X;
        if (Min.Y > r.Min.Y) Min.Y = r.Min.Y;
        if (Max.X < r.Max.X) Max.X = r.Max.X;
        if (Max.Y < r.Max.Y) Max.Y = r.Max.Y;
    }

    public void Expand(float amount)
    {
        Min.X -= amount;
        Min.Y -= amount;
        Max.X += amount;
        Max.Y += amount;
    }

    public void Expand(Vector2 amount)
    {
        Min.X -= amount.X;
        Min.Y -= amount.Y;
        Max.X += amount.X;
        Max.Y += amount.Y;
    }

    public void Translate(Vector2 d)
    {
        Min.X += d.X;
        Min.Y += d.Y;
        Max.X += d.X;
        Max.Y += d.Y;
    }

    public void TranslateX(float dx)
    {
        Min.X += dx;
        Max.X += dx;
    }

    public void TranslateY(float dy)
    {
        Min.Y += dy;
        Max.Y += dy;
    }

    public static ImRect RectBetweenPoints(Vector2 a, Vector2 b)
    {
        return new ImRect(
                          x1: MathUtils.Min(a.X, b.X),
                          y1: MathUtils.Min(a.Y, b.Y),
                          x2: MathUtils.Max(a.X, b.X),
                          y2: MathUtils.Max(a.Y, b.Y));
    }

    public static ImRect RectWithSize(Vector2 position, Vector2 size)
    {
        return new ImRect(position, position + size);
    }

    // Simple version, may lead to an inverted rectangle, which is fine for Contains/Overlaps test but not for display.
    public void ClipWith(ImRect r)
    {
        Min = MathUtils.Max(Min, r.Min);
        Max = MathUtils.Min(Max, r.Max);
    }

    // Full version, ensure both points are fully clipped.
    public void ClipWithFull(ImRect r)
    {
        Min = MathUtils.Clamp(Min, r.Min, r.Max);
        Max = MathUtils.Clamp(Max, r.Min, r.Max);
    }

    public void Floor()
    {
        var size = Max - Min;
        Min.X = (int)Min.X;
        Min.Y = (int)Min.Y;
        Max.X = Min.X + (int)size.X;
        Max.Y = Min.Y + (int)size.Y;
    }

    bool IsInverted()
    {
        return Min.X > Max.X || Min.Y > Max.Y;
    }

    public override string ToString()
    {
        return $"Rect {Min}  {Max}";
    }

    public float GetAspect()
    {
        return GetWidth() / GetHeight();
    }
}