using Editor.Gui.UiHelpers;
using ImGuiNET;

namespace Editor.Gui.Graph.Dialogs
{
    public class LibWarningDialog : ModalDialog
    {
        public void Draw()
        {
            if (BeginDialog("Careful now"))
            {
                ImGui.TextUnformatted($"You tried to open a library symbol.\nAny change would affect {DependencyCount} operators using it.");
                ImGui.Spacing();
                
                
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("I know what I'm doing"))
                {
                    UserSettings.Config.WarnBeforeLibEdit = false;
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }
            EndDialog();
        }

        public static int DependencyCount=0;
    }
}