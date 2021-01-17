using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class CombineToSymbolDialog : ModalDialog
    {
        public void Draw(Instance compositionOp, List<SymbolChildUi> selectedChildUis, ref string nameSpace, ref string combineName, ref string description)
        {
            DialogSize = new Vector2(500, 280);

            if (BeginDialog("Combine into symbol"))
            {
                // namespace and title
                {
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.Text("Namespace");
                    ImGui.SameLine();

                    ImGui.SetCursorPosX(250 + 20); // Not sure how else to layout this
                    ImGui.Text("Name");
                    ImGui.PopFont();

                    ImGui.SetNextItemWidth(250);
                    InputWithTypeAheadSearch.Draw("##namespace", ref nameSpace,
                                                              SymbolRegistry.Entries.Values.Select(i => i.Namespace).Distinct().OrderBy(i => i));
                        
                    ImGui.SetNextItemWidth(150);
                    ImGui.SameLine();

                    if (ImGui.IsWindowAppearing())
                        ImGui.SetKeyboardFocusHere();

                    ImGui.InputText("##name", ref combineName, 255);

                    CustomComponents.HelpText("The name is a C# class. It must be unique and not include spaces or special characters");
                    ImGui.Spacing();
                }

                // Description
                {
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.Text("Description");
                    ImGui.PopFont();
                    ImGui.SetNextItemWidth(460);
                    ImGui.InputTextMultiline("##description", ref description, 1024, new Vector2(450, 60));
                }

                if (CustomComponents.DisablableButton("Combine", NodeOperations.IsNewSymbolNameValid(combineName)))
                {
                    var compositionSymbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];
                    NodeOperations.CombineAsNewType(compositionSymbolUi, selectedChildUis, combineName, nameSpace, description);
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