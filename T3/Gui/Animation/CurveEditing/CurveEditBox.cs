using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.Selection;
using UiHelpers;

using static ImGuiNET.ImGui;


namespace T3.Gui.Animation.CurveEditing
{
    public class CurveEditBox
    {

        public CurveEditBox(ICanvas curveCanvas, SelectionHandler selectionHandler)
        {
            _curveCanvas = curveCanvas;
            _selectionHandler = selectionHandler;
        }

        
        public void Draw()
        {
            var drawlist = ImGui.GetWindowDrawList();
            if (_selectionHandler.SelectedElements.Count <= 1)
                return;

            _bounds = GetBoundingBox();
            float MinU = _bounds.Min.X;
            float MaxU = _bounds.Max.X;
            float MinV = _bounds.Min.Y;
            float MaxV = _bounds.Max.Y;

            var boundsOnScreen = _curveCanvas.TransformRect(_bounds);
            drawlist.AddRect(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderColor);
            drawlist.AddRectFilled(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderFill);

            var deltaOnCanvas = _curveCanvas.InverseTransformDirection(GetIO().MouseDelta);

            ScaleHandle(
                id: "##top",
                screenPos: boundsOnScreen.Min - VerticalHandleOffset,
                size: new Vector2(boundsOnScreen.GetWidth(), DragHandleSize),
                Direction.Vertical,
                scale: (MaxV + deltaOnCanvas.Y - MinV) / (MaxV - MinV),
                (float scale, CurvePointUi ep) => { ep.ManipulateV(MinV + (ep.PosOnCanvas.Y - MinV) * scale); }
                );

            ScaleHandle(
                id: "##bottom",
                screenPos: new Vector2(boundsOnScreen.Min.X, boundsOnScreen.Max.Y),
                size: new Vector2(boundsOnScreen.GetWidth(), DragHandleSize),
                Direction.Vertical,
                scale: (MaxV - deltaOnCanvas.Y - MinV) / (MaxV - MinV),
                (float scale, CurvePointUi ep) => { ep.ManipulateV(MaxV - (MaxV - ep.PosOnCanvas.Y) * scale); }
                );

            ScaleHandle(
                id: "##right",
                screenPos: new Vector2(boundsOnScreen.Max.X, boundsOnScreen.Min.Y),
                size: new Vector2(DragHandleSize, boundsOnScreen.GetHeight()),
                Direction.Horizontal,
                scale: (MaxU + deltaOnCanvas.X - MinU) / (MaxU - MinU),
                (float scale, CurvePointUi ep) => { ep.ManipulateU(MinU + (ep.PosOnCanvas.X - MinU) * scale); }
                );

            ScaleHandle(
                id: "##left",
                screenPos: boundsOnScreen.Min - HorizontalHandleOffset,
                size: new Vector2(DragHandleSize, boundsOnScreen.GetHeight()),
                Direction.Horizontal,
                scale: (MaxU - deltaOnCanvas.X - MinU) / (MaxU - MinU),
                (float scale, CurvePointUi ep) => { ep.ManipulateU(MaxU - (MaxU - ep.PosOnCanvas.X) * scale); }
                );

            var combined = (MoveRingInnerRadius + MoveRingOuterRadius);
            var center = _curveCanvas.TransformPositionFloored(_bounds.GetCenter());

            MoveHandle(
                "<##moveLeft", Direction.Horizontal,
                screenPos: center + new Vector2(-MoveRingOuterRadius, -0.5f * combined),
                size: new Vector2(MoveRingOuterRadius - MoveRingInnerRadius, combined));


            MoveHandle(
                "^##moveUp", Direction.Vertical,
                screenPos: center + new Vector2(-0.5f * combined, -MoveRingOuterRadius),
                size: new Vector2(combined, MoveRingOuterRadius - MoveRingInnerRadius));

            MoveHandle(
                ">##moveRight", Direction.Horizontal,
                screenPos: center + new Vector2(MoveRingInnerRadius, -0.5f * combined),
                size: new Vector2(MoveRingOuterRadius - MoveRingInnerRadius, combined));

            MoveHandle(
                "v##moveDown", Direction.Vertical,
                screenPos: center + new Vector2(-0.5f * combined, MoveRingInnerRadius),
                size: new Vector2(combined, MoveRingOuterRadius - MoveRingInnerRadius));

            MoveHandle(
                "+##both", Direction.Both,
                screenPos: center - new Vector2(1, 1) * MoveRingInnerRadius,
                size: Vector2.One * MoveRingInnerRadius * 2);


        }

        private static float MoveRingOuterRadius = 25;
        private static float MoveRingInnerRadius = 10;


        private enum Direction
        {
            Horizontal,
            Vertical,
            Both,
        }

        private void ScaleHandle(string id, Vector2 screenPos, Vector2 size, Direction direction, float scale, Action<float, CurvePointUi> scaleFunction)
        {
            if ((direction == Direction.Vertical && _bounds.GetHeight() <= 0.001f)
             || (direction == Direction.Horizontal && _bounds.GetWidth() <= 0.001f))
                return;

            SetCursorScreenPos(screenPos);

            Button(id, size);

            if (IsItemActive() || IsItemHovered())
                SetMouseCursor(
                    direction == Direction.Horizontal
                        ? ImGuiNET.ImGuiMouseCursor.ResizeEW
                        : ImGuiNET.ImGuiMouseCursor.ResizeNS);

            if (!IsItemActive() || !IsMouseDragging(0))
                return;

            if (Double.IsNaN(scale) || Math.Abs(scale) > 10000)
                return;

            if (scale < 0)
                scale = 0;

            foreach (var ep in CurvePointsControls)
            {
                scaleFunction(scale, ep);
            }
        }


        private void MoveHandle(string labelAndId, Direction direction, Vector2 screenPos, Vector2 size)
        {
            SetCursorScreenPos(screenPos);

            Button(labelAndId, size);

            if (IsItemActive() || IsItemHovered())
            {
                switch (direction)
                {
                    case Direction.Horizontal:
                        SetMouseCursor(ImGuiNET.ImGuiMouseCursor.ResizeEW); break;
                    case Direction.Vertical:
                        SetMouseCursor(ImGuiNET.ImGuiMouseCursor.ResizeNS); break;
                    case Direction.Both:
                        SetMouseCursor(ImGuiNET.ImGuiMouseCursor.ResizeAll); break;
                }
            }

            if (!IsItemActive() || !IsMouseDragging(0))
                return;

            var delta = _curveCanvas.InverseTransformDirection(GetIO().MouseDelta);
            if (direction == Direction.Horizontal)
                delta.Y = 0;
            else if (direction == Direction.Vertical)
                delta.X = 0;


            foreach (var ep in CurvePointsControls)
            {

                ep.PosOnCanvas += delta;
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


        //private static float MinU;
        //private static float MaxU;
        //private static float MinV;
        //private static float MaxV;

        private ImRect _bounds;
        private readonly SelectionHandler _selectionHandler;
        private readonly ICanvas _curveCanvas;


        // Styling
        private static float DragHandleSize = 10;
        private static Vector2 VerticalHandleOffset = new Vector2(0, DragHandleSize);
        private static Vector2 HorizontalHandleOffset = new Vector2(DragHandleSize, 0);
        private static Vector2 HandleOffset = new Vector2(DragHandleSize, DragHandleSize);

        private static Color SelectBoxBorderColor = new Color(1, 1, 1, 0.2f);
        private static Color SelectBoxBorderFill = new Color(1, 1, 1, 0.05f);
    }
}


