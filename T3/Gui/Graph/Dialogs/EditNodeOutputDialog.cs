using System;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class EditNodeOutputDialog : ModalDialog
    {
        public void Draw()
        {
            if (BeginDialog("Edit node output"))
            {
                ImGui.Text("Current output is called " + _outputDefinition.Name);

                var symbolChild = _symbolChildUi.SymbolChild;
                var outputEntry = symbolChild.Outputs[_outputDefinition.Id];
                Type enumType = typeof(DirtyFlagTrigger);
                var values = Enum.GetValues(enumType);
                var valueNames = Enum.GetNames(enumType);
                string currentValueName = Enum.GetName(enumType, outputEntry.DirtyFlagTrigger);

                int index = 0;
                for (int i = 0; i < values.Length; i++)
                {
                    if (valueNames[i] == currentValueName)
                    {
                        index = i;
                        break;
                    }
                }

                if (ImGui.Combo("##dirtyFlagTriggerDropDownParam", ref index, valueNames, valueNames.Length))
                {
                    outputEntry.DirtyFlagTrigger = (DirtyFlagTrigger)Enum.Parse(enumType, valueNames[index]);
                }

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