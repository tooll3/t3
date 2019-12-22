using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.UiHelpers;

namespace T3.Gui.Graph.Dialogs
{
    public class EditInputsDialog : ModalDialog
    {
        private Instance _compositionOp;
        public void Draw(Instance compositionOp, List<SymbolChildUi> selectedSymbolChildUis)
        {
            if (BeginDialog("Edit inputs"))
            {
                _compositionOp = compositionOp;
                EnsureSelection();

                DrawInputList();

                ImGui.BeginChild("InputDetails", new Vector2(-1, 260), false);
                {
                    ImGui.Text("Input Details");
                    ImGui.EndChild();
                }
                
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                EndDialogContent();
            }

            EndDialog();
        }

        private void DrawInputList()
        {
            ImGui.BeginChild("InputList", new Vector2(150, ImGui.GetContentRegionAvail().Y), false);

            {
                for(var index =0; index < _compositionOp.Inputs.Count; index++)
                //foreach (var input in _compositionOp.Inputs)
                {
                    var input = _compositionOp.Inputs[index];
                    if (ImGui.Selectable(input.Input.Name, input.Id == _selectedInputId))
                    {
                        _selectedInputId = input.Id;
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
                    }
    
                    // Drag to reorder
                    if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                    {
                        var dragDy = ImGui.GetMouseDragDelta(0).Y;
                        if (dragDy < 0.0f && index > 0)
                        {
                            SwapInputs(index, index-1);
                            ImGui.ResetMouseDragDelta();
                        }
                        else if (dragDy > 0.0f && index < _compositionOp.Inputs.Count-1)
                        {
                            SwapInputs(index, index+1);
                            ImGui.ResetMouseDragDelta();
                        }
                    }
                }
            }
            ImGui.EndChild();
            ImGui.SameLine();
        }

        // TODO: This needs to be properly implemented
        private void SwapInputs(int indexA, int indexB)
        {
            var inputDefs = _compositionOp.Symbol.InputDefinitions;
            var tmpDef = inputDefs[indexA];
            inputDefs[indexA] = inputDefs[indexB];
            inputDefs[indexB] = tmpDef;

            foreach (var instance in _compositionOp.Symbol._instancesOfSymbol)
            {
                var inputs = instance.Inputs;
                var tmp = inputs[indexA];
                inputs[indexA] = inputs[indexB];
                inputs[indexB] = tmp;
            }
        }
        
        
        private void EnsureSelection()
        {
            var symbol = _compositionOp.Symbol;
            if (symbol.InputDefinitions.Count == 0)
            {
                _selectedInputId = Guid.Empty;
                return;
            }

            var selected = symbol.InputDefinitions.FirstOrDefault(inputDef => inputDef.Id == _selectedInputId);
            if (selected != null)
            {
                return;
            }

            _selectedInputId = symbol.InputDefinitions[0].Id;
        }

        private Guid _selectedInputId = Guid.Empty;
    }
}