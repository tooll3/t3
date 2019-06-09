using ImGuiNET;
using System.Numerics;
using T3.Gui;

namespace imHelpers
{


    /// <summary>
    /// A collection of helper and debug function for IMGUI development
    /// </summary>
    class THelpers
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
            var overlayDrawlist = ImGui.GetForegroundDrawList();
            overlayDrawlist.AddRect(screenMin, screenMax, Color.TGreen);
            overlayDrawlist.AddText(new Vector2(screenMin.X, screenMax.Y), Color.TGreen, label);
        }

        public static void DebugRect(Vector2 screenMin, Vector2 screenMax, Color color, string label = "")
        {
            var overlayDrawlist = ImGui.GetForegroundDrawList();
            overlayDrawlist.AddRect(screenMin, screenMax, color);
            overlayDrawlist.AddText(new Vector2(screenMin.X, screenMax.Y), color, label);
        }



        /// <summary>
        /// Draws an outline of the current (last) Imgui item
        /// </summary>
        public static void DebugItemRect(string label = "", uint color = 0xff20ff80)
        {
            if (UiSettingsWindow.ItemRegionsVisible)
                DebugRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), color, label);
        }


        public static void DebugWindowRect(string label = "", uint color = 0xffff2080)
        {
            if (UiSettingsWindow.WindowRegionsVisible)
                DebugRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), color, label);
        }
    }

    /// <summary>
    /// 2D axis aligned bounding-box. It's a port of IMGUIs internal class.
    /// FIXME: this should be replaced with a .net Rect-Class
    /// </summary>
    public struct ImRect
    {
        public Vector2 Min;    // Upper-left
        public Vector2 Max;    // Lower-right

        public ImRect(Vector2 min, Vector2 max)
        {
            Min = min; Max = max;
        }

        public ImRect(Vector4 v)
        {
            Min = new Vector2(v.X, v.Y); Max = new Vector2(v.Z, v.W);
        }

        public ImRect(float x1, float y1, float x2, float y2)
        {
            Min = new Vector2(x1, y1); Max = new Vector2(x2, y2);
        }

        public Vector2 GetCenter()
        {
            return new Vector2((Min.X + Max.X) * 0.5f, (Min.Y + Max.Y) * 0.5f);
        }

        public Vector2 GetSize()
        {
            return new Vector2(Max.X - Min.X, Max.Y - Min.Y);
        }

        public float GetWidth()
        {
            return Max.X - Min.X;
        }

        public float GetHeight()
        {
            return Max.Y - Min.Y;
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

        public bool Contains(Vector2 p)
        {
            return p.X >= Min.X && p.Y >= Min.Y && p.X < Max.X && p.Y < Max.Y;
        }

        public bool Contains(ImRect r)
        {
            return r.Min.X >= Min.X && r.Min.Y >= Min.Y && r.Max.X <= Max.X && r.Max.Y <= Max.Y;
        }

        public bool Overlaps(ImRect r)
        {
            return r.Min.Y < Max.Y && r.Max.Y > Min.Y && r.Min.X < Max.X && r.Max.X > Min.X;
        }

        public void Add(Vector2 p)
        {
            if (Min.X > p.X) Min.X = p.X; if (Min.Y > p.Y) Min.Y = p.Y; if (Max.X < p.X) Max.X = p.X; if (Max.Y < p.Y) Max.Y = p.Y;
        }
        public void Add(ImRect r)
        {
            if (Min.X > r.Min.X) Min.X = r.Min.X; if (Min.Y > r.Min.Y) Min.Y = r.Min.Y; if (Max.X < r.Max.X) Max.X = r.Max.X; if (Max.Y < r.Max.Y) Max.Y = r.Max.Y;
        }
        public void Expand(float amount)
        {
            Min.X -= amount; Min.Y -= amount; Max.X += amount; Max.Y += amount;
        }
        public void Expand(Vector2 amount)
        {
            Min.X -= amount.X; Min.Y -= amount.Y; Max.X += amount.X; Max.Y += amount.Y;
        }
        public void Translate(Vector2 d)
        {
            Min.X += d.X; Min.Y += d.Y; Max.X += d.X; Max.Y += d.Y;
        }
        public void TranslateX(float dx)
        {
            Min.X += dx; Max.X += dx;
        }
        public void TranslateY(float dy)
        {
            Min.Y += dy; Max.Y += dy;
        }

        public static ImRect RectBetweenPoints(Vector2 a, Vector2 b)
        {
            return new ImRect(
                x1: Im.Min(a.X, b.X),
                y1: Im.Min(a.Y, b.Y),
                x2: Im.Max(a.X, b.X),
                y2: Im.Max(a.Y, b.Y));
        }

        public static ImRect RectWithSize(Vector2 position, Vector2 size)
        {
            return new ImRect(position, position + size);
        }


        // Simple version, may lead to an inverted rectangle, which is fine for Contains/Overlaps test but not for display.
        public void ClipWith(ImRect r)
        {
            Min = Im.Max(Min, r.Min); Max = Im.Min(Max, r.Max);
        }

        // Full version, ensure both points are fully clipped.
        public void ClipWithFull(ImRect r)
        {
            Min = Im.Clamp(Min, r.Min, r.Max); Max = Im.Clamp(Max, r.Min, r.Max);
        }
        public void Floor()
        {
            Min.X = (float)(int)Min.X; Min.Y = (float)(int)Min.Y; Max.X = (float)(int)Max.X; Max.Y = (float)(int)Max.Y;
        }
        bool IsInverted() { return Min.X > Max.X || Min.Y > Max.Y; }
    }

    /// <summary>
    /// Manual port of helper-functions defined in imgui_internal.h
    /// </summary>
    static class Im
    {
        public static Vector2 Min(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X < rhs.X ? lhs.X : rhs.X, lhs.Y < rhs.Y ? lhs.Y : rhs.Y);
        }

        public static Vector2 Max(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X >= rhs.X ? lhs.X : rhs.X, lhs.Y >= rhs.Y ? lhs.Y : rhs.Y);
        }

        public static Vector2 Clamp(Vector2 v, Vector2 mn, Vector2 mx)
        {
            return new Vector2((v.X < mn.X) ? mn.X : (v.X > mx.X)
            ? mx.X : v.X, (v.Y < mn.Y) ? mn.Y : (v.Y > mx.Y) ? mx.Y : v.Y);
        }

        public static T Min<T>(T lhs, T rhs) where T : System.IComparable<T>
        {
            return lhs.CompareTo(rhs) < 0 ? lhs : rhs;
        }

        public static T Max<T>(T lhs, T rhs) where T : System.IComparable<T>
        {
            return lhs.CompareTo(rhs) >= 0 ? lhs : rhs;
        }

        public static T Clamp<T>(this T val, T min, T max) where T : System.IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static float Lerp(float a, float b, float t) { return (float)(a + (b - a) * t); }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        {
            return new Vector2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }
        public static double Lerp(double a, double b, double t) { return (double)(a + (b - a) * t); }
        public static int Lerp(int a, int b, float t) { return (int)(a + (b - a) * t); }
        public static void Swap<T>(ref T a, ref T b) { T tmp = a; a = b; b = tmp; }
    }

}
