using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using t3.Gui.Audio;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Interaction.WithCurves;
using T3.Gui.Selection;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Combines multiple <see cref="ITimeObjectManipulation"/>s into a single consistent
    /// timeline that allows dragging selected time elements of various types.
    /// </summary>
    public class TimeLineCanvas : CurveEditCanvas
    {
        public TimeLineCanvas()
        {
            Playback = Playback.Current;
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


        public void Draw(Instance compositionOp)
        {
            Current = this;
            SelectedAnimationParameters = GetAnimationParametersForSelectedNodes(compositionOp);
            UpdateLocalTimeTranslation(compositionOp);
            ScrollToTimeAfterStopped();

            var modeChanged = UpdateMode();
            DrawCurveCanvas(drawAdditionalCanvasContent: DrawCanvasContent);

            void DrawCanvasContent()
            {
                if (SoundtrackUtils.TryFindingSoundtrack(compositionOp, out var soundtrack))
                {
                    _timeLineImage.Draw(Drawlist, soundtrack);
                }
                
                ImGui.SetScrollY(0);

                HandleDeferredActions();

                if (KeyboardBinding.Triggered(UserActions.DeleteSelection))
                    DeleteSelectedElements();

                _timeRasterSwitcher.Draw(Playback);

                switch (Mode)
                {
                    case Modes.DopeView:
                        LayersArea.Draw(compositionOp, Playback);
                        DopeSheetArea.Draw(compositionOp, SelectedAnimationParameters);
                        break;
                    case Modes.CurveEditor:
                        _horizontalRaster.Draw(this);
                        _timelineCurveEditArea.Draw(compositionOp, SelectedAnimationParameters, fitCurvesVertically: modeChanged);
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

                if (FenceState == SelectionFence.States.CompletedAsClick)
                {
                    var newTime = InverseTransformPositionFloat(ImGui.GetMousePos()).X;
                    if (Playback.IsLooping)
                    {
                        var newStartTime = newTime - newTime % 4;
                        var duration = Playback.LoopRange.Duration;
                        Playback.LoopRange.Start = newStartTime;
                        Playback.LoopRange.Duration = duration;
                    }
                    else
                    {
                        Playback.TimeInBars = newTime;
                    }
                }
            }
            Current = null;
        }



        #region handle nested timelines ----------------------------------
        private void UpdateLocalTimeTranslation(Instance compositionOp)
        {
            // if (UserScrolledCanvas) return;

            float originalScreenPos = 0f;
            float newScreenPos = 0f;
            float centerOfTimeRange = 0f;
            float centerOfSourceRange = 0f;

            _nestedTimeScale = 1f;
            _nestedTimeScroll = 0f;

            var compositionTimeClip = NodeOperations.GetCompositionTimeClip(compositionOp);
            if (compositionTimeClip != null)
            {
                centerOfTimeRange = (compositionTimeClip.TimeRange.Start +
                                     compositionTimeClip.TimeRange.End) * 0.5f;
                centerOfSourceRange = (compositionTimeClip.SourceRange.Start +
                                       compositionTimeClip.SourceRange.End) * 0.5f;
            }

            var parents = NodeOperations.GetParentInstances(compositionOp).ToList();
            parents.Insert(0, compositionOp);
            parents.Reverse();
            foreach (var p in parents)
            {
                if (p.Outputs.Count <= 0 || !(p.Outputs[0] is ITimeClipProvider timeClipProvider))
                    continue;

                var clip = timeClipProvider.TimeClip;
                var scale = clip.SourceRange.Duration / clip.TimeRange.Duration;
                centerOfTimeRange = (clip.TimeRange.Start +
                                     clip.TimeRange.End) * 0.5f;
                centerOfSourceRange = (clip.SourceRange.Start +
                                       clip.SourceRange.End) * 0.5f;

                originalScreenPos = TransformX(centerOfTimeRange);
                _nestedTimeScale /= scale;
                newScreenPos = TransformX(centerOfSourceRange);
                _nestedTimeScroll += InverseTransformDirection(new Vector2(newScreenPos - originalScreenPos, 0)).X
                                    / _nestedTimeScale;
            }

            ImGui.TextUnformatted($"localScale: {_nestedTimeScale}   localScroll: {_nestedTimeScroll}   " +
                                  $"Scroll: {Scroll.X}   Scale: {Scale.X}");
        }

        /// <summary>
        /// Override the default implementation to support time clip nesting 
        /// </summary>
        public override Vector2 TransformPositionFloat(Vector2 posOnCanvas)
        {
            var localScale = new Vector2(_nestedTimeScale, 1);
            var localScroll = new Vector2(_nestedTimeScroll, 0);

            // nested tranformation: transform to local coordinates first, then use outer tranformation
            var localPos = (posOnCanvas - localScroll) * localScale;
            return base.TransformPositionFloat(localPos);
        }

        public override Vector2 InverseTransformPositionFloat(Vector2 posOnScreen)
        {
            var localScale = new Vector2(_nestedTimeScale, 1);
            var localScroll = new Vector2(_nestedTimeScroll, 0);

            // nested tranformation: transform to local coordinates first, then invert inner tranformation
            var localPos = base.InverseTransformPositionFloat(posOnScreen);
            return localPos / localScale + localScroll;
        }

        public override void ZoomWithMouseWheel(Vector2 focusCenterOnScreen)
        {
            UserZoomedCanvas = false;

            //DrawCanvasDebugInfos();

            var zoomDelta = ComputeZoomDeltaFromMouseWheel();
            var clamped = ClampScaleToValidRange(ScaleTarget * zoomDelta);
            if (clamped == ScaleTarget)
                return;

            if (Math.Abs(zoomDelta - 1) < 0.001f)
                return;

            var zoom = zoomDelta * Vector2.One;
            if (IsCurveCanvas)
            {
                if (ImGui.GetIO().KeyAlt)
                {
                    zoom.X = 1;
                }
                else if (ImGui.GetIO().KeyShift)
                {
                    zoom.Y = 1;
                }
            }

            ScaleTarget *= zoom;

            if (Math.Abs(zoomDelta) > 0.1f)
                UserZoomedCanvas = true;

            var focusCenterOnCanvas = base.InverseTransformPositionFloat(focusCenterOnScreen);
            ScrollTarget += (focusCenterOnCanvas - ScrollTarget) * (zoomDelta - 1.0f) / zoom;
        }

        public new float NestedTimeScale => Scale.X / _nestedTimeScale;
        public new float NestedTimeScroll => Scroll.X * Scale.X + _nestedTimeScroll * NestedTimeScale;
        #endregion

        private void HandleDeferredActions()
        {
            if (UserActionRegistry.WasActionQueued(UserActions.PlaybackJumpToNextKeyframe))
            {
                var nextKeyframeTime = Double.PositiveInfinity;
                foreach (var next in SelectedAnimationParameters
                                    .SelectMany(animationParam => animationParam.Curves, (param, curve) => curve.GetNextU(Playback.TimeInBars + 0.001f))
                                    .Where<double?>(next => next != null && next.Value < nextKeyframeTime))
                {
                    nextKeyframeTime = next.Value;
                }

                if (!Double.IsPositiveInfinity(nextKeyframeTime))
                    Playback.TimeInBars = nextKeyframeTime;
            }

            if (UserActionRegistry.WasActionQueued(UserActions.PlaybackJumpToPreviousKeyframe))
            {
                var prevKeyframeTime = Double.NegativeInfinity;
                foreach (var next in SelectedAnimationParameters
                                    .SelectMany(animationParam => animationParam.Curves, (param, curve) => curve.GetPreviousU(Playback.TimeInBars - 0.001f))
                                    .Where<double?>(previous => previous != null && previous.Value > prevKeyframeTime))
                {
                    prevKeyframeTime = next.Value;
                }

                if (!Double.IsNegativeInfinity(prevKeyframeTime))
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

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left) || ImGui.IsItemClicked())
            {
                var draggedTime = InverseTransformX(Io.MousePos.X);
                if (ImGui.GetIO().KeyShift)
                {
                    SnapHandlerForU.CheckForSnapping(ref draggedTime, Scale.X, new List<IValueSnapAttractor> { _currentTimeMarker });
                }

                Playback.TimeInBars = draggedTime;
            }

            ImGui.SetCursorPos(Vector2.Zero);
        }

        private void ScrollToTimeAfterStopped()
        {
            var isPlaying = Math.Abs(Playback.PlaybackSpeed) > 0.01f;
            var wasPlaying = Math.Abs(_lastPlaybackSpeed) > 0.01f;

            if (!isPlaying && wasPlaying)
            {
                if (!IsCurrentTimeVisible())
                {
                    // assume we are not scrolling, what screen positino would the playhead be at?
                    var oldScroll = Scroll;
                    Scroll = new Vector2(0, Scroll.Y);
                    var posScreen = TransformX((float) Playback.TimeInBars);
                    // position that playhead in the center of the window
                    ScrollTarget.X = InverseTransformX(posScreen - WindowSize.X*0.5f);
                    // restore old state of scrolling
                    Scroll = oldScroll;
                }
            }

            _lastPlaybackSpeed = Playback.PlaybackSpeed;
        }

        private bool IsCurrentTimeVisible()
        {
            var timePosInScreen = TransformPosition(new Vector2((float)this.Playback.TimeInBars, 0));
            var timelineArea = ImRect.RectWithSize(WindowPos, WindowSize);
            timePosInScreen.Y = timelineArea.GetCenter().Y; // Adjust potential vertical scrolling of timeline area
            return timelineArea.Contains(timePosInScreen);
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

        
        
        // TODO: this is horrible and should be refactored
        private List<AnimationParameter> GetAnimationParametersForSelectedNodes(Instance compositionOp)
        {
            var selection = NodeSelection.GetSelectedNodes<ISelectableCanvasObject>();
            var symbolUi = SymbolUiRegistry.Entries[compositionOp.Symbol.Id];
            var animator = symbolUi.Symbol.Animator;
            
            // No Linq to avoid allocations
            _pinnedParams.Clear();
            foreach (Instance child in compositionOp.Children)
            foreach (var input in child.Inputs)
            {
                if (animator.IsInputSlotAnimated(input))
                    foreach (var pinnedInputSlot in DopeSheetArea.PinnedParameters)
                    {
                        if (pinnedInputSlot == input.GetHashCode())
                            _pinnedParams.Add(new AnimationParameter() { Instance = child, Input = input, Curves = animator.GetCurvesForInput(input), ChildUi = symbolUi.ChildUis.Single(childUi => childUi.Id == child.SymbolChildId) });
                    }
            }

            _curvesForSelection.Clear();
            
            foreach (Instance child in compositionOp.Children)
            foreach (var selectedElement in selection)
            {
                if (child.SymbolChildId == selectedElement.Id)
                    foreach (var input in child.Inputs)
                    {
                        if (animator.IsInputSlotAnimated(input))
                            _curvesForSelection.Add(new AnimationParameter() { Instance = child, Input = input, Curves = animator.GetCurvesForInput(input), ChildUi = symbolUi.ChildUis.Single(childUi => childUi.Id == selectedElement.Id) });
                    }
            }

            _pinnedParams.AddRange(_curvesForSelection.FindAll(sp => _pinnedParams.All(pp => pp.Input != sp.Input)));
            return _pinnedParams;
        }

        public List<AnimationParameter> SelectedAnimationParameters = new List<AnimationParameter>();



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

        private float _nestedTimeScale = 1f;
        private float _nestedTimeScroll = 0f;
        private double _lastPlaybackSpeed;
        private readonly List<AnimationParameter> _pinnedParams = new(20);
        private List<AnimationParameter> _curvesForSelection = new(64);

        // Styling
        public const float TimeLineDragHeight = 30;

        public struct AnimationParameter
        {
            public IEnumerable<Curve> Curves;
            public IInputSlot Input;
            public Instance Instance;
            public SymbolChildUi ChildUi;
        }
    }
}