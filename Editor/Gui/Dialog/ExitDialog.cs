#nullable enable
using ImGuiNET;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;

namespace T3.Editor.Gui.Dialog;

internal sealed class ExitDialog : ModalDialog
{
    internal void Draw()
    {
        DialogSize = new Vector2(330, 200) * T3Ui.UiScaleFactor;
        
        if (BeginDialog(string.Empty))
        {
            FormInputs.AddSectionHeader("Are you leaving?");

            FormInputs.AddVerticalSpace();         
            
            ImGui.PushStyleColor(ImGuiCol.Button, UiColors.BackgroundButton.Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.BackgroundActive.Rgba);

            if (ImGui.Button("The show must go on"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(50, 1));
            ImGui.SameLine();
        
            if (ImGui.Button("Exit"))
            {
                Log.Debug("Shutting down");
                EditorUi.Instance.ExitApplication();   
            }
            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            EndDialogContent();
        }
        EndDialog();
    }
}