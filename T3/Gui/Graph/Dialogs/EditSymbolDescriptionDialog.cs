using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class EditSymbolDescriptionDialog : ModalDialog
    {
        public void Draw(Symbol operatorSymbol)
        {
            DialogSize = new Vector2(450, 600);
            
            if (BeginDialog("Edit description"))
            {
                var symbolUi = SymbolUiRegistry.Entries[operatorSymbol.Id];
                var desc = symbolUi.Description ?? string.Empty;
                
                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();

                ImGui.InputTextMultiline("##name", ref desc, 2000, new Vector2(400,500), ImGuiInputTextFlags.None);
                symbolUi.Description = desc;


                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }
                
                EndDialogContent();
            }
            EndDialog();
        }
    }
}