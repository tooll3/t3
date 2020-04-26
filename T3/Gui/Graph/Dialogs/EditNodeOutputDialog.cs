using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class EditNodeOutputDialog : ModalDialog
    {
        public void Draw()
        {
            if (BeginDialog("Edit node output"))
            {
                ImGui.Text("insert stuff here...");
                ImGui.Text("Current output is called " + _outputDefinition.Name);
                //ImGui.Checkbox("Multi-Input", ref _multiInput);

                if (ImGui.Button("Close"))
                {
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }

        public void OpenForOutput(SymbolChildUi symbolChildUi, Symbol.OutputDefinition outputDefinition)
        {
            _symbolChildUi = symbolChildUi;
            _outputDefinition = outputDefinition;
            ShowNextFrame();
        }

        private SymbolChildUi _symbolChildUi;
        private Symbol.OutputDefinition _outputDefinition;
    }
}