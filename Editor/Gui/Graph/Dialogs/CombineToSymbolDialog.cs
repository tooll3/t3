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
    internal class CombineToSymbolDialog : ModalDialog
    {
        public void Draw(Instance compositionOp, List<SymbolUi.Child> selectedChildUis, List<Annotation> selectedAnnotations, ref string nameSpace,
                         ref string combineName, ref string description)
        {
            DialogSize = new Vector2(500, 350);

            if (BeginDialog("Combine into symbol"))
            {
                var projectChanged = CustomComponents.DrawProjectDropdown(ref _projectToCopyTo);
                if(projectChanged && _projectToCopyTo != null)
                {
                    nameSpace = _projectToCopyTo.CsProjectFile.RootNamespace + '.' + compositionOp.Symbol.Namespace.Split('.').Last();
                }

                if (_projectToCopyTo != null)
                {
                    // namespace and title
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.TextUnformatted("Namespace");
                    ImGui.SameLine();

                    ImGui.SetCursorPosX(250 + 20); // Not sure how else to layout this
                    ImGui.TextUnformatted("Name");
                    ImGui.PopFont();
                    
                    var rootNamespace = _projectToCopyTo.CsProjectFile.RootNamespace;
                    var correct = nameSpace.StartsWith(rootNamespace) && GraphUtils.IsNamespaceValid(nameSpace);

                    ImGui.SetNextItemWidth(250);
                    InputWithTypeAheadSearch.Draw("##namespace2", ref nameSpace,
                                                  _projectToCopyTo.SymbolUis.Select(x => x.Symbol)
                                                                .Select(i => i.Namespace)
                                                                .Distinct()
                                                                .OrderBy(i => i),
                                                  warning: !correct);

                    ImGui.SetNextItemWidth(150);
                    ImGui.SameLine();

                    if (ImGui.IsWindowAppearing())
                        ImGui.SetKeyboardFocusHere();

                    ImGui.InputText("##name", ref combineName, 255);

                    CustomComponents.HelpText("The name is a C# class. It must be unique and not include spaces or special characters");
                    ImGui.Spacing();

                    // Description
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.TextUnformatted("Description");
                    ImGui.PopFont();
                    ImGui.SetNextItemWidth(460);
                    ImGui.InputTextMultiline("##description", ref description, 1024, new Vector2(450, 60));

                    ImGui.Checkbox("Combine as time clip", ref _shouldBeTimeClip);

                    if (CustomComponents.DisablableButton("Combine", correct && GraphUtils.IsNewSymbolNameValid(combineName, compositionOp.Symbol),
                                                          enableTriggerWithReturn: false))
                    {
                        var compositionSymbolUi = compositionOp.GetSymbolUi();
                        Combine.CombineAsNewType(compositionSymbolUi, _projectToCopyTo, selectedChildUis, selectedAnnotations, combineName, nameSpace, description,
                                                 _shouldBeTimeClip);
                        _shouldBeTimeClip = false; // Making timeclips this is normally a one off operation
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

        private static bool _shouldBeTimeClip;
        private EditableSymbolProject _projectToCopyTo;
    }
}