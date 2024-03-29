using ImGuiNET;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows
{
    /// <summary>
    /// A base class that unifies how windows are rendered and persisted
    /// </summary>
    public abstract class Window
    {
        public bool AllowMultipleInstances = false;
        public ImGuiWindowFlags WindowFlags;

        protected bool PreventWindowDragging = true;

        public abstract IReadOnlyList<Window> GetInstances();

        public void Draw()
        {
            UpdateBeforeDraw();

            if (!Config.Visible)
                return;

            if (!_wasVisible)
            {
                ImGui.SetNextWindowSize(new Vector2(550, 450));
                _wasVisible = true;
            }

            var hideFrameBorder = (WindowFlags & ImGuiWindowFlags.NoMove) != ImGuiWindowFlags.None;
            if (hideFrameBorder)
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            if (ImGui.Begin(Config.Title, ref Config.Visible, WindowFlags))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, T3Style.WindowChildPadding);
                // Prevent window header from becoming invisible 
                var windowPos = ImGui.GetWindowPos();
                if (windowPos.X <= 0) windowPos.X = 0;
                if (windowPos.Y <= 0) windowPos.Y = 0;
                ImGui.SetWindowPos(windowPos);

                var preventMouseScrolling = T3Ui.MouseWheelFieldWasHoveredLastFrame ? ImGuiWindowFlags.NoScrollWithMouse : ImGuiWindowFlags.None;
                if (PreventWindowDragging)
                    ImGui.BeginChild("inner", ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin(), false,
                                     ImGuiWindowFlags.NoMove | preventMouseScrolling | WindowFlags);

                var idBefore = ImGui.GetID("");
                DrawContent();
                var idAfter = ImGui.GetID("");
                if (idBefore != idAfter)
                {
                    Log.Warning($"Inconsistent ImGui-ID after rendering {this}  {idBefore} != {idAfter}");
                }

                if (PreventWindowDragging)
                    ImGui.EndChild();

                ImGui.PopStyleVar(); // innerWindowPadding
                ImGui.End();
            }

            if (!Config.Visible)
            {
                Close();
            }

            if (hideFrameBorder)
                ImGui.PopStyleVar();
        }

        private bool _wasVisible;

        public void DrawMenuItemToggle()
        {
            if (AllowMultipleInstances)
            {
                var menuTitle = string.IsNullOrEmpty(MenuTitle) 
                                    ? $"Open new {Config.Title} Window"
                                    : MenuTitle;
                if (ImGui.MenuItem(menuTitle))
                {
                    AddAnotherInstance();
                }
            }
            else
            {
                var menuTitle = string.IsNullOrEmpty(MenuTitle) 
                                    ? Config.Title 
                                    : MenuTitle;

                if (ImGui.MenuItem(menuTitle, "", Config.Visible))
                {
                    Config.Visible = !Config.Visible;
                }

                if (!Config.Visible)
                    Close();
            }
        }
        
        protected abstract void DrawContent();

        protected virtual void UpdateBeforeDraw()
        {
        }
       
        protected virtual void Close()
        {
        }

        protected virtual void AddAnotherInstance()
        {
        }

        public class WindowConfig
        {
            public string Title;
            public bool Visible;
        }

        internal WindowConfig Config = new();

        protected string MenuTitle;
    }
}