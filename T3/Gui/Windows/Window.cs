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
        protected bool _visible = false;
        protected bool _allowMultipeInstances = false;
        protected bool _canBeOpenedFromAppMenu = true;
        protected string _title = "Window";
        protected abstract void DrawContent();
        protected ImGuiWindowFlags _windowFlags;

        protected Window() { }

        public void DrawMenuItemToggle()
        {
            if (!_visible && !_canBeOpenedFromAppMenu)
                return;

            if (_allowMultipeInstances)
            {
                if (ImGui.MenuItem("New " + _title))
                {
                    AddAnotherInstance();
                }
            }
            else
            {
                if (ImGui.MenuItem(_title, "", _visible))
                {
                    _visible = !_visible;
                }
                if (!_visible)
                    Close();
            }
        }

        protected virtual void UpdateBeforeDraw() { }

        public virtual void Draw()
        {
            if (_allowMultipeInstances)
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

            if (!_visible)
                return;

            if (ImGui.Begin(_title, ref _visible, _windowFlags))
            {
                // Prevent window header from becoming invisible 
                var windowPos = ImGui.GetWindowPos();
                if (windowPos.X <= 0) windowPos.X = 0;
                if (windowPos.Y <= 0) windowPos.Y = 0;
                ImGui.SetWindowPos(windowPos);
                
                DrawContent();
                ImGui.End();
            }

            if (!_visible)
            {
                Close();
            }
        }

        protected virtual void DrawAllInstances() { }
        protected virtual void Close() { }
        protected virtual void AddAnotherInstance() { }
    }
}
