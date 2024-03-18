using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Editor.Compilation;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Dialogs
{
    public class RenameSymbolDialog : ModalDialog
    {
        public void Draw(List<SymbolChildUi> selectedChildUis, ref string name)
        {
            var canRename = selectedChildUis.All(x => !x.SymbolChild.Symbol.SymbolPackage.IsReadOnly);
            
            if (!canRename)
                return;
            
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

                var symbolUi = selectedChildUis[0];
                var symbol = symbolUi.SymbolChild.Symbol;
                
                if (CustomComponents.DisablableButton("Rename", GraphUtils.IsNewSymbolNameValid(name, symbol)))
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
}