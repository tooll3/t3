using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Logging;
using T3.Gui.Selection;
using UiHelpers;

using static ImGuiNET.ImGui;


namespace T3.Gui.Animation.CurveEditing
{
    public class CurveEditBox
    {

        public CurveEditBox(CurveEditCanvas curveCanvas)
        {
            _curveCanvas = curveCanvas;
        }
        private CurveEditCanvas _curveCanvas;

        public void Draw()
        {
            if (_curveCanvas.SelectionHandler.SelectedElements.Count <= 1)
                return;

            _bounds = GetBoundingBox();
            MinU = _bounds.Min.X;
            MaxU = _bounds.Max.X;
            MinV = _bounds.Min.Y;
            MaxV = _bounds.Max.Y;

            var boundsOnScreen = _curveCanvas.TransformRect(_bounds);
            _curveCanvas.DrawList.AddRect(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderColor);
            _curveCanvas.DrawList.AddRectFilled(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderFill);

            var deltaOnCanvas = _curveCanvas.InverseTransformDirection(GetIO().MouseDelta);

            ScaleAtSide(
                id: "##top",
                screenPos: boundsOnScreen.Min - VerticalHandleOffset,
                size: new Vector2(boundsOnScreen.GetWidth(), DragHandleSize),
                cursor: ImGuiNET.ImGuiMouseCursor.ResizeNS,
                scale: (_bounds.Max.Y + deltaOnCanvas.Y - MinV) / (MaxV - MinV),
                (float scale, CurvePointUi ep) => { ep.ManipulateV(MinV + (ep.PosOnCanvas.Y - MinV) * scale); }
                );

            ScaleAtSide(
                id: "##bottom",
                screenPos: new Vector2(boundsOnScreen.Min.X, boundsOnScreen.Max.Y),
                size: new Vector2(boundsOnScreen.GetWidth(), DragHandleSize),
                cursor: ImGuiNET.ImGuiMouseCursor.ResizeNS,
                scale: (_bounds.Max.Y - deltaOnCanvas.Y - MinV) / (MaxV - MinV),
                (float scale, CurvePointUi ep) => { ep.ManipulateV(MaxV - (MaxV - ep.PosOnCanvas.Y) * scale); }
                );

            // Right
            {
                SetCursorScreenPos(new Vector2(boundsOnScreen.Max.X, boundsOnScreen.Min.Y));
                Button("##right", new Vector2(DragHandleSize, boundsOnScreen.GetHeight()));
                if (IsItemActive() || IsItemHovered())
                    SetMouseCursor(ImGuiNET.ImGuiMouseCursor.ResizeEW);

                if (IsItemActive() && IsMouseDragging(0))
                {
                    var scale = (_bounds.Max.X - MinU) / (MaxU - MinU);

                    if (Double.IsNaN(scale) || Math.Abs(scale) > 10000)
                        return;

                    foreach (var ep in CurvePointsControls)
                    {
                        ep.ManipulateV(MaxV - (MaxV - ep.PosOnCanvas.Y) * scale);
                    }
                }
            }
        }

        private void ScaleAtSide(string id, Vector2 screenPos, Vector2 size, ImGuiNET.ImGuiMouseCursor cursor, float scale, Action<float, CurvePointUi> scaleFunction)
        {
            SetCursorScreenPos(screenPos);
            Button(id, size);
            if (IsItemActive() || IsItemHovered())
                SetMouseCursor(cursor);

            if (!IsItemActive() || !IsMouseDragging(0))
                return;

            if (Double.IsNaN(scale) || Math.Abs(scale) > 10000)
                return;


            foreach (var ep in CurvePointsControls)
            {
                scaleFunction(scale, ep);
            }
        }





        /*
            public void ScaleAtRightPosition()
            {
                var position = Mouse.GetPosition(CurveEditor);
                var endU = CurveEditor.xToU(position.X);

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    var snapEnd = CurveEditor._USnapHandler.CheckForSnapping(endU);
                    if (!Double.IsNaN(snapEnd))
                    {
                        endU = snapEnd;
                    }
                }

                var scale = (endU - MinU) / (MaxU - MinU);

                if (!Double.IsNaN(scale) && Math.Abs(scale) < 10000)
                {
                    if (scale != 1.0)
                    {
                        CurveEditor.DisableRebuildOnCurveChangeEvents();
                        var idx = 0;
                        foreach (var cpc in CurvePointsControls)
                        {
                            cpc.ManipulateU(MinU + (cpc.U - MinU) * scale);
                            _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
                            ++idx;
                        }
                        _moveKeyframesCommand.Do();

                    }
                }
                CurveEditor.EnableRebuildOnCurveChangeEvents();
                UpdateShapeAndLines();
            }


            public void ScaleAtLeftPosition()
            {
                var position = Mouse.GetPosition(CurveEditor);
                var startU = CurveEditor.xToU(position.X);

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    var snapStart = CurveEditor._USnapHandler.CheckForSnapping(startU);
                    if (!Double.IsNaN(snapStart))
                    {
                        startU = snapStart;
                    }
                }

                var scale = (MaxU - startU) / (MaxU - MinU);
                if (!Double.IsNaN(scale) && Math.Abs(scale) < 10000)
                {
                    if (scale != 1.0)
                    {
                        CurveEditor.DisableRebuildOnCurveChangeEvents();

                        var idx = 0;
                        foreach (var cpc in CurvePointsControls)
                        {
                            cpc.ManipulateU(MaxU - (MaxU - cpc.U) * scale);
                            _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
                            ++idx;
                        }
                        _moveKeyframesCommand.Do();
                    }
                }
                CurveEditor.EnableRebuildOnCurveChangeEvents();
                UpdateShapeAndLines();
            }
        */

        #region XAML event handlers


        private static void XMoveBothThumb_DragDelta()
        {
            //var idx = 0;
            //foreach (var cpc in CurvePointsControls)
            //{
            //    cpc.ManipulateV(cpc.V + CurveEditor.yToV(e.VerticalChange) - CurveEditor.yToV(0));
            //    cpc.ManipulateU(cpc.U + CurveEditor.xToU(e.HorizontalChange) - CurveEditor.xToU(0));
            //    _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
            //    _addOrUpdateKeyframeCommands[idx].KeyframeValue = cpc.m_vdef;
            //    ++idx;
            //}
            //_moveKeyframesCommand.Do();
        }

        /*
        private void XMoveVerticalThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newMinV = MinV + CurveEditor.dyToV(e.VerticalChange);
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                var snapMin = CurveEditor._ValueSnapHandler.CheckForSnapping(newMinV);
                if (!Double.IsNaN(snapMin))
                {
                    newMinV = snapMin;
                }
            }
            var deltaV = newMinV - MinV;

            CurveEditor.DisableRebuildOnCurveChangeEvents();
            var idx = 0;
            foreach (var ep in CurvePointsControls)
            {
                ep.ManipulateV(ep.V + deltaV);
                _addOrUpdateKeyframeCommands[idx].KeyframeValue = ep.m_vdef;
                ++idx;
            }
            _moveKeyframesCommand.Do();
            CurveEditor.EnableRebuildOnCurveChangeEvents();
            UpdateShapeAndLines();
        }

        private void XMoveHorizonalThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newStartU = MinU + CurveEditor.dxToU(e.HorizontalChange);
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                var snapStart = CurveEditor._USnapHandler.CheckForSnapping(newStartU);
                if (!Double.IsNaN(snapStart))
                {
                    newStartU = snapStart;
                }
            }
            var deltaU = newStartU - MinU;

            CurveEditor.DisableRebuildOnCurveChangeEvents();
            var idx = 0;
            foreach (var cpc in CurvePointsControls)
            {
                cpc.ManipulateU(cpc.U + deltaU);
                _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
                ++idx;
            }
            _moveKeyframesCommand.Do();
            CurveEditor.EnableRebuildOnCurveChangeEvents();
            UpdateShapeAndLines();
        }
        */

        //private void DragCompleted(object sender, DragCompletedEventArgs e)
        //{
        //    CompleteMoveKeyframeCommand();
        //}

        //public void CompleteMoveKeyframeCommand()
        //{
        //    App.Current.UndoRedoStack.Add(_moveKeyframesCommand);
        //    CurveEditor.RebuildCurrentCurves();
        //}

        //public void StartMoveKeyframeCommand()
        //{
        //    _addOrUpdateKeyframeCommands.Clear();
        //    MakeUpdateCommandsForSelectedCurvePoints();
        //    _moveKeyframesCommand = new MacroCommand("Move Keyframes", _addOrUpdateKeyframeCommands);
        //}

        //private void MakeUpdateCommandsForSelectedCurvePoints()
        //{
        //    var timeCurveTuples = from curvePoint in CurvePointsControls
        //                          select new Tuple<double, ICurve>(curvePoint.U, curvePoint.Curve);
        //    foreach (var tuple in timeCurveTuples)
        //    {
        //        var keyframeTime = tuple.Item1;
        //        var curve = tuple.Item2;
        //        _addOrUpdateKeyframeCommands.Add(new AddOrUpdateKeyframeCommand(keyframeTime, curve.GetV(keyframeTime), curve));
        //    }
        //}

        //private List<AddOrUpdateKeyframeCommand> _addOrUpdateKeyframeCommands = new List<AddOrUpdateKeyframeCommand>();
        //private MacroCommand _moveKeyframesCommand;

        private IEnumerable<CurvePointUi> CurvePointsControls
        {
            get
            {
                return from selectedElement in _curveCanvas.SelectionHandler.SelectedElements
                       let curvePoint = selectedElement as CurvePointUi
                       where curvePoint != null
                       select curvePoint;
            }
        }
        #endregion


        private ImRect GetBoundingBox()
        {
            float minU = float.PositiveInfinity;
            float minV = float.PositiveInfinity;
            float maxU = float.NegativeInfinity;
            float maxV = float.NegativeInfinity;

            if (_curveCanvas.SelectionHandler.SelectedElements.Count > 1)
            {
                foreach (var selected in _curveCanvas.SelectionHandler.SelectedElements)
                {
                    if (selected is CurvePointUi pc)
                    {
                        minU = Math.Min(minU, pc.PosOnCanvas.X);
                        maxU = Math.Max(maxU, pc.PosOnCanvas.X);
                        minV = Math.Min(minV, pc.PosOnCanvas.Y);
                        maxV = Math.Max(maxV, pc.PosOnCanvas.Y);
                    }
                }
                return new ImRect(minU, minV, maxU, maxV);
            }
            else if (_curveCanvas.SelectionHandler.SelectedElements.Count == 1)
            {
                var pc = _curveCanvas.SelectionHandler.SelectedElements[0] as CurvePointUi;
                minU = maxU = pc.PosOnCanvas.X;
                minV = maxV = pc.PosOnCanvas.Y;
                return new ImRect(minU, minV, maxU, maxV);
            }
            return new ImRect(100, 100, 200, 200);
        }


        private static float MinU;
        private static float MaxU;
        private static float MinV;
        private static float MaxV;

        private ImRect _bounds;


        // Styling
        private static float DragHandleSize = 10;
        private static Vector2 VerticalHandleOffset = new Vector2(0, DragHandleSize);
        private static Vector2 HorizontalHandleOffset = new Vector2(DragHandleSize, 0);
        private static Vector2 HandleOffset = new Vector2(DragHandleSize, DragHandleSize);

        private static Color SelectBoxBorderColor = new Color(1, 1, 1, 0.2f);
        private static Color SelectBoxBorderFill = new Color(1, 1, 1, 0.05f);
    }
}


