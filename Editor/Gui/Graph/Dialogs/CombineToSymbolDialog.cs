using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs;

internal class CombineToSymbolDialog : ModalDialog
{
    public void Draw(Instance compositionOp, List<SymbolUi.Child> selectedChildUis, List<Annotation> selectedAnnotations, ref string nameSpace,
                     ref string combineName, ref string description)
    {
        DialogSize = new Vector2(500, 350);

        if (BeginDialog("Combine into symbol"))
        {
            _ = SymbolModificationInputs.DrawProjectDropdown(ref nameSpace, ref _projectToCopyTo);

            if (_projectToCopyTo != null)
            {
                _ = SymbolModificationInputs.DrawSymbolNameAndNamespaceInputs(ref combineName, ref nameSpace, _projectToCopyTo, out var symbolNamesValid);

                ImGui.Spacing();

                // Description
                ImGui.PushFont(Fonts.FontSmall);
                ImGui.TextUnformatted("Description");
                ImGui.PopFont();
                ImGui.SetNextItemWidth(460);
                ImGui.InputTextMultiline("##description", ref description, 1024, new Vector2(450, 60));

                ImGui.Checkbox("Combine as time clip", ref _shouldBeTimeClip);
                    
                if (CustomComponents.DisablableButton("Combine", symbolNamesValid, enableTriggerWithReturn: false))
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