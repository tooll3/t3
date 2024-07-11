using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows
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

        public abstract List<Window> GetInstances();

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
            
            if (!_wasVisible)
            {
                ImGui.SetNextWindowSize(new Vector2(550,450));
                _wasVisible = true;
            }
            
            var hideFrameBorder = (WindowFlags & ImGuiWindowFlags.NoMove) != ImGuiWindowFlags.None;
            if(hideFrameBorder)
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);

            if (ImGui.Begin(Config.Title, ref Config.Visible, WindowFlags))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, T3Style.WindowPaddingForWindows);
                
                // Prevent window header from becoming invisible 
                var windowPos = ImGui.GetWindowPos();
                if (windowPos.X <= 0) windowPos.X = 0;
                if (windowPos.Y <= 0) windowPos.Y = 0;
                ImGui.SetWindowPos(windowPos);
                
                var preventMouseScrolling = T3Ui.MouseWheelFieldWasHoveredLastFrame ? ImGuiWindowFlags.NoScrollWithMouse : ImGuiWindowFlags.None;
                if (PreventWindowDragging)
                    ImGui.BeginChild("inner", 
                                     ImGui.GetWindowContentRegionMax()- ImGui.GetWindowContentRegionMin(), 
                                     true, 
                                     ImGuiWindowFlags.NoMove| preventMouseScrolling | WindowFlags  );
                

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5,5));
                
                var idBefore = ImGui.GetID("");
                DrawContent();
                var idAfter = ImGui.GetID("");
                
                ImGui.PopStyleVar();
                
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


        public class WindowConfig
        {
            public string Title;
            public bool Visible;
        }

        public WindowConfig Config = new();

        protected string MenuTitle;
        private bool _wasVisible;
    }
}