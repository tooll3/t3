using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;

namespace T3.Gui.Windows
{
    /// <summary>
    /// A base class that unifies how windows are rendered and persisted
    /// </summary>
    public abstract class Window
    {
        public bool AllowMultipleInstances = false;
        protected abstract void DrawContent();
        protected ImGuiWindowFlags WindowFlags;
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
            UpdateBeforeDraw();

            if (!Config.Visible)
                return;

            if (ImGui.Begin(Config.Title, ref Config.Visible, WindowFlags))
            {
                StoreWindowLayout();

                // Prevent window header from becoming invisible 
                var windowPos = ImGui.GetWindowPos();
                if (windowPos.X <= 0) windowPos.X = 0;
                if (windowPos.Y <= 0) windowPos.Y = 0;
                ImGui.SetWindowPos(windowPos);

                DrawContent();
                ImGui.End();
            }

            if (!Config.Visible)
            {
                Close();
            }
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
            Config.Position = ImGui.GetWindowPos();
            Config.Size = ImGui.GetWindowSize();
        }

        public class WindowConfig
        {
            public string Title;
            public bool Visible;
            public Vector2 Position;
            public Vector2 Size;
        }

        public WindowConfig Config = new WindowConfig();
    }
}