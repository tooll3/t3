using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class DuplicateSymbolDialog : ModalDialog
    {
        public void Draw(Instance compositionOp, List<SymbolChildUi> selectedChildUis, ref string nameSpace, ref string combineName)
        {
            if (BeginDialog("Duplicate as new symbol"))
            {
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.Text("Namespace");
                ImGui.SameLine();

                ImGui.SetCursorPosX(250 + 20); // Not sure how else to layout this
                ImGui.Text("Name");
                ImGui.PopFont();

                ImGui.SetNextItemWidth(250);
                ImGui.InputText("##namespace", ref nameSpace, 255);

                ImGui.SetNextItemWidth(150);
                ImGui.SameLine();

                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                ImGui.InputText("##name", ref combineName, 255);

                CustomComponents
                   .HelpText("This is a C# class. It must be unique and\nnot include spaces or special characters");
                ImGui.Spacing();

                if (CustomComponents.DisablableButton("Duplicate", NodeOperations.IsNewSymbolNameValid(combineName)))
                {
                    var compositionSymbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];
                    NodeOperations.DuplicateAsNewType(compositionSymbolUi,
                                                      selectedChildUis.First().SymbolChild,
                                                      newTypeName: combineName,
                                                      nameSpace: nameSpace);
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