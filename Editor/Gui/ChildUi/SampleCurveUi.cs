using System.Numerics;
using Editor.Gui;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.InputUi.CombinedInputs;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Operators.Types.Id_b724ea74_d5d7_4928_9cd1_7a7850e4e179;

namespace T3.Editor.Gui.ChildUi
{
    public static class SampleCurveUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is SampleCurve sampleCurve))
                return SymbolChildUi.CustomUiResult.None;

            var innerRect = selectableScreenRect;
            innerRect.Expand(-7);

            if (innerRect.GetHeight() < 0)
                return SymbolChildUi.CustomUiResult.None;
            

            var curve = sampleCurve.Curve.Value;
            if (curve == null)
            {
                //Log.Warning("Can't draw undefined gradient");
                return SymbolChildUi.CustomUiResult.None;
            }

            ImGui.PushClipRect(innerRect.Min, innerRect.Max, true);
            ImGui.SetCursorScreenPos(innerRect.Min);
            ImGui.BeginChild("curve" + instance.SymbolChildId.GetHashCode(), innerRect.GetSize());
            {
                var preventEditingUnlessCtrlPressed = ImGui.GetIO().KeyCtrl
                                                          ? T3Ui.EditingFlags.None
                                                          : T3Ui.EditingFlags.PreventMouseInteractions;

                CurveInputEditing.DrawCanvasForCurve(curve, T3Ui.EditingFlags.ExpandVertically
                                                            | preventEditingUnlessCtrlPressed
                                                            | T3Ui.EditingFlags.PreventZoomWithMouseWheel);

                DrawSamplePointIndicator();
            }
            ImGui.EndChild();
            ImGui.PopClipRect();

            return SymbolChildUi.CustomUiResult.Rendered
                   | SymbolChildUi.CustomUiResult.PreventTooltip
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph
                   | SymbolChildUi.CustomUiResult.PreventInputLabels
                   | SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp;

            void DrawSamplePointIndicator()
            {
                var canvas = CurveInputEditing.GetCanvasForCurve(curve);
                if (canvas == null)
                    return;
                
                var x = canvas.TransformPosition(new Vector2(sampleCurve.U.Value, 0)).X;
                if (!(x >= innerRect.Min.X) || !(x < innerRect.Max.X))
                    return;
                
                var pMin = new Vector2(x, innerRect.Min.Y);
                var pMax = new Vector2(x + 1, innerRect.Max.Y);
                drawList.AddRectFilled(pMin, pMax, Color.Orange);
            }
        }
    }
}