using System;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Operators.Types.Id_dab61a12_9996_401e_9aa6_328dd6292beb;

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
                var warning = string.Empty;
                var isValid = GraphUtils.IsNewSymbolNameValid(_parameterName) && _selectedType != null;

                if (!isValid)
                {
                    warning = "parameter must not contain spaces or non-special characters.";
                }
                else if(symbol.InputDefinitions.Exists(i => i.Name == _parameterName))
                {
                    warning = "Parameter name already exists.";
                    isValid = false;
                }
                else if (symbol.InstanceType == typeof(HomeCanvas))
                {
                    warning = "You can't add parameters to the home canvas.";
                    isValid = false;
                }
                
                FormInputs.SetIndent(100);
                FormInputs.AddStringInput("Name", ref _parameterName, "ParameterName", warning);
                
                FormInputs.DrawInputLabel("Type");
                TypeSelector.Draw(ref _selectedType);
                
                FormInputs.AddCheckBox("Multi-Input", ref _multiInput);
                
                FormInputs.AddVerticalSpace(5);
                FormInputs.ApplyIndent();
                if (CustomComponents.DisablableButton("Add", isValid))
                {
                    InputsAndOutputs.AddInputToSymbol(_parameterName, _multiInput, _selectedType, symbol);
                    _parameterName = string.Empty;
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
        private Type _selectedType = typeof(float);

    }
}