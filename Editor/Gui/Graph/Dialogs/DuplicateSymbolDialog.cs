using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs
{
    internal class DuplicateSymbolDialog : ModalDialog
    {
        public void Draw(Instance compositionOp, List<SymbolChildUi> selectedChildUis, ref string nameSpace, ref string newTypeName, ref string description)
        {
            DialogSize = new Vector2(700, 300);

            if (BeginDialog("Duplicate as new symbol"))
            {
                var projectChanged = CustomComponents.DrawProjectDropdown(ref _projectToCopyTo);
                if(projectChanged && _projectToCopyTo != null)
                {
                    nameSpace = _projectToCopyTo.CsProjectFile.RootNamespace + '.' + compositionOp.Symbol.Namespace.Split('.').Last();
                }

                if (_projectToCopyTo != null)
                {
                    // Name and namespace
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.TextUnformatted("Namespace");
                    ImGui.SameLine();

                    ImGui.SetCursorPosX(250 + 20); // Not sure how else to layout this
                    ImGui.TextUnformatted("Name");
                    ImGui.PopFont();
                    
                    var rootNamespace = _projectToCopyTo.CsProjectFile.RootNamespace;
                    var correct = nameSpace.StartsWith(rootNamespace) && GraphUtils.IsNamespaceValid(nameSpace);

                    ImGui.SetNextItemWidth(250);
                    //ImGui.InputText("##namespace", ref nameSpace, 255);
                    InputWithTypeAheadSearch.Draw("##namespace", ref nameSpace,
                                                  _projectToCopyTo.SymbolUis.Select(x => x.Symbol)
                                                                .Select(i => i.Namespace)
                                                                .Distinct()
                                                                .OrderBy(i => i),
                                                  warning: !correct);

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

                    if (CustomComponents.DisablableButton("Duplicate", correct && GraphUtils.IsNewSymbolNameValid(newTypeName, compositionOp.Symbol),
                                                          enableTriggerWithReturn: false))
                    {
                        var compositionSymbolUi = compositionOp.GetSymbolUi();
                        var position = selectedChildUis.First().PosOnCanvas + new Vector2(0, 100);

                        Duplicate.DuplicateAsNewType(compositionSymbolUi, _projectToCopyTo, selectedChildUis.First().SymbolChild.Symbol.Id, newTypeName, nameSpace, description,
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

        private EditableSymbolProject _projectToCopyTo;
    }
}