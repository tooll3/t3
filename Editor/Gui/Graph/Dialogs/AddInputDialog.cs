using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph.Dialogs;

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
                
            _ = SymbolModificationInputs.DrawFieldInputs(symbol, ref _parameterName, ref _selectedType, out var isValid);
                
            FormInputs.AddCheckBox("Multi-Input", ref _multiInput);
                
            FormInputs.AddVerticalSpace(5);
            FormInputs.ApplyIndent();
            if (CustomComponents.DisablableButton("Add", _selectedType != null && isValid))
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