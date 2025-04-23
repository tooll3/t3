using System.Numerics;
using ImGuiNET;
using Lib.numbers.curve;
using T3.Core.Operator;
using T3.Editor.Gui;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.InputsAndTypes;

namespace libEditor.CustomUi;

public static class SampleCurveUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect, Vector2 canvasScale)
    {
        if (!(instance is SampleCurve sampleCurve))
            return SymbolUi.Child.CustomUiResult.None;
            
        var dragWidth = WidgetElements.DrawOperatorDragHandle(selectableScreenRect, drawList, canvasScale);
        var innerRect = selectableScreenRect;
        innerRect.Min.X += dragWidth;
        innerRect.Min.Y += 1;
            
        if (innerRect.GetHeight() < 0)
            return SymbolUi.Child.CustomUiResult.PreventTooltip
                   | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph
                   | SymbolUi.Child.CustomUiResult.PreventInputLabels
                   | SymbolUi.Child.CustomUiResult.PreventOpenParameterPopUp;
            
        var curve = (sampleCurve.Curve.HasInputConnections) 
                        ? sampleCurve.Curve.Value 
                        :sampleCurve.Curve.TypedInputValue.Value;

        //var curve = sampleCurve.Curve.Value;
        if (curve == null)
        {
            //Log.Warning("Can't draw undefined gradient");
            return SymbolUi.Child.CustomUiResult.PreventTooltip
                   | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph
                   | SymbolUi.Child.CustomUiResult.PreventInputLabels
                   | SymbolUi.Child.CustomUiResult.PreventOpenParameterPopUp;
        }

        ImGui.PushClipRect(innerRect.Min, innerRect.Max, true);
        ImGui.SetCursorScreenPos(innerRect.Min) ;
        ImGui.BeginChild("curve" + instance.SymbolChildId.GetHashCode(), innerRect.GetSize(), false, ImGuiWindowFlags.NoScrollbar);
        {
            var cloneIfModified = sampleCurve.Curve.Input.IsDefault;
                
            var preventEditingUnlessCtrlPressed = ImGui.GetIO().KeyCtrl
                                                      ? T3Ui.EditingFlags.None
                                                      : T3Ui.EditingFlags.PreventMouseInteractions;

            var keepPositionForIcon = ImGui.GetCursorPos() + Vector2.One;
            var modified2 = CurveInputEditing.DrawCanvasForCurve(ref curve, 
                                                                 sampleCurve.Curve.Input,
                                                                 cloneIfModified,
                                                                 instance.Parent, T3Ui.EditingFlags.ExpandVertically
                                                                                  | preventEditingUnlessCtrlPressed
                                                                                  | T3Ui.EditingFlags.PreventZoomWithMouseWheel);

            var showPopupIcon = innerRect.GetHeight()> ImGui.GetFrameHeight()* T3Ui.UiScaleFactor * 2;
            if (showPopupIcon && CurveEditPopup.DrawPopupIndicator(instance.Parent, sampleCurve.Curve.Input, ref curve, keepPositionForIcon, cloneIfModified, out var popupResult))
            {
                modified2 = popupResult;
            }
                
            if ((modified2 & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing)
            {
                if (cloneIfModified)
                {
                    sampleCurve.Curve.SetTypedInputValue(curve);
                }
                sampleCurve.Result.DirtyFlag.Invalidate();
                sampleCurve.CurveOutput.DirtyFlag.ForceInvalidate();
            }

            DrawSamplePointIndicator();
        }
        ImGui.EndChild();
        ImGui.PopClipRect();

        return SymbolUi.Child.CustomUiResult.Rendered
               | SymbolUi.Child.CustomUiResult.PreventTooltip
               | SymbolUi.Child.CustomUiResult.PreventOpenSubGraph
               | SymbolUi.Child.CustomUiResult.PreventInputLabels
               | SymbolUi.Child.CustomUiResult.PreventOpenParameterPopUp;

        void DrawSamplePointIndicator()
        {
            ICanvas canvas = null;//CurveInputEditing.GetCanvasForCurve(curve);
            if (canvas == null)
                return;
                
            var x = canvas.TransformPosition(new Vector2(sampleCurve.U.Value, 0)).X;
            if (!(x >= innerRect.Min.X) || !(x < innerRect.Max.X))
                return;
                
            var pMin = new Vector2(x, innerRect.Min.Y);
            var pMax = new Vector2(x + 1, innerRect.Max.Y);
            drawList.AddRectFilled(pMin, pMax, UiColors.StatusAnimated);
        }
    }
}