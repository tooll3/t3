using System.Collections.Generic;
using ImGuiNET;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class RenameSymbolDialog : ModalDialog
    {
        public void Draw(List<SymbolChildUi> selectedChildUis, ref string name)
        {
            if (BeginDialog("Rename symbol"))
            {
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.TextUnformatted("Name");
                ImGui.PopFont();

                ImGui.SetNextItemWidth(150);

                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                ImGui.InputText("##name", ref name, 255);

                CustomComponents.HelpText("This is a C# class. It must be unique and\nnot include spaces or special characters");
                ImGui.Spacing();

                if (CustomComponents.DisablableButton("Rename", NodeOperations.IsNewSymbolNameValid(name)))
                {
                    NodeOperations.RenameSymbol(selectedChildUis[0].SymbolChild.Symbol, name);
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
    }
}