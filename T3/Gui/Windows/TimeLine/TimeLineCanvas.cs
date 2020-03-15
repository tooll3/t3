using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public class TimeLineCanvas : ICanvas, ITimeElementSelectionHolder
    {
        public TimeLineCanvas(Playback playback = null)
        {
            Playback = playback;
            _dopeSheetArea = new DopeSheetArea(_snapHandler, this);
            _selectionFence = new TimeSelectionFence(this);
            _curveEditArea = new CurveEditArea(this, _snapHandler);
            var selectionRange = new TimeSelectionRange(this, _snapHandler);
            LayersArea = new LayersArea(_snapHandler);

            _snapHandler.AddSnapAttractor(_playbackRange);
            _snapHandler.AddSnapAttractor(_timeRasterSwitcher);
            _snapHandler.AddSnapAttractor(_currentTimeMarker);
            _snapHandler.AddSnapAttractor(selectionRange);
            _snapHandler.AddSnapAttractor(LayersArea);

            _snapHandler.SnappedEvent += SnappedEventHandler;
        }

        public bool FoundTimeClipForCurrentTime => LayersArea.FoundClipWithinCurrentTime;

        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters)
        {
            Current = this;
            UpdateLocalTimeTranslation(compositionOp);
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
                _timeLineImage.Draw(_drawlist, Playback);
                ImGui.SetScrollY(0);
                ImGui.Text($"Scale: {Scale.X} Offset: {Scroll.X}   LocScale: {_localScale} LocCffset: {_localOffset}");
                HandleDeferredActions(animationParameters);
                HandleInteraction();
                _timeRasterSwitcher.Draw(Playback);
                DrawSnapIndicator();
                //_layersArea.Draw(compositionOp);

                switch (Mode)
                {
                    case Modes.DopeView:
                        LayersArea.Draw(compositionOp, Playback);
                        _dopeSheetArea.Draw(compositionOp, animationParameters);
                        break;
                    case Modes.CurveEditor:
                        _horizontalRaster.Draw(this);
                        _curveEditArea.Draw(compositionOp, animationParameters, bringCurvesIntoView: modeChanged);
                        break;
                }

                var compositionTimeClip = NodeOperations.GetCompositionTimeClip(compositionOp);
                if (compositionTimeClip != null || Playback.IsLooping)
                {
                    _playbackRange.Draw(this, compositionTimeClip, _drawlist, _snapHandler);
                }
                
                _currentTimeMarker.Draw(Playback);
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
                                    .SelectMany(animationParam => animationParam.Curves, (param, curve) => curve.GetNextU(Playback.TimeInBars + 0.001f))
                                    .Where(next => next != null && next.Value < nextKeyframeTime))
                {
                    nextKeyframeTime = next.Value;
                }

                if (!double.IsPositiveInfinity(nextKeyframeTime))
                    Playback.TimeInBars = nextKeyframeTime;
            }

            if (UserActionRegistry.WasActionQueued(UserActions.PlaybackJumpToPreviousKeyframe))
            {
                var prevKeyframeTime = double.NegativeInfinity;
                foreach (var next in animationParameters
                                    .SelectMany(animationParam => animationParam.Curves, (param, curve) => curve.GetPreviousU(Playback.TimeInBars - 0.001f))
                                    .Where(previous => previous != null && previous.Value > prevKeyframeTime))
                {
                    prevKeyframeTime = next.Value;
                }

                if (!double.IsNegativeInfinity(prevKeyframeTime))
                    Playback.TimeInBars = prevKeyframeTime;
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
            if (Playback == null)
                return;

            var max = ImGui.GetContentRegionMax();
            var clampedSize = max;
            clampedSize.Y = Im.Min(TimeLineDragHeight, max.Y - 1);

            ImGui.SetCursorPos(new Vector2(0, max.Y - clampedSize.Y));
            var screenPos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(screenPos, screenPos + new Vector2(clampedSize.X, 1), Color.Black);

            ImGui.InvisibleButton("##TimeDrag", clampedSize);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            }

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0) || ImGui.IsItemClicked())
            {
                var draggedTime = InverseTransformPosition(_io.MousePos).X;
                if (ImGui.GetIO().KeyShift)
                {
                    _snapHandler.CheckForSnapping(ref draggedTime, _currentTimeMarker);
                }

                Playback.TimeInBars = draggedTime;
            }

            ImGui.SetCursorPos(Vector2.Zero);
        }

        public void SetVisibleRange(Vector2 scale, Vector2 scroll)
        {
            _scaleTarget = scale;
            _scrollTarget = scroll;
        }

        public void SetVisibleValueRange(float valueScale, float valueScroll)
        {
            _scaleTarget = new Vector2(_scaleTarget.X, valueScale);
            _scrollTarget = new Vector2(_scrollTarget.X, valueScroll);
        }

        public void SetVisibleTimeRange(float timeScale, float timeScroll)
        {
            _scaleTarget = new Vector2(timeScale, _scaleTarget.Y);
            _scrollTarget = new Vector2(timeScroll, _scrollTarget.Y);
        }

        private void SnappedEventHandler(double snapPosition)
        {
            _lastSnapTime = ImGui.GetTime();
            _lastSnapU = (float)snapPosition;
        }

        private void DrawSnapIndicator()
        {
            var opacity = 1 - ((float)(ImGui.GetTime() - _lastSnapTime) / _snapIndicatorDuration).Clamp(0, 1);
            var color = Color.Orange;
            color.Rgba.W = opacity;
            var p = new Vector2(TransformPositionX(_lastSnapU), 0);
            _drawlist.AddRectFilled(p, p + new Vector2(1, 2000), color);
        }

        private double _lastSnapTime;
        private float _snapIndicatorDuration = 1;
        private float _lastSnapU;

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

        public void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragStretchCommand(scaleU, scaleV, originU, originV);
            }
        }

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = new TimeRange(float.PositiveInfinity, float.NegativeInfinity);

            foreach (var sh in _selectionHolders)
            {
                timeRange.Unite(sh.GetSelectionTimeRange());
            }

            return timeRange;
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
                    _selectionHolders.Remove(LayersArea);
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
                    _selectionHolders.Add(LayersArea);
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
            var scale =   Scale.X  * _localScale;
            var offset =  (Scroll.X - _localOffset) / _localScale;
            return (xOnCanvas - offset) * scale + WindowPos.X;
        }

        public float TransformGlobalTime(float time)
        {
            var localTime = (time - _localOffset) / _localScale;
            return TransformPositionX(localTime);
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
            var scale =   Scale.X  * _localScale;
            var offset =  (Scroll.X - _localOffset) / _localScale;
            return (xOnScreen - WindowPos.X) / scale  + offset;
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
        /// Damped scale factors for u and v
        /// </summary>
        public Vector2 Scale { get; private set; } = new Vector2(1, -1);

        public Vector2 WindowPos { get; private set; }
        public Vector2 WindowSize { get; private set; }
        public Vector2 Scroll { get; private set; } = new Vector2(0, 0.0f);


        
        private void UpdateLocalTimeTranslation(Instance compositionOp)
        {
            _localScale = 1f;
            _localOffset = 0f;
            ;
            var parents= GraphCanvas.Current.GetParents().Reverse().ToList();
            parents.Add(compositionOp);
            foreach (var p in parents)
            {
                if (p.Outputs.Count <= 0 || !(p.Outputs[0] is ITimeClipProvider timeClipProvider))
                    continue;
                
                var clip = timeClipProvider.TimeClip;
                var scale = clip.TimeRange.Duration / clip.SourceRange.Duration;
                _localScale *= scale ;
                _localOffset += clip.TimeRange.Start - clip.SourceRange.Start * scale;
            }
        } 
        
        #endregion

        public float NestedTimeScale => Scale.X * _localScale;
        public float NestedTimeOffset => (Scroll.X - _localOffset) / _localScale;

        private float _localScale = 1;
        private float _localOffset = 0;

        internal readonly Playback Playback;

        private readonly TimeRasterSwitcher _timeRasterSwitcher = new TimeRasterSwitcher();
        private readonly HorizontalRaster _horizontalRaster = new HorizontalRaster();
        private readonly PlaybackRange _playbackRange = new PlaybackRange();

        private readonly DopeSheetArea _dopeSheetArea;
        private readonly CurveEditArea _curveEditArea;
        private readonly TimeLineImage _timeLineImage = new TimeLineImage();

        private readonly CurrentTimeMarker _currentTimeMarker = new CurrentTimeMarker();
        private readonly ValueSnapHandler _snapHandler = new ValueSnapHandler();
        private readonly TimeSelectionFence _selectionFence;
        public readonly LayersArea LayersArea;

        public static TimeLineCanvas Current;

        private ImGuiIOPtr _io;
        private Vector2 _mouse;
        private Vector2 _scrollTarget = new Vector2(-25f, 0.0f);
        private Vector2 _scaleTarget = new Vector2(10, -1);
        private ImDrawListPtr _drawlist;

        // Styling
        public const float TimeLineDragHeight = 40;
    }
}