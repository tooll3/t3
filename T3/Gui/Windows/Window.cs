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

        protected Window() { }

        public void DrawMenuItemToggle()
        {
            if (!_visible && !_canBeOpenedFromAppMenu)
                return;

            if (ImGui.MenuItem(_title, "", _visible))
            {
                _visible = !_visible;
            }
        }

        protected virtual void UpdateBeforeDraw()
        {

        }

        public void Draw()
        {
            UpdateBeforeDraw();

            if (!_visible)
                return;

            if (ImGui.Begin(_title, ref _visible))
            {
                DrawContent();
                ImGui.End();
            }
        }
    }
}
