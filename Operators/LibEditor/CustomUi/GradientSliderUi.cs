using System.Numerics;
using ImGuiNET;
using Lib.numbers.color;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.Commands;
using T3.Editor.UiModel.Commands.Graph;
using T3.Editor.UiModel.InputsAndTypes;

namespace libEditor.CustomUi;

public static class GradientSliderUi
{
    private static ChangeInputValueCommand _inputValueCommandInFlight;
    private static object _inputSlotForActiveCommand;

    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect, Vector2 canvasScale)
    {
        if (instance is not SampleGradient gradientInstance
            || !ImGui.IsRectVisible(selectableScreenRect.Min, selectableScreenRect.Max))
            return SymbolUi.Child.CustomUiResult.None;

        var dragWidth = WidgetElements.DrawDragIndicator(selectableScreenRect, drawList, canvasScale);
        var innerRect = selectableScreenRect;
        innerRect.Min.X += dragWidth;
            
        var gradient = (gradientInstance.Gradient.HasInputConnections) 
                           ? gradientInstance.Gradient.Value 
                           :gradientInstance.Gradient.TypedInputValue.Value;

        InputEditStateFlags editState = InputEditStateFlags.Nothing;
        if (gradient != null)   
        {
            var cloneIfModified = gradientInstance.Gradient.Input.IsDefault;
                
            editState = GradientEditor.Draw(ref gradient, drawList, innerRect, cloneIfModified);
            var inputSlot = gradientInstance.Gradient;

            if (editState.HasFlag(InputEditStateFlags.Started) )
            {
                _inputSlotForActiveCommand = inputSlot;
                _inputValueCommandInFlight =
                    new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input, inputSlot.Input.Value);
            }

            if (editState.HasFlag(InputEditStateFlags.Modified))
            {
                if (cloneIfModified)
                {
                    gradientInstance.Gradient.SetTypedInputValue(gradient);
                }
                gradientInstance.Color.DirtyFlag.Invalidate();
                gradientInstance.OutGradient.DirtyFlag.Invalidate();
                    
                if (_inputValueCommandInFlight == null || _inputSlotForActiveCommand != inputSlot)
                {
                    _inputValueCommandInFlight =
                        new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input, inputSlot.Input.Value);
                    _inputSlotForActiveCommand = inputSlot;
                }

                _inputValueCommandInFlight.AssignNewValue(inputSlot.Input.Value);
                inputSlot.DirtyFlag.Invalidate();
            }

            if (editState.HasFlag(InputEditStateFlags.Finished))
            {
                if (_inputValueCommandInFlight != null && _inputSlotForActiveCommand == inputSlot)
                {
                    UndoRedoStack.Add(_inputValueCommandInFlight);
                }

                _inputValueCommandInFlight = null;
            }                
                
                
            var x = gradientInstance.SamplePos.Value.Clamp(0, 1) * innerRect.GetWidth();
            var pMin = new Vector2(innerRect.Min.X + x, innerRect.Min.Y);
            var pMax = new Vector2(innerRect.Min.X + x + 2, innerRect.Max.Y);
            drawList.AddRectFilled(pMin, pMax, UiColors.StatusAnimated);
        }

        return SymbolUi.Child.CustomUiResult.Rendered 
               | SymbolUi.Child.CustomUiResult.PreventInputLabels 
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph 
               | SymbolUi.Child.CustomUiResult.PreventTooltip 
               | SymbolUi.Child.CustomUiResult.PreventOpenParameterPopUp
               | (editState != InputEditStateFlags.Nothing ? SymbolUi.Child.CustomUiResult.IsActive : SymbolUi.Child.CustomUiResult.None);
    }
}