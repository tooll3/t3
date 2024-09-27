using ImGuiNET;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs;

public class RenameSymbolDialog : ModalDialog
{
    public void Draw(List<SymbolUi.Child> selectedChildUis, ref string name)
    {
        var canRename = selectedChildUis.Count == 1 && !selectedChildUis[0].SymbolChild.Symbol.SymbolPackage.IsReadOnly;
            
        if (!canRename)
            return;
            
        if (BeginDialog("Rename symbol"))
        {
            ImGui.PushFont(Fonts.FontSmall);
            ImGui.TextUnformatted("Name");
            ImGui.PopFont();

            ImGui.SetNextItemWidth(150);

            var symbolUi = selectedChildUis[0];
            var symbol = symbolUi.SymbolChild.Symbol;
            _ = SymbolModificationInputs.DrawSymbolNameInput(ref name, symbol.Namespace, symbol.SymbolPackage, true, out var isNameValid);

            ImGui.Spacing();
                
            if (CustomComponents.DisablableButton("Rename", isNameValid))
            {
                SymbolNaming.RenameSymbol(symbol, name);
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