using System;
using System.Linq;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.InputUi.VectorInputs;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;


namespace T3.Editor.Gui.Graph.Dialogs
{
    public class AddInputDialog : ModalDialog
    {
        public AddInputDialog()
        {
            Flags = ImGuiWindowFlags.NoResize;
            
        }
        
        public void Draw(Symbol symbol)
        {
            if (BeginDialog("Add parameter input"))
            {
                FormInputs.SetIndent(100);
                FormInputs.AddStringInput("Name", ref _parameterName);
                
                FormInputs.DrawInputLabel("Type");
                TypeSelector.Draw(ref _selectedType);
                
                FormInputs.AddCheckBox("Multi-Input", ref _multiInput);

                var isValid = GraphUtils.IsNewSymbolNameValid(_parameterName) && _selectedType != null;
                FormInputs.ApplyIndent();
                if (CustomComponents.DisablableButton("Add", isValid))
                {
                    InputsAndOutputs.AddInputToSymbol(_parameterName, _multiInput, _selectedType, symbol);
                    var symbolUi = SymbolUiRegistry.Entries[symbol.Id];
                    var inputUi = symbolUi.InputUis.Values.SingleOrDefault(i => i.InputDefinition.Name == _parameterName);
                    if (inputUi is FloatInputUi floatInputUi)
                    {
                        floatInputUi.Min = -1;
                        floatInputUi.Max = 42;
                    }
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

        private string _parameterName = string.Empty;
        private bool _multiInput;
        private Type _selectedType;

    }
}