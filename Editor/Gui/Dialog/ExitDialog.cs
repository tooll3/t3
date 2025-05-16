#nullable enable
using ImGuiNET;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.Editor.UiModel.ProjectHandling;

namespace T3.Editor.Gui.Dialog;

internal sealed class ExitDialog : ModalDialog
{
    internal void Draw()
    {
        DialogSize = new Vector2(330, 200);

        if (BeginDialog(string.Empty))
        {
            FormInputs.AddSectionHeader("Are you leaving?");

            var changeCount = GetChangedSymbolCount();
            if (changeCount > 0)
            {
                ImGui.Text($"Your have {changeCount} unsaved changes.");
            }

            FormInputs.AddVerticalSpace();
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f);
            ImGui.PushStyleColor(ImGuiCol.Button, UiColors.BackgroundButton.Rgba);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiColors.BackgroundActive.Rgba);

            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine(0, 4);
            ImGui.Dummy(new Vector2(4, 1));
            ImGui.SameLine();

            ImGui.PushFont(Fonts.FontBold);
            if (ImGui.Button("Exit"))
            {
                Log.Debug("Shutting down");
                EditorUi.Instance.ExitApplication();
            }

            ImGui.PopFont();

            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
            EndDialogContent();
        }

        EndDialog();
    }

    private static int GetChangedSymbolCount()
    {
        var changeCount = 0;
        if (ProjectView.Focused != null)
        {
            foreach (var package in SymbolPackage.AllPackages)
            {
                foreach (var x in package.Symbols.Values)
                {
                    if (x.GetSymbolUi().HasBeenModified)
                    {
                        changeCount++;
                    }
                }
            }
        }

        return changeCount;
    }
}