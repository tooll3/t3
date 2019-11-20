using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Gui.Windows
{
    /// <summary>
    /// A base class that unifies how windows are rendered and persisted
    /// </summary>
    public abstract class Window
    {
        protected bool Visible;
        protected bool AllowMultipleInstances = false;
        protected string Title = "Window";
        protected abstract void DrawContent();
        protected ImGuiWindowFlags WindowFlags;

        public void DrawMenuItemToggle()
        {
            if (AllowMultipleInstances)
            {
                if (ImGui.MenuItem("New " + Title))
                {
                    AddAnotherInstance();
                }
            }
            else
            {
                if (ImGui.MenuItem(Title, "", Visible))
                {
                    Visible = !Visible;
                }

                if (!Visible)
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

        protected void DrawOneInstance()
        {
            UpdateBeforeDraw();

            if (!Visible)
                return;

            if (ImGui.Begin(Title, ref Visible, WindowFlags))
            {
                // Prevent window header from becoming invisible 
                var windowPos = ImGui.GetWindowPos();
                if (windowPos.X <= 0) windowPos.X = 0;
                if (windowPos.Y <= 0) windowPos.Y = 0;
                ImGui.SetWindowPos(windowPos);

                DrawContent();
                ImGui.End();
            }

            if (!Visible)
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
    }
}