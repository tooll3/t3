using System;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.UiHelpers
{
    public class ModalDialog
    {
        public ModalDialog(string title = "Dialog")
        {
            _title = title;
        }

        private string _title;
        
        
        public void Draw(Action drawContent)
        {
            if (_shouldShowNextFrame)
            {
                _shouldShowNextFrame = false;
                ImGui.OpenPopup(_title);
            }

            ImGui.SetNextWindowSize(new Vector2(500, 200), ImGuiCond.FirstUseEver);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20,20));
            if (ImGui.BeginPopupModal(_title))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4,10));
                drawContent();
                ImGui.PopStyleVar();
                ImGui.EndPopup();
            }
            ImGui.PopStyleVar();
        }

        public void ShowNextFrame()
        {
            _shouldShowNextFrame = true;
        }

        private bool _shouldShowNextFrame;
    }
}