using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs
{
    public class DuplicateSymbolDialog : ModalDialog
    {
        public void Draw(Instance compositionOp, List<SymbolChildUi> selectedChildUis, ref string nameSpace, ref string newTypeName, ref string description, ref string rootNamespace)
        {
            DialogSize = new Vector2(500, 300);

            if (BeginDialog("Duplicate as new symbol"))
            {

                FormInputs.AddSymbolProjectDropdown(ref rootNamespace, out var project);

                if (project != null)
                {
                    // Name and namespace
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.TextUnformatted("Namespace");
                    ImGui.SameLine();

                    ImGui.SetCursorPosX(250 + 20); // Not sure how else to layout this
                    ImGui.TextUnformatted("Name");
                    ImGui.PopFont();
                    
                    var projectRootNamespace = project.CsProjectFile.RootNamespace;
                    var changed = FormInputs.EnforceStringStart(projectRootNamespace + '.', ref nameSpace, true);

                    ImGui.SetNextItemWidth(250);
                    //ImGui.InputText("##namespace", ref nameSpace, 255);
                    InputWithTypeAheadSearch.Draw("##namespace", ref nameSpace,
                                                  SymbolRegistry.Entries.Values.Select(i => i.Namespace).Distinct().OrderBy(i => i),
                                                  warning: changed);

                    ImGui.SetNextItemWidth(150);
                    ImGui.SameLine();

                    if (ImGui.IsWindowAppearing())
                        ImGui.SetKeyboardFocusHere();

                    ImGui.InputText("##name", ref newTypeName, 255);

                    CustomComponents.HelpText("This is a C# class. It must be unique and\nnot include spaces or special characters");
                    ImGui.Spacing();


                    // Description
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.TextUnformatted("Description");
                    ImGui.PopFont();
                    ImGui.SetNextItemWidth(460);
                    ImGui.InputTextMultiline("##description", ref description, 1024, new Vector2(450, 60));

                    if (CustomComponents.DisablableButton("Duplicate", GraphUtils.IsNewSymbolNameValid(newTypeName, compositionOp.Symbol),
                                                          enableTriggerWithReturn: false))
                    {
                        var compositionSymbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];
                        var position = selectedChildUis.First().PosOnCanvas + new Vector2(0, 100);

                        Duplicate.DuplicateAsNewType(compositionSymbolUi, project, selectedChildUis.First().SymbolChild.Symbol.Id, newTypeName, nameSpace, description,
                                                     position);
                        T3Ui.Save(false);
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                }

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