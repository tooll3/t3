using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Windows
{
    /// <summary>
    /// A base class that unifies how windows are rendered and persisted
    /// </summary>
    public abstract class Window
    {
        public bool AllowMultipleInstances = false;
        protected abstract void DrawContent();
        public ImGuiWindowFlags WindowFlags;

        protected bool PreventWindowDragging = true;
        //public static List<Window> WindowInstances = new List<Window>();

        public abstract List<Window> GetInstances();

        public void DrawMenuItemToggle()
        {
            if (AllowMultipleInstances)
            {
                if (ImGui.MenuItem("New " + Config.Title))
                {
                    AddAnotherInstance();
                }
            }
            else
            {
                if (ImGui.MenuItem(Config.Title, "", Config.Visible))
                {
                    Config.Visible = !Config.Visible;
                }

                if (!Config.Visible)
                    Close();
            }
        }

        protected virtual void UpdateBeforeDraw()
        {
        }

        public void Draw()
        {
            if (AllowMultipleInstances)
            {
                DrawAllInstances();
            }
            else
            {
                DrawOneInstance();
            }
        }

        public void DrawOneInstance()
        {
            if (Config.Size == Vector2.Zero)
            {
                Config.Size = WindowConfig.DefaultSize;
                ApplySizeAndPosition();
            }
            UpdateBeforeDraw();

            if (!Config.Visible)
                return;

            if (!_wasVisibled)
            {
                ApplySizeAndPosition();
                var size = WindowManager.GetPixelPositionFromRelative(Config.Size);
                ImGui.SetNextWindowSize(size);
                _wasVisibled = true;
            }
            
            var hideFrameBorder = (WindowFlags & ImGuiWindowFlags.NoMove) != ImGuiWindowFlags.None;
            if(hideFrameBorder)
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            
            if (ImGui.Begin(Config.Title, ref Config.Visible, WindowFlags))
            {
                StoreWindowLayout();

                // Prevent window header from becoming invisible 
                var windowPos = ImGui.GetWindowPos();
                if (windowPos.X <= 0) windowPos.X = 0;
                if (windowPos.Y <= 0) windowPos.Y = 0;
                ImGui.SetWindowPos(windowPos);

                var preventMouseScrolling = T3Ui.MouseWheelFieldWasHoveredLastFrame ? ImGuiWindowFlags.NoScrollWithMouse : ImGuiWindowFlags.None;
                if (PreventWindowDragging)
                    ImGui.BeginChild("inner", ImGui.GetWindowContentRegionMax()- ImGui.GetWindowContentRegionMin(), false, ImGuiWindowFlags.NoMove| preventMouseScrolling | WindowFlags);

                DrawContent();

                if (PreventWindowDragging)
                    ImGui.EndChild();
                
                ImGui.End();
            }

            if (!Config.Visible)
            {
                Close();
            }
            
            if(hideFrameBorder)
                ImGui.PopStyleVar();
        }

        protected virtual void DrawAllInstances()
        {
        }

        protected virtual void Close()
        {
        }

        protected virtual void AddAnotherInstance()
        {
        }

        private void StoreWindowLayout()
        {
            if (WindowManager.IsWindowMinimized)
                return;
            
            Config.Position = WindowManager.GetRelativePositionFromPixel(ImGui.GetWindowPos());
            Config.Size = WindowManager.GetRelativePositionFromPixel(ImGui.GetWindowSize());
        }

        public class WindowConfig
        {
            public string Title;
            public bool Visible;
            public Vector2 Position = DefaultPosition;
            public Vector2 Size = DefaultSize;
            
            public static Vector2 DefaultSize = new Vector2(0.3f,0.2f);
            public static Vector2 DefaultPosition = new Vector2(0.2f,0.2f);
        }

        public WindowConfig Config = new WindowConfig();

        public void ApplySizeAndPosition()
        {
            ImGui.SetWindowPos(Config.Title, WindowManager.GetPixelPositionFromRelative(Config.Position));

            if (Config.Size == Vector2.Zero)
                Config.Size = WindowConfig.DefaultSize;
            
            ImGui.SetWindowSize(Config.Title, WindowManager.GetPixelPositionFromRelative(Config.Size));
        }

        private bool _wasVisibled;
    }
}