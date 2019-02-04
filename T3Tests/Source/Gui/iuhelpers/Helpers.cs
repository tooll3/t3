using System.Numerics;
using ImGuiNET;
using t3.graph;

namespace imHelpers
{
    static class TColors
    {
        public readonly static Vector4 White = new Vector4(1, 1, 1, 1);
        public readonly static Vector4 Black = new Vector4(0, 0, 0, 1);
        public static uint ToUint(float r, float g, float b, float a = 1) { return ImGui.GetColorU32(new Vector4(r, g, b, a)); }
        public static uint ToUint(int r, int g, int b, int a = 255) { var sc = 1 / 255f; return ImGui.GetColorU32(new Vector4(r * sc, g * sc, b * sc, a * sc)); }
    }

    class THelpers
    {
        public static ImDrawListPtr OutlinedRect(ref ImDrawListPtr drawList, Vector2 position, Vector2 size, uint background, uint outline, float cornerRadius = 4)
        {
            drawList.AddRectFilled(position, position + size, background, cornerRadius);
            drawList.AddRect(position, position + size, outline, cornerRadius);
            return drawList;
        }


        public static void DebugRect(Vector2 min, Vector2 max, string label = "", uint color = 0x88ffff80)
        {
            return;
            var overlayDrawlist = ImGui.GetOverlayDrawList();
            overlayDrawlist.AddRect(min, max, color);
            overlayDrawlist.AddText(new Vector2(min.X, max.Y), color, label);
        }

        public static void DebugItemRect(string label = "item", uint color = 0xff20ff80)
        {
            // DebugRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), label, color);
        }

        public static void DebugWindowRect(string label = "window", uint color = 0xffff2080)
        {
            // DebugRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), label, color);
        }

        // public static void DebugWindowRect(string label = "window", uint color = 0xffff2080)
        // {
        //     DebugRect(ImGui.GetFrameHeight(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), label, color);
        // }

        /*
                  public static Vector2 HandleDrag(Rect clipRect, int objectId, int button = 0, Action<Vector2> OnDragStart = null, Action<Vector2, Vector2> OnDrag = null, Action<Vector2> OnRelease = null)
                  {
                      var delta = Vector2.zero;
                      var e = Event.current;
                      if (e.button != button)
                          return delta;

                      switch (e.type)
                      {
                          case EventType.MouseDown:
                              if (clipRect.Contains(e.mousePosition))
                              {
                                  OnDragStart?.Invoke(e.mousePosition);
                                  _draggedObjecId = objectId;
                                  _dragStartPosition = e.mousePosition;
                                  e.Use();
                              }

                              break;

                          case EventType.MouseDrag:
                              if (_draggedObjecId == objectId)
                              {
                                  delta = e.delta;
                                  OnDrag?.Invoke(e.mousePosition - _dragStartPosition, e.delta);
                                  e.Use();
                              }

                              break;

                          case EventType.MouseUp:
                              if (_draggedObjecId == objectId)
                              {
                                  OnRelease?.Invoke(e.mousePosition - _dragStartPosition);

                                  _draggedObjecId = 0;
                                  e.Use();
                              }
                              break;
                      }

                      return delta;
                  }

                  private static int _draggedObjecId = 0;
                   */
    }

    /// <summary>
    ///2D axis aligned bounding-box
    /// </summary>
    struct ImRect
    {
        public Vector2 Min;    // Upper-left
        public Vector2 Max;    // Lower-right

        //ImRect() : Min(FLT_MAX, FLT_MAX), Max(-FLT_MAX,-FLT_MAX) { }
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
        public static double Lerp(double a, double b, double t) { return (double)(a + (b - a) * t); }
        public static int Lerp(int a, int b, float t) { return (int)(a + (b - a) * t); }
        public static void Swap<T>(ref T a, ref T b) { T tmp = a; a = b; b = tmp; }
    }
}
