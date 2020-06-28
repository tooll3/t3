using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Selection;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Shows a list of Layers with <see cref="TimeClip"/>s
    /// </summary>
    public class LayersArea : ITimeObjectManipulation, IValueSnapAttractor
    {
        public LayersArea(ValueSnapHandler snapHandler)
        {
            _snapHandler = snapHandler;
        }

        public void Draw(Instance compositionOp, Playback playback)
        {
            _drawList = ImGui.GetWindowDrawList();
            _compositionOp = compositionOp;
            _playback = playback;

            ImGui.BeginGroup();
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, 3)); // keep some padding 
                _minScreenPos = ImGui.GetCursorScreenPos();
                var allTimeClips = NodeOperations.GetAllTimeClips(_compositionOp);
                DrawAllLayers(allTimeClips);
                DrawContextMenu();
            }
            ImGui.EndGroup();
        }

        private bool _contextMenuIsOpen;

        private void DrawContextMenu()
        {
            if (!_contextMenuIsOpen && !ImGui.IsWindowHovered())
                return;

            if (SelectedItems.Count == 0)
                return;

            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!_contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                _contextMenuIsOpen = true;
                if (ImGui.MenuItem("Delete", null, false, SelectedItems.Count > 0))
                {
                    UndoRedoStack.AddAndExecute(new TimeClipDeleteCommand(_compositionOp, SelectedItems));
                    SelectedItems.Clear();
                }

                if (ImGui.MenuItem("Clear Time Stretch", null, false, SelectedItems.Count > 0))
                {
                    var moveTimeClipCommand = new MoveTimeClipsCommand(_compositionOp, SelectedItems.ToList());
                    foreach (var clip in SelectedItems)
                    {
                        clip.SourceRange = clip.TimeRange.Clone();
                    }

                    moveTimeClipCommand.StoreCurrentValues();
                    UndoRedoStack.AddAndExecute(moveTimeClipCommand);
                    SelectedItems.Clear();
                }

                ImGui.EndPopup();
            }
            else
            {
                _contextMenuIsOpen = false;
            }

            ImGui.PopStyleVar();
        }

        private int _minLayerIndex = int.MaxValue;
        private int _maxLayerIndex = int.MinValue;

        public float LastHeight;

        private void DrawAllLayers(List<ITimeClip> clips)
        {
            FoundClipWithinCurrentTime = false;
            if (clips.Count == 0)
                return;

            _minLayerIndex = int.MaxValue;
            _maxLayerIndex = int.MinValue;

            foreach (var clip in clips)
            {
                _minLayerIndex = Math.Min(clip.LayerIndex, _minLayerIndex);
                _maxLayerIndex = Math.Max(clip.LayerIndex, _maxLayerIndex);
            }

            // Draw layer lines
            var min = ImGui.GetCursorScreenPos();
            var max = min + new Vector2(ImGui.GetContentRegionAvail().X, LayerHeight * (_maxLayerIndex - _minLayerIndex + 1) + 1);
            var layerArea = new ImRect(min, max);
            LastHeight = max.Y - min.Y;
            
            _drawList.AddRectFilled(new Vector2(min.X, max.Y - 2),
                                    new Vector2(max.X, max.Y - 1), new Color(0, 0, 0, 0.4f));
            
            foreach (var clip in clips)
            {
                DrawClip(clip, layerArea, _minLayerIndex);
            }
            
            ImGui.SetCursorScreenPos(min + new Vector2(0, LayerHeight));
        }

        /// <summary>
        ///  This will updated during redraw and thus has one frame delay
        /// </summary>
        public bool FoundClipWithinCurrentTime;

        private void DrawClip(ITimeClip timeClip, ImRect layerArea, int minLayerIndex)
        {
            FoundClipWithinCurrentTime |= timeClip.TimeRange.Contains(_playback.TimeInBars);

            var xStartTime = TimeLineCanvas.Current.TransformX(timeClip.TimeRange.Start) + 1;
            var xEndTime = TimeLineCanvas.Current.TransformX(timeClip.TimeRange.End);
            var position = new Vector2(xStartTime,
                                       layerArea.Min.Y + (timeClip.LayerIndex - minLayerIndex) * LayerHeight);

            var clipWidth = xEndTime - xStartTime;
            var showSizeHandles = clipWidth > 4 * HandleWidth;
            var bodyWidth = showSizeHandles
                                ? (clipWidth - 2 * HandleWidth)
                                : clipWidth;

            var bodySize = new Vector2(bodyWidth, LayerHeight);
            var clipSize = new Vector2(clipWidth, LayerHeight - 1);

            var symbolUi = SymbolUiRegistry.Entries[_compositionOp.Symbol.Id];
            var symbolChildUi = symbolUi.ChildUis.Single(child => child.Id == timeClip.Id);

            ImGui.PushID(symbolChildUi.Id.GetHashCode());

            var isSelected = SelectedItems.Contains(timeClip);
            //symbolChildUi.IsSelected = isSelected;

            var color = new Color(0.5f);
            _drawList.AddRectFilled(position, position + clipSize - new Vector2(1, 0), color);

            var timeRemapped = timeClip.TimeRange != timeClip.SourceRange;
            var timeStretched = Math.Abs(timeClip.TimeRange.Duration - timeClip.SourceRange.Duration) > 0.001;
            if (timeStretched)
            {
                _drawList.AddRectFilled(position, position + new Vector2(clipSize.X - 1, 2), Color.Red);
            }
            else if (timeRemapped)
            {
                _drawList.AddRectFilled(position, position + new Vector2(clipSize.X - 1, 2), Color.Orange);
            }

            if (isSelected)
                _drawList.AddRect(position - Vector2.One, position + clipSize - new Vector2(1, 0) + Vector2.One, Color.White);

            ImGui.PushClipRect(position, position + clipSize - new Vector2(1, 0), true);
            var label = timeStretched
                            ? symbolChildUi.SymbolChild.ReadableName + $" ({GetSpeed(timeClip)}%)"
                            : symbolChildUi.SymbolChild.ReadableName;
            ImGui.PushFont(Fonts.FontSmall);
            _drawList.AddText(position + new Vector2(4, 1), isSelected ? Color.White : Color.Black, label);
            ImGui.PopFont();
            ImGui.PopClipRect();

            ImGui.SetCursorScreenPos(showSizeHandles ? (position + _handleOffset) : position);

            var wasClicked = ImGui.InvisibleButton("body", bodySize);

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                {
                    ImGui.Text($"In: {timeClip.TimeRange.Start}");
                    ImGui.Text($"Out: {timeClip.TimeRange.End}");
                    if (timeRemapped)
                    {
                        ImGui.Text($"Source In: {timeClip.SourceRange.Start}");
                        ImGui.Text($"Source Out: {timeClip.SourceRange.End}");
                    }

                    if (timeStretched)
                    {
                        var speed = GetSpeed(timeClip);
                        ImGui.Text($"Speed: {speed:0.}%");
                    }
                }
                ImGui.EndTooltip();
            }

            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
            {
                var instance = _compositionOp.Children.Single(child => child.SymbolChildId == symbolChildUi.Id);
                SelectionManager.SetSelection(symbolChildUi, instance);
                SelectionManager.FitViewToSelection();
                SelectedItems.Clear();
                SelectedItems.Add(timeClip);
            }

            if (ImGui.IsItemHovered())
            {
                T3Ui.AddHoveredId(symbolChildUi.Id);
            }

            var notClickingOrDragging = !ImGui.IsItemActive() && !ImGui.IsMouseDragging(ImGuiMouseButton.Left);
            if (notClickingOrDragging && _moveClipsCommand != null)
            {
                TimeLineCanvas.Current.CompleteDragCommand();

                if (_moveClipsCommand != null)
                {
                    _moveClipsCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveClipsCommand);
                    _moveClipsCommand = null;
                }
            }

            HandleDragging(timeClip, isSelected, wasClicked, HandleDragMode.Body, position);

            var handleSize = showSizeHandles ? new Vector2(HandleWidth, LayerHeight) : Vector2.One;

            ImGui.SetCursorScreenPos(position);
            var aHandleClicked = ImGui.InvisibleButton("startHandle", handleSize);
            HandleDragging(timeClip, isSelected, false, HandleDragMode.Start, position);

            ImGui.SetCursorScreenPos(position + new Vector2(bodyWidth + HandleWidth, 0));
            aHandleClicked |= ImGui.InvisibleButton("endHandle", handleSize);
            HandleDragging(timeClip, isSelected, false, HandleDragMode.End, position);

            if (aHandleClicked)
            {
                TimeLineCanvas.Current.CompleteDragCommand();

                if (_moveClipsCommand != null)
                {
                    _moveClipsCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveClipsCommand);
                    _moveClipsCommand = null;
                }
            }

            ImGui.PopID();
        }

        private static double GetSpeed(ITimeClip timeClip)
        {
            return Math.Abs(timeClip.TimeRange.Duration) > 0.001
                       ? Math.Round((timeClip.TimeRange.Duration / timeClip.SourceRange.Duration) * 100)
                       : 9999;
        }

        private enum HandleDragMode
        {
            Body = 0,
            Start,
            End,
        }

        //private float _dragStartedAtTime;
        private float _timeWithinDraggedClip;
        private float _posYInsideDraggedClip;

        private void HandleDragging(ITimeClip timeClip, bool isSelected, bool wasClicked, HandleDragMode mode, Vector2 position)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(mode == HandleDragMode.Body
                                         ? ImGuiMouseCursor.Hand
                                         : ImGuiMouseCursor.ResizeEW);
            }

            if (!wasClicked && (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 1f)))
                return;

            if (ImGui.GetIO().KeyCtrl)
            {
                if (isSelected)
                    SelectedItems.Remove(timeClip);

                return;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                }

                SelectedItems.Add(timeClip);
            }

            var mousePos = ImGui.GetIO().MousePos;
            if (_moveClipsCommand == null)
            {
                var dragStartedAtTime = TimeLineCanvas.Current.InverseTransformX(mousePos.X);
                _timeWithinDraggedClip = dragStartedAtTime - timeClip.TimeRange.Start;
                _posYInsideDraggedClip = mousePos.Y - position.Y;
                TimeLineCanvas.Current.StartDragCommand();
            }

            switch (mode)
            {
                case HandleDragMode.Body:
                    var currentDragTime = TimeLineCanvas.Current.InverseTransformX(mousePos.X);

                    var newStartTime = currentDragTime - _timeWithinDraggedClip;

                    var newDragPosY = mousePos.Y - position.Y;
                    var dy = _posYInsideDraggedClip - newDragPosY;

                    if (_snapHandler.CheckForSnapping(ref newStartTime, TimeLineCanvas.Current.Scale.X))
                    {
                        TimeLineCanvas.Current.UpdateDragCommand(newStartTime - timeClip.TimeRange.Start, dy);
                        return;
                    }

                    var newEndTime = newStartTime + timeClip.TimeRange.Duration;
                    _snapHandler.CheckForSnapping(ref newEndTime, TimeLineCanvas.Current.Scale.X);

                    TimeLineCanvas.Current.UpdateDragCommand(newEndTime - timeClip.TimeRange.End, dy);
                    break;

                case HandleDragMode.Start:
                    var newDragStartTime = TimeLineCanvas.Current.InverseTransformX(mousePos.X);
                    _snapHandler.CheckForSnapping(ref newDragStartTime, TimeLineCanvas.Current.Scale.X);
                    TimeLineCanvas.Current.UpdateDragAtStartPointCommand(newDragStartTime - timeClip.TimeRange.Start, 0);
                    break;

                case HandleDragMode.End:
                    var newDragTime = TimeLineCanvas.Current.InverseTransformX(mousePos.X);
                    _snapHandler.CheckForSnapping(ref newDragTime, TimeLineCanvas.Current.Scale.X);

                    TimeLineCanvas.Current.UpdateDragAtEndPointCommand(newDragTime - timeClip.TimeRange.End, 0);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        #region implement interface --------------------------------------------
        void ITimeObjectManipulation.ClearSelection()
        {
            SelectedItems.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            if (selectMode == SelectionFence.SelectModes.Replace)
                SelectedItems.Clear();

            var startTime = TimeLineCanvas.Current.InverseTransformX(screenArea.Min.X);
            var endTime = TimeLineCanvas.Current.InverseTransformX(screenArea.Max.X);

            var layerMinIndex = (screenArea.Min.Y - _minScreenPos.Y) / LayerHeight + _minLayerIndex;
            var layerMaxIndex = (screenArea.Max.Y - _minScreenPos.Y) / LayerHeight + _minLayerIndex;

            var allClips = NodeOperations.GetAllTimeClips(_compositionOp);

            var matchingClips = allClips.FindAll(clip => clip.TimeRange.Start <= endTime
                                                         && clip.TimeRange.End >= startTime
                                                         && clip.LayerIndex <= layerMaxIndex
                                                         && clip.LayerIndex >= layerMinIndex - 1);
            switch (selectMode)
            {
                case SelectionFence.SelectModes.Add:
                case SelectionFence.SelectModes.Replace:
                    SelectedItems.UnionWith(matchingClips);
                    break;
                case SelectionFence.SelectModes.Remove:
                    SelectedItems.ExceptWith(matchingClips);
                    break;
            }
        }

        ICommand ITimeObjectManipulation.StartDragCommand()
        {
            _moveClipsCommand = new MoveTimeClipsCommand(_compositionOp, SelectedItems.ToList());
            return _moveClipsCommand;
        }

        void ITimeObjectManipulation.UpdateDragCommand(double dt, double dy)
        {
            var dragContent = ImGui.GetIO().KeyAlt;

            foreach (var clip in SelectedItems)
            {
                if (dragContent)
                {
                    //TODO: fix continuous dragging
                    clip.SourceRange.Start += (float)dt;
                    clip.SourceRange.End += (float)dt;
                }
                else
                {
                    clip.TimeRange.Start += (float)dt;
                    clip.TimeRange.End += (float)dt;
                }

                if (clip.LayerIndex > _minLayerIndex && dy > LayerHeight)
                {
                    clip.LayerIndex--;
                }
                else if (clip.LayerIndex == _minLayerIndex && dy > LayerHeight + 20)
                {
                    clip.LayerIndex--;
                    _posYInsideDraggedClip -= 10;
                }
                else if (dy < -LayerHeight)
                {
                    clip.LayerIndex++;
                }
            }
        }

        void ITimeObjectManipulation.UpdateDragAtStartPointCommand(double dt, double dv)
        {
            var trim = !ImGui.GetIO().KeyAlt;
            foreach (var clip in SelectedItems)
            {
                // Keep 1 frame min duration
                var org = clip.TimeRange.Start;
                clip.TimeRange.Start = (float)Math.Min(clip.TimeRange.Start + dt, clip.TimeRange.End - MinDuration);
                var d = clip.TimeRange.Start - org;
                if (trim)
                    clip.SourceRange.Start += d;
            }
        }

        void ITimeObjectManipulation.UpdateDragAtEndPointCommand(double dt, double dv)
        {
            var trim = !ImGui.GetIO().KeyAlt;
            foreach (var clip in SelectedItems)
            {
                // Keep 1 frame min duration
                var org = clip.TimeRange.End;
                clip.TimeRange.End = (float)Math.Max(clip.TimeRange.End + dt, clip.TimeRange.Start + MinDuration);
                var d = clip.TimeRange.End - org;
                if (trim)
                    clip.SourceRange.End += d;
            }
        }

        void ITimeObjectManipulation.UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var clip in SelectedItems)
            {
                clip.TimeRange.Start = (float)(originU + (clip.TimeRange.Start - originU) * scaleU);
                clip.TimeRange.End = (float)Math.Max(originU + (clip.TimeRange.End - originU) * scaleU, clip.TimeRange.Start + MinDuration);
            }
        }

        private const float MinDuration = 1 / 60f; // In bars

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = TimeRange.Undefined;
            foreach (var s in SelectedItems)
            {
                timeRange.Unite(s.TimeRange.Start);
                timeRange.Unite(s.TimeRange.End);
            }

            return timeRange;
        }

        void ITimeObjectManipulation.CompleteDragCommand()
        {
            if (_moveClipsCommand == null)
                return;

            // Update reference in macro-command 
            _moveClipsCommand.StoreCurrentValues();
            // UndoRedoStack.Add(_moveClipsCommand);
            _moveClipsCommand = null;
        }

        void ITimeObjectManipulation.DeleteSelectedElements()
        {
            //TODO: Implement
        }
        #endregion

        #region implement snapping interface -----------------------------------
        /// <summary>
        /// Snap to all non-selected Clips
        /// </summary>
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime, float canvasScale)
        {
            SnapResult bestSnapResult = null;
                
            var allClips = NodeOperations.GetAllTimeClips(_compositionOp);

            foreach (var clip in allClips)
            {
                if (SelectedItems.Contains(clip))
                    continue;

                ValueSnapHandler.CheckForBetterSnapping(targetTime, clip.TimeRange.Start, canvasScale, ref bestSnapResult);
                ValueSnapHandler.CheckForBetterSnapping(targetTime, clip.TimeRange.End, canvasScale, ref bestSnapResult);
            }

            return bestSnapResult;
        }
        #endregion

        private Vector2 _minScreenPos;

        public readonly HashSet<ITimeClip> SelectedItems = new HashSet<ITimeClip>();
        private static MoveTimeClipsCommand _moveClipsCommand;
        private const int LayerHeight = 18;
        private const float HandleWidth = 5;
        private readonly Vector2 _handleOffset = new Vector2(HandleWidth, 0);

        private ImDrawListPtr _drawList;
        private Instance _compositionOp;
        private readonly ValueSnapHandler _snapHandler;
        private Playback _playback;
    }
}