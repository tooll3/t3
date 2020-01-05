using ImGuiNET;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows.Output
{
    public class EditResolutionDialog : ModalDialog
    {
        public void Draw(ResolutionHandling.Resolution resolution)
        {
            if (BeginDialog("Edit output resolution"))
            {
                ImGui.SetNextItemWidth(120);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Name:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##parameterName", ref resolution.Title, 255);

                ImGui.Spacing();

                ImGui.SetNextItemWidth(120);
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Resolution:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(250);

                var res = new int[2] { resolution.Width, resolution.Height };
                ImGui.DragInt2("##resolution", ref res[0], 255);
                resolution.Width = res[0];
                resolution.Height = res[1];

                if (CustomComponents.DisablableButton("Add", resolution.IsValid))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    if (!resolution.IsValid)
                    {
                        ResolutionHandling.Resolutions.Remove(resolution);
                    }
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }
    }
}