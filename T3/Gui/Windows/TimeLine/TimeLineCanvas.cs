using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Interaction.WithCurves;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Combines multiple <see cref="ITimeObjectManipulation"/>s into a single consistent
    /// timeline that allows dragging selected time elements of various types.
    /// </summary>
    public class TimeLineCanvas : CurveEditCanvas
    {
        public TimeLineCanvas(ref Playback playback)
        {
            Playback = playback;
            DopeSheetArea = new DopeSheetArea(SnapHandlerForU, this);
            _timelineCurveEditArea = new TimelineCurveEditArea(this, SnapHandlerForU, SnapHandlerForV);
            _timeSelectionRange = new TimeSelectionRange(this, SnapHandlerForU);
            LayersArea = new LayersArea(SnapHandlerForU);

            SnapHandlerForV.AddSnapAttractor(_horizontalRaster);
            SnapHandlerForU.AddSnapAttractor(_clipRange);
            SnapHandlerForU.AddSnapAttractor(_loopRange);
            SnapHandlerForU.AddSnapAttractor(_timeRasterSwitcher);
            SnapHandlerForU.AddSnapAttractor(_currentTimeMarker);
            //SnapHandlerForU.AddSnapAttractor(_timeSelectionRange);
            SnapHandlerForU.AddSnapAttractor(LayersArea);
        }

        public bool FoundTimeClipForCurrentTime => LayersArea.FoundClipWithinCurrentTime;
        
        
        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters)
        {
            Current = this;
            UpdateLocalTimeTranslation(compositionOp);
            
            var modeChanged = UpdateMode();
            DrawCurveCanvas(drawAdditionalCanvasContent:DrawCanvasContent);

            void DrawCanvasContent()
            {
                _timeLineImage.Draw(Drawlist, Playback);
                ImGui.SetScrollY(0);

                HandleDeferredActions(animationParameters);

                if (KeyboardBinding.Triggered(UserActions.DeleteSelection))
                    DeleteSelectedElements();

                _timeRasterSwitcher.Draw(Playback);

                switch (Mode)
                {
                    case Modes.DopeView:
                        LayersArea.Draw(compositionOp, Playback);
                        DopeSheetArea.Draw(compositionOp, animationParameters);
                        break;
                    case Modes.CurveEditor:
                        _horizontalRaster.Draw(this);
                        _timelineCurveEditArea.Draw(compositionOp, animationParameters, bringCurvesIntoView: modeChanged);
                        break;
                }

                var compositionTimeClip = NodeOperations.GetCompositionTimeClip(compositionOp);

                if (Playback.IsLooping)
                {
                    _loopRange.Draw(this, Playback, Drawlist, SnapHandlerForU);
                }
                else if (compositionTimeClip != null)
                {
                    _clipRange.Draw(this, compositionTimeClip, Drawlist, SnapHandlerForU);
                }

                _timeSelectionRange.Draw(Drawlist);

                _currentTimeMarker.Draw(Playback);
                DrawDragTimeArea();
            }            

        }


        #region handle nested timelines ----------------------------------
        private void UpdateLocalTimeTranslation(Instance compositionOp)
        {
            _nestedTimeScale = 1f;
            _nestedTimeOffset = 0f;
            
            var parents = GraphCanvas.GetParents(compositionOp).Reverse().ToList();
            parents.Add(compositionOp);
            foreach (var p in parents)
            {
                if (p.Outputs.Count <= 0 || !(p.Outputs[0] is ITimeClipProvider timeClipProvider))
                    continue;

                var clip = timeClipProvider.TimeClip;
                var scale = clip.TimeRange.Duration / clip.SourceRange.Duration;
                _nestedTimeScale *= scale;
                _nestedTimeOffset += clip.TimeRange.Start - clip.SourceRange.Start * scale;
            }
            
            // ImGui.Text($"localScale: {_nestedTimeScale}   localScroll: {_nestedTimeOffset}");
        }

        
        /// <summary>
        /// Override the default implement to support time clip nesting 
        /// </summary>
        public override Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            var localScale = new Vector2(_nestedTimeScale,1);
            var localScroll = new Vector2(_nestedTimeOffset,0);
            
            return (posOnCanvas * localScale + localScroll) * Scale + Scroll + WindowPos;
        }
        
        
        public override Vector2 InverseTransformPosition(Vector2 posOnScreen)
        {
            var localScale = new Vector2(_nestedTimeScale,1);
            var localScroll = new Vector2(_nestedTimeOffset,0);
            
            return (posOnScreen- localScroll * Scale - Scroll - WindowPos) / (localScale * Scale);
        }
        

        public float TransformGlobalTime(float time)
        {
            return base.TransformPosition(new Vector2(time, 0)).X;
        }
        
        public float NestedTimeScale => Scale.X * _nestedTimeScale;
        public float NestedTimeOffset => (Scroll.X - _nestedTimeOffset) + _nestedTimeOffset;        
        
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
            clampedSize.Y = Math.Min(TimeLineDragHeight, max.Y - 1);

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
                var draggedTime = InverseTransformX(Io.MousePos.X);
                if (ImGui.GetIO().KeyShift)
                {
                    SnapHandlerForU.CheckForSnapping(ref draggedTime, Scale.X, new List<IValueSnapAttractor> { _currentTimeMarker});
                }

                Playback.TimeInBars = draggedTime;
            }

            ImGui.SetCursorPos(Vector2.Zero);
        }

        
        #region view modes
        private bool UpdateMode()
        {
            if (Mode == _lastMode)
                return false;

            switch (_lastMode)
            {
                case Modes.DopeView:
                    TimeObjectManipulators.Remove(DopeSheetArea);
                    TimeObjectManipulators.Remove(LayersArea);
                    SnapHandlerForU.RemoveSnapAttractor(DopeSheetArea);
                    break;

                case Modes.CurveEditor:
                    TimeObjectManipulators.Remove(_timelineCurveEditArea);
                    SnapHandlerForU.RemoveSnapAttractor(_timelineCurveEditArea);
                    break;
            }

            switch (Mode)
            {
                case Modes.DopeView:
                    TimeObjectManipulators.Add(DopeSheetArea);
                    TimeObjectManipulators.Add(LayersArea);
                    SnapHandlerForU.AddSnapAttractor(DopeSheetArea);
                    break;

                case Modes.CurveEditor:
                    TimeObjectManipulators.Add(_timelineCurveEditArea);
                    SnapHandlerForU.AddSnapAttractor(_timelineCurveEditArea);
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



        internal readonly Playback Playback;

        private readonly TimeRasterSwitcher _timeRasterSwitcher = new TimeRasterSwitcher();
        private readonly HorizontalRaster _horizontalRaster = new HorizontalRaster();
        private readonly ClipRange _clipRange = new ClipRange();
        private readonly LoopRange _loopRange = new LoopRange();

        public readonly DopeSheetArea DopeSheetArea;
        private readonly TimelineCurveEditArea _timelineCurveEditArea;
        private readonly TimeLineImage _timeLineImage = new TimeLineImage();

        private readonly CurrentTimeMarker _currentTimeMarker = new CurrentTimeMarker();
        private readonly TimeSelectionRange _timeSelectionRange;
        public readonly LayersArea LayersArea;

        public static TimeLineCanvas Current;

        private float _nestedTimeScale = 1;
        private float _nestedTimeOffset = 0;

        

        // Styling
        public const float TimeLineDragHeight = 40;
    }
}