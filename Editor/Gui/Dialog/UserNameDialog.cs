using ImGuiNET;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Dialog
{
    public class UserNameDialog : ModalDialog
    {
        private string _userName = DefaultName;

        public void Draw()
        {
            if (BeginDialog("Edit project name"))
            {
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.TextUnformatted("Nickname");
                ImGui.PopFont();

                ImGui.SetNextItemWidth(150);

                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                ImGui.InputText("##name", ref _userName, 255);

                CustomComponents
                   .HelpText("Tooll will use this to group your projects into a namespace.\n\n(This is a local setting only and not stored online.\n\nIt should be short and not contain spaces or special characters.");
                ImGui.Spacing();

                if (CustomComponents.DisablableButton("Rename", GraphUtils.IsValidProjectName(_userName)))
                {
                    try
                    {
                        ProjectNameChanged?.Invoke(this, _userName);
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error while renaming user {e}");
                    }

                    _userName = DefaultName;
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }

        public event EventHandler<string> ProjectNameChanged;
        private const string DefaultName = "RadNewProjectName";
    }
}