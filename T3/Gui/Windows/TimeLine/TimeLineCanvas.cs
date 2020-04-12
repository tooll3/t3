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
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public class TimeLineCanvas : CurveCanvas, ITimeObjectManipulation
    {
        public TimeLineCanvas(Playback playback = null)
        {
            Playback = playback;
            _dopeSheetArea = new DopeSheetArea(_snapHandler, this);
            _selectionFence = new TimeSelectionFence(this);
            _timelineCurveEditArea = new TimelineCurveEditArea(this, _snapHandler);
            _timeSelectionRange = new TimeSelectionRange(this, _snapHandler);
            LayersArea = new LayersArea(_snapHandler);

            _snapHandler.AddSnapAttractor(_clipRange);
            _snapHandler.AddSnapAttractor(_loopRange);
            _snapHandler.AddSnapAttractor(_timeRasterSwitcher);
            _snapHandler.AddSnapAttractor(_currentTimeMarker);
            _snapHandler.AddSnapAttractor(_timeSelectionRange);
            _snapHandler.AddSnapAttractor(LayersArea);

            _snapHandler.SnappedEvent += SnappedEventHandler;
        }

        public bool FoundTimeClipForCurrentTime => LayersArea.FoundClipWithinCurrentTime;

        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters)
        {
            Current = this;
            UpdateLocalTimeTranslation(compositionOp);

            _drawlist = ImGui.GetWindowDrawList();

            Update();

            var modeChanged = UpdateMode();

            ImGui.BeginChild("timeline", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                _drawlist = ImGui.GetWindowDrawList();
                _timeLineImage.Draw(_drawlist, Playback);
                ImGui.SetScrollY(0);

                HandleDeferredActions(animationParameters);
                HandleInteraction();

                if (KeyboardBinding.Triggered(UserActions.DeleteSelection))
                    DeleteSelectedElements();

                _timeRasterSwitcher.Draw(Playback);
                DrawSnapIndicator();

                switch (Mode)
                {
                    case Modes.DopeView:
                        LayersArea.Draw(compositionOp, Playback);
                        _dopeSheetArea.Draw(compositionOp, animationParameters);
                        break;
                    case Modes.CurveEditor:
                        _horizontalRaster.Draw(this);
                        _timelineCurveEditArea.Draw(compositionOp, animationParameters, bringCurvesIntoView: modeChanged);
                        break;
                }

                var compositionTimeClip = NodeOperations.GetCompositionTimeClip(compositionOp);

                if (Playback.IsLooping)
                {
                    _loopRange.Draw(this, Playback, _drawlist, _snapHandler);
                }
                else if (compositionTimeClip != null)
                {
                    _clipRange.Draw(this, compositionTimeClip, _drawlist, _snapHandler);
                }

                _timeSelectionRange.Draw(_drawlist);

                _currentTimeMarker.Draw(Playback);
                DrawDragTimeArea();
                _selectionFence.Draw();
            }
            ImGui.EndChild();
        }

        #region handle nested timelines ----------------------------------
        private void UpdateLocalTimeTranslation(Instance compositionOp)
        {
            _localTimeScale = 1f;
            _localTimeOffset = 0f;
            ;
            var parents = GraphCanvas.Current.GetParents().Reverse().ToList();
            parents.Add(compositionOp);
            foreach (var p in parents)
            {
                if (p.Outputs.Count <= 0 || !(p.Outputs[0] is ITimeClipProvider timeClipProvider))
                    continue;

                var clip = timeClipProvider.TimeClip;
                var scale = clip.TimeRange.Duration / clip.SourceRange.Duration;
                _localTimeScale *= scale;
                _localTimeOffset += clip.TimeRange.Start - clip.SourceRange.Start * scale;
            }
        }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public override float TransformPositionX(float xOnCanvas)
        {
            var scale = Scale.X * _localTimeScale;
            var offset = (Scroll.X - _localTimeOffset) / _localTimeScale;
            return (int)((xOnCanvas - offset) * scale + WindowPos.X);
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public override float InverseTransformPositionX(float xOnScreen)
        {
            var scale = Scale.X * _localTimeScale;
            var offset = (Scroll.X - _localTimeOffset) / _localTimeScale;
            return (xOnScreen - WindowPos.X) / scale + offset;
        }

        public float TransformGlobalTime(float time)
        {
            var localTime = (time - _localTimeOffset) / _localTimeScale;
            return TransformPositionX(localTime);
        }
        #endregion

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

        private void DrawDragTimeArea()
        {
            if (Playback == null)
                return;

            var max = ImGui.GetContentRegionMax();
            var clampedSize = max;
            clampedSize.Y = Im.Min(TimeLineDragHeight, max.Y - 1);

            ImGui.SetCursorPos(new Vector2(0, max.Y - clampedSize.Y));
            var screenPos = ImGui.GetCursorScreenPos();
            ImGui.GetWindowDrawList().AddRectFilled(screenPos, screenPos + new Vector2(clampedSize.X, clampedSize.Y), new Color(0, 0, 0, 0.1f));

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

        #region implement ISelectionHolder
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

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragAtStartPointCommand(dt, dv);
            }
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragAtEndPointCommand(dt, dv);
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

        private readonly List<ITimeObjectManipulation> _selectionHolders = new List<ITimeObjectManipulation>();
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
                    _selectionHolders.Remove(_timelineCurveEditArea);
                    _snapHandler.RemoveSnapAttractor(_timelineCurveEditArea);
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
                    _selectionHolders.Add(_timelineCurveEditArea);
                    _snapHandler.AddSnapAttractor(_timelineCurveEditArea);
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

        public float NestedTimeScale => Scale.X * _localTimeScale;
        public float NestedTimeOffset => (Scroll.X - _localTimeOffset) / _localTimeScale;

        internal readonly Playback Playback;

        private readonly TimeRasterSwitcher _timeRasterSwitcher = new TimeRasterSwitcher();
        private readonly HorizontalRaster _horizontalRaster = new HorizontalRaster();
        private readonly ClipRange _clipRange = new ClipRange();
        private readonly LoopRange _loopRange = new LoopRange();

        private readonly DopeSheetArea _dopeSheetArea;
        private readonly TimelineCurveEditArea _timelineCurveEditArea;
        private readonly TimeLineImage _timeLineImage = new TimeLineImage();

        private readonly CurrentTimeMarker _currentTimeMarker = new CurrentTimeMarker();
        private readonly ValueSnapHandler _snapHandler = new ValueSnapHandler();
        private readonly TimeSelectionFence _selectionFence;
        private readonly TimeSelectionRange _timeSelectionRange;
        public readonly LayersArea LayersArea;

        public static TimeLineCanvas Current;

        private float _localTimeScale = 1;
        private float _localTimeOffset = 0;

        private ImDrawListPtr _drawlist;

        // Styling
        public const float TimeLineDragHeight = 40;
    }
}