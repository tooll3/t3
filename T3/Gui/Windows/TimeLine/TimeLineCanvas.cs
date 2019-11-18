using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using SharpDX.Direct2D1;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public class TimeLineCanvas : ICanvas, ITimeElementSelectionHolder
    {
        public TimeLineCanvas(ClipTime clipTime = null)
        {
            ClipTime = clipTime;
            _dopeSheetArea = new DopeSheetArea(_snapHandler, this);
            _selectionFence = new TimeSelectionFence(this);
            _curveEditArea = new CurveEditArea(this, _snapHandler);

            _snapHandler.AddSnapAttractor(_timeRasterSwitcher);
            _snapHandler.AddSnapAttractor(_currentTimeMarker);
            _snapHandler.SnappedEvent += SnappedEventHandler;
        }

        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters)
        {
            Current = this;
            _io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();
            _drawlist = ImGui.GetWindowDrawList();

            WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
            WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);

            // Damp scaling
            const float dampSpeed = 30f;
            var damping = _io.DeltaTime * dampSpeed;
            if (!float.IsNaN(damping) && damping > 0.001f && damping <= 1.0f)
            {
                Scale = Im.Lerp(Scale, _scaleTarget, damping);
                Scroll = Im.Lerp(Scroll, _scrollTarget, damping);
            }

            var modeChanged = UpdateMode();

            ImGui.BeginChild("timeline", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                _drawlist = ImGui.GetWindowDrawList();
                _timeLineImage.Draw(_drawlist);
                HandleDeferredActions(animationParameters);
                HandleInteraction();
                _timeRasterSwitcher.Draw(ClipTime);
                DrawSnapIndicator();
                //_layersArea.Draw(compositionOp);

                switch (Mode)
                {
                    case Modes.DopeView:
                        _dopeSheetArea.Draw(compositionOp, animationParameters);
                        break;
                    case Modes.CurveEditor:
                        _curveEditArea.Draw(compositionOp, animationParameters, bringCurvesIntoView: modeChanged);
                        break;
                }

                DrawTimeRange();
                _currentTimeMarker.Draw(ClipTime);
                DrawDragTimeArea();
                _selectionFence.Draw();
            }
            ImGui.EndChild();
        }

        private void HandleInteraction()
        {
            if (!ImGui.IsWindowHovered())
                return;

            if (ImGui.IsMouseDragging(1))
            {
                _scrollTarget -= InverseTransformDirection(_io.MouseDelta);
            }

            if (Math.Abs(_io.MouseWheel) > 0.01f)
                HandleZoomViewWithMouseWheel();
        }

        private void HandleDeferredActions(List<GraphWindow.AnimationParameter> animationParameters)
        {
            if (UserActionRegistry.WasActionQueued(UserActions.PlaybackJumpToNextKeyframe))
            {
                var nextKeyframeTime = double.PositiveInfinity;
                foreach (var next in animationParameters
                                    .SelectMany(animationParam => animationParam.Curves, (param, curve) => curve.GetNextU(ClipTime.Time + 0.001f))
                                    .Where(next => next != null && next.Value < nextKeyframeTime))
                {
                    nextKeyframeTime = next.Value;
                }

                if (!double.IsPositiveInfinity(nextKeyframeTime))
                    ClipTime.Time = nextKeyframeTime;
            }

            if (UserActionRegistry.WasActionQueued(UserActions.PlaybackJumpToPreviousKeyframe))
            {
                var prevKeyframeTime = double.NegativeInfinity;
                foreach (var next in animationParameters
                                    .SelectMany(animationParam => animationParam.Curves, (param, curve) => curve.GetPreviousU(ClipTime.Time - 0.001f))
                                    .Where(previous => previous != null && previous.Value > prevKeyframeTime))
                {
                    prevKeyframeTime = next.Value;
                }

                if (!double.IsNegativeInfinity(prevKeyframeTime))
                    ClipTime.Time = prevKeyframeTime;
            }
        }

        private void HandleZoomViewWithMouseWheel()
        {
            var zoomDelta = ComputeZoomDeltaFromMouseWheel();
            var uAtMouse = InverseTransformDirection(_mouse - WindowPos);
            var uScaled = uAtMouse / zoomDelta;
            var deltaU = uScaled - uAtMouse;

            if (_io.KeyShift)
            {
                _scrollTarget.Y -= deltaU.Y;
                _scaleTarget.Y *= zoomDelta;
            }
            else
            {
                _scrollTarget.X -= deltaU.X;
                _scaleTarget.X *= zoomDelta;
            }
        }

        private float ComputeZoomDeltaFromMouseWheel()
        {
            const float zoomSpeed = 1.2f;
            var zoomSum = 1f;
            if (_io.MouseWheel < 0.0f)
            {
                for (var zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                {
                    zoomSum /= zoomSpeed;
                }
            }

            if (_io.MouseWheel > 0.0f)
            {
                for (var zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                {
                    zoomSum *= zoomSpeed;
                }
            }

            zoomSum = zoomSum.Clamp(0.01f, 100f);
            return zoomSum;
        }

        private void DrawDragTimeArea()
        {
            if (ClipTime == null)
                return;

            var max = ImGui.GetContentRegionMax();
            var clamp = max;
            clamp.Y = Im.Min(TimeLineDragHeight, max.Y - 1);

            ImGui.SetCursorPos(new Vector2(0, max.Y - clamp.Y));
            ImGui.InvisibleButton("##TimeDrag", clamp);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            }

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0) || ImGui.IsItemClicked())
            {
                ClipTime.Time = InverseTransformPosition(_io.MousePos).X;
            }

            ImGui.SetCursorPos(Vector2.Zero);
        }

        public void SetVisibleValueRange(float valueScale, float valueScroll)
        {
            _scaleTarget = new Vector2(_scaleTarget.X, valueScale);
            _scrollTarget = new Vector2(_scrollTarget.X, valueScroll);
        }

        #region time range
        private static readonly Vector2 TimeRangeShadowSize = new Vector2(5, 9999);
        private static readonly Color TimeRangeShadowColor = new Color(0, 0, 0, 0.5f);
        private static readonly Color TimeRangeOutsideColor = new Color(0.0f, 0.0f, 0.0f, 0.3f);
        private static readonly Color TimeRangeMarkerColor = new Color(1f, 1, 1f, 0.3f);

        private void DrawTimeRange()
        {
            if (ClipTime == null)
                return;

            ImGui.PushStyleColor(ImGuiCol.Button, TimeRangeMarkerColor.Rgba);

            // Range start
            {
                var xRangeStart = TransformPositionX((float)ClipTime.TimeRangeStart);
                var rangeStartPos = new Vector2(xRangeStart, 0);

                // Shade outside
                _drawlist.AddRectFilled(
                                        new Vector2(0, 0),
                                        new Vector2(xRangeStart, TimeRangeShadowSize.Y),
                                        TimeRangeOutsideColor);

                // Shadow
                _drawlist.AddRectFilled(
                                        rangeStartPos - new Vector2(TimeRangeShadowSize.X - 1, 0),
                                        rangeStartPos + new Vector2(0, TimeRangeShadowSize.Y),
                                        TimeRangeShadowColor);

                // Line
                _drawlist.AddRectFilled(rangeStartPos, rangeStartPos + new Vector2(1, 9999), TimeRangeShadowColor);

                SetCursorToBottom(
                                  xRangeStart - TimeRangeHandleSize.X,
                                  TimeRangeHandleSize.Y);

                ImGui.Button("##StartPos", TimeRangeHandleSize);

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    ClipTime.TimeRangeStart += InverseTransformDirection(_io.MouseDelta).X;
                }
            }

            // Range end
            {
                var rangeEndX = TransformPositionX((float)ClipTime.TimeRangeEnd);
                var rangeEndPos = new Vector2(rangeEndX, 0);

                // Shade outside
                var windowMaxX = ImGui.GetContentRegionAvail().X + WindowPos.X;
                if (rangeEndX < windowMaxX)
                    _drawlist.AddRectFilled(
                                            rangeEndPos,
                                            rangeEndPos + new Vector2(windowMaxX - rangeEndX, TimeRangeShadowSize.Y),
                                            TimeRangeOutsideColor);

                // Shadow
                _drawlist.AddRectFilled(
                                        rangeEndPos,
                                        rangeEndPos + TimeRangeShadowSize,
                                        TimeRangeShadowColor);

                // Line
                _drawlist.AddRectFilled(rangeEndPos, rangeEndPos + new Vector2(1, 9999), TimeRangeShadowColor);

                SetCursorToBottom(
                                  rangeEndX,
                                  TimeRangeHandleSize.Y);

                ImGui.Button("##EndPos", TimeRangeHandleSize);

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    ClipTime.TimeRangeEnd += InverseTransformDirection(_io.MouseDelta).X;
                }
            }

            ImGui.PopStyleColor();
        }
        #endregion

        private void SnappedEventHandler(double snapPosition)
        {
            _lastSnapTime = ImGui.GetTime();
            _lastSnapU = (float)snapPosition;
        }

        private void DrawSnapIndicator()
        {
            var opacity = 1 - Im.Clamp((float)(ImGui.GetTime() - _lastSnapTime) / _snapIndicatorDuration, 0, 1);
            var color = Color.Orange;
            color.Rgba.W = opacity;
            var p = new Vector2(TransformPositionX(_lastSnapU), 0);
            _drawlist.AddRectFilled(p, p + new Vector2(1, 2000), color);
        }

        private double _lastSnapTime;
        private float _snapIndicatorDuration = 1;
        private float _lastSnapU = 0;

        #region ISelection holder
        public void ClearSelection()
        {
            foreach (var sh in _selectionHolders)
            {
                sh.ClearSelection();
            }
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectMode selectMode)
        {
            foreach (var sh in _selectionHolders)
            {
                sh.UpdateSelectionForArea(screenArea, selectMode);
            }
        }

        public ICommand StartDragCommand()
        {
            foreach (var s in _selectionHolders)
            {
                s.StartDragCommand();
            }

            return null;
        }

        public void UpdateDragCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragCommand(dt, dv);
            }
        }

        public void UpdateDragStartCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragStartCommand(dt, dv);
            }
        }

        public void UpdateDragEndCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragEndCommand(dt, dv);
            }
        }

        public void CompleteDragCommand()
        {
            foreach (var s in _selectionHolders)
            {
                s.CompleteDragCommand();
            }
        }

        public void DeleteSelectedElements()
        {
            foreach (var s in _selectionHolders)
            {
                s.DeleteSelectedElements();
            }
        }

        private readonly List<ITimeElementSelectionHolder> _selectionHolders = new List<ITimeElementSelectionHolder>();
        #endregion

        #region view modes
        private bool UpdateMode()
        {
            if (Mode == _lastMode)
                return false;

            switch (_lastMode)
            {
                case Modes.DopeView:
                    _selectionHolders.Remove(_dopeSheetArea);
                    _snapHandler.RemoveSnapAttractor(_dopeSheetArea);
                    break;

                case Modes.CurveEditor:
                    _selectionHolders.Remove(_curveEditArea);
                    _snapHandler.RemoveSnapAttractor(_curveEditArea);
                    break;
            }

            switch (Mode)
            {
                case Modes.DopeView:
                    _selectionHolders.Add(_dopeSheetArea);
                    _snapHandler.AddSnapAttractor(_dopeSheetArea);
                    break;

                case Modes.CurveEditor:
                    _selectionHolders.Add(_curveEditArea);
                    _snapHandler.AddSnapAttractor(_curveEditArea);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            _lastMode = Mode;
            return true;
        }

        public enum Modes
        {
            DopeView,
            CurveEditor,
        }

        public Modes Mode = Modes.DopeView;
        private Modes _lastMode = Modes.CurveEditor; // Make different to force initial update
        #endregion

        #region implement ICanvas =================================================================
        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            return (posOnCanvas - Scroll) * Scale + WindowPos;
        }

        public Vector2 TransformPositionFloored(Vector2 posOnCanvas)
        {
            return Im.Floor((posOnCanvas - Scroll) * Scale + WindowPos);
        }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public float TransformPositionX(float xOnCanvas)
        {
            return (xOnCanvas - Scroll.X) * Scale.X + WindowPos.X;
        }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public float TransformPositionY(float yOnCanvas)
        {
            return (yOnCanvas - Scroll.Y) * Scale.Y + WindowPos.Y;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public Vector2 InverseTransformPosition(Vector2 posOnScreen)
        {
            return (posOnScreen - WindowPos) / Scale + Scroll;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public float InverseTransformPositionX(float xOnScreen)
        {
            return (xOnScreen - WindowPos.X) / Scale.X + Scroll.X;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public float InverseTransformPositionY(float yOnScreen)
        {
            return (yOnScreen - WindowPos.Y) / Scale.Y + Scroll.Y;
        }

        /// <summary>
        /// Convert direction on canvas to delta in screen space
        /// </summary>
        public Vector2 TransformDirection(Vector2 vectorInCanvas)
        {
            return vectorInCanvas * Scale;
        }

        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 InverseTransformDirection(Vector2 vectorInScreen)
        {
            return vectorInScreen / Scale;
        }

        /// <summary>
        /// Convert rectangle on canvas to screen space
        /// </summary>
        public ImRect TransformRect(ImRect canvasRect)
        {
            var r = new ImRect(TransformPositionFloored(canvasRect.Min), TransformPositionFloored(canvasRect.Max));
            if (r.Min.Y > r.Max.Y)
            {
                var t = r.Min.Y;
                r.Min.Y = r.Max.Y;
                r.Max.Y = t;
            }

            return r;
        }

        public ImRect InverseTransformRect(ImRect screenRect)
        {
            var r = new ImRect(InverseTransformPosition(screenRect.Min), InverseTransformPosition(screenRect.Max));
            if (!(r.Min.Y > r.Max.Y))
                return r;

            var t = r.Min.Y;
            r.Min.Y = r.Max.Y;
            r.Max.Y = t;
            return r;
        }

        /// <summary>
        /// Get relative position within canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale - Scroll;
        }

        IEnumerable<ISelectable> ICanvas.SelectableChildren => new List<ISelectable>();

        public bool IsRectVisible(Vector2 pos, Vector2 size)
        {
            return pos.X + size.X >= WindowPos.X
                   && pos.Y + size.Y >= WindowPos.Y
                   && pos.X < WindowPos.X + WindowSize.X
                   && pos.Y < WindowPos.Y + WindowSize.Y;
        }

        /// <summary>
        /// Damped scale factors for u and v
        /// </summary>
        public Vector2 Scale { get; private set; } = new Vector2(1, -1);

        public Vector2 WindowPos { get; private set; }
        public Vector2 WindowSize { get; private set; }
        public Vector2 Scroll { get; private set; } = new Vector2(0, 0.0f);
        private Vector2 _scrollTarget = new Vector2(-1.0f, 0.0f);
        public List<ISelectable> SelectableChildren { get; set; }
        public SelectionHandler SelectionHandler { get; set; } = null;
        #endregion

        private static void SetCursorToBottom(float xInScreen, float paddingFromBottom)
        {
            var max = ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos();
            var p = new Vector2(xInScreen, max.Y - paddingFromBottom);
            ImGui.SetCursorScreenPos(p);
        }

        internal readonly ClipTime ClipTime;

        private readonly TimeRasterSwitcher _timeRasterSwitcher = new TimeRasterSwitcher();
        //private readonly LayersArea _layersArea;

        private readonly DopeSheetArea _dopeSheetArea;
        private readonly CurveEditArea _curveEditArea;
        private readonly TimeLineImage _timeLineImage = new TimeLineImage();

        private readonly CurrentTimeMarker _currentTimeMarker = new CurrentTimeMarker();
        private readonly ValueSnapHandler _snapHandler = new ValueSnapHandler();
        private readonly TimeSelectionFence _selectionFence;

        public static TimeLineCanvas Current;

        private ImGuiIOPtr _io;
        private Vector2 _mouse;
        private Vector2 _scaleTarget = new Vector2(100, -1);

        private ImDrawListPtr _drawlist;

        // Styling
        private const float TimeLineDragHeight = 20;
        private static readonly Vector2 TimeRangeHandleSize = new Vector2(10, 20);
    }
}