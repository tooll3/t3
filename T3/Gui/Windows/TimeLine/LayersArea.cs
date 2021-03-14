using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
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
                ClipSelection.UpdateForComposition(compositionOp);
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, 3)); // keep some padding 
                _minScreenPos = ImGui.GetCursorScreenPos();
                DrawAllLayers(ClipSelection.AllClips);
                DrawContextMenu();
            }
            ImGui.EndGroup();
        }

        private bool _contextMenuIsOpen;

        private void DrawContextMenu()
        {
            if (!_contextMenuIsOpen && !ImGui.IsWindowHovered())
                return;

            if (ClipSelection.Count == 0)
                return;

            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!_contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                _contextMenuIsOpen = true;
                if (ImGui.MenuItem("Delete", null, false, ClipSelection.Count > 0))
                {
                    UndoRedoStack.AddAndExecute(new TimeClipDeleteCommand(_compositionOp, ClipSelection.SelectedClips));
                    ClipSelection.Clear();
                }

                if (ImGui.MenuItem("Clear Time Stretch", null, false, ClipSelection.Count > 0))
                {
                    var moveTimeClipCommand = new MoveTimeClipsCommand(_compositionOp, ClipSelection.SelectedClips.ToList());
                    foreach (var clip in ClipSelection.SelectedClips)
                    {
                        clip.SourceRange = clip.TimeRange.Clone();
                    }

                    moveTimeClipCommand.StoreCurrentValues();
                    UndoRedoStack.AddAndExecute(moveTimeClipCommand);
                    ClipSelection.Clear();
                }

                if (ImGui.MenuItem("Cut at time"))
                {
                    ImGui.SameLine();

                    var timeInBars = _playback.TimeInBars;
                    var matchingClips = ClipSelection.AllClips.Where(clip => clip.TimeRange.Contains(timeInBars));

                    foreach (var clip in matchingClips)
                    {
                        var compositionSymbolUi = SymbolUiRegistry.Entries[_compositionOp.Symbol.Id];
                        var symbolChildUi = compositionSymbolUi.ChildUis.Single(child => child.Id == clip.Id);

                        Vector2 newPos = symbolChildUi.PosOnCanvas;
                        newPos.Y += symbolChildUi.Size.Y + 5.0f;
                        var cmd = new CopySymbolChildrenCommand(compositionSymbolUi, new[] { symbolChildUi }, compositionSymbolUi, newPos);
                        cmd.Do();

                        // Set new end to the original time clip
                        float orgTimeRangeEnd = clip.TimeRange.End;
                        float originalSourceDuration = clip.SourceRange.Duration;
                        float normalizedCutPosition = ((float)_playback.TimeInBars - clip.TimeRange.Start) / clip.TimeRange.Duration;
                        
                        clip.TimeRange.End = (float)_playback.TimeInBars;// = new TimeRange(clip.TimeRange.Start, (float)_playback.TimeInBars);


                        // Apply new time range to newly added instance
                        Guid newChildId = cmd.OldToNewIdDict[clip.Id];
                        var newInstance = _compositionOp.Children.Single(child => child.SymbolChildId == newChildId);
                        var newTimeClip = newInstance.Outputs.OfType<ITimeClipProvider>().Single().TimeClip;
                        
                        //newTimeClip.TimeRange.Start = (float)_playback.TimeInBars; // = new TimeRange((float)_playback.TimeInBars, originalEndTime);
                        newTimeClip.TimeRange = new TimeRange((float)_playback.TimeInBars, orgTimeRangeEnd);
                        newTimeClip.SourceRange.Start = newTimeClip.SourceRange.Start + originalSourceDuration * normalizedCutPosition;
                        newTimeClip.SourceRange.End = clip.SourceRange.End;

                        clip.SourceRange.End = originalSourceDuration * normalizedCutPosition;
                    }
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

        private void DrawAllLayers(IReadOnlyCollection<ITimeClip> clips)
        {
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

        private void DrawClip(ITimeClip timeClip, ImRect layerArea, int minLayerIndex)
        {

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

            var isSelected = ClipSelection.SelectedClips.Contains(timeClip);


            var color = new Color(0.8f, 0.8f, 0.4f, 0.4f);
            _drawList.AddRectFilled(position, position + clipSize - new Vector2(1, 0), color);

            var timeRemapped = timeClip.TimeRange != timeClip.SourceRange;
            var timeStretched = Math.Abs(timeClip.TimeRange.Duration - timeClip.SourceRange.Duration) > 0.001;
            if (timeStretched)
            {
                _drawList.AddRectFilled(position + new Vector2(0, clipSize.Y - 2), 
                                        position + new Vector2(clipSize.X - 1, clipSize.Y), 
                                        Color.Red);
            }
            else if (timeRemapped)
            {
                _drawList.AddRectFilled(position + new Vector2(0, clipSize.Y - 1), 
                                        position + new Vector2(clipSize.X - 1, clipSize.Y), 
                                        Color.Orange);
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

            if (isSelected && timeRemapped && ClipSelection.Count == 1)
            {
                //var verticalOffset = 100;
                var verticalOffset = ImGui.GetContentRegionMax().Y + ImGui.GetWindowPos().Y - position.Y - LayerHeight;
                var horizontalOffset =  TimeLineCanvas.Current.TransformDirection(new Vector2(timeClip.SourceRange.Start - timeClip.TimeRange.Start,0)).X;
                var startPosition = position + new Vector2(0, LayerHeight);
                _drawList.AddBezierCurve(startPosition, 
                                         startPosition + new Vector2(0,verticalOffset),
                                         startPosition +  new Vector2(horizontalOffset,0),
                                         startPosition +  new Vector2(horizontalOffset,verticalOffset), 
                                         _timeRemappingColor,1);
                
                horizontalOffset =  TimeLineCanvas.Current.TransformDirection(new Vector2(timeClip.SourceRange.End - timeClip.TimeRange.End,0)).X;
                var endPosition = position + new Vector2(clipSize.X, LayerHeight);
                _drawList.AddBezierCurve(endPosition, 
                                         endPosition + new Vector2(0,verticalOffset),
                                         endPosition +  new Vector2(horizontalOffset,0),
                                         endPosition +  new Vector2(horizontalOffset,verticalOffset), 
                                         _timeRemappingColor,1);
                
            }
            
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
                //var instance = _compositionOp.Children.Single(child => child.SymbolChildId == symbolChildUi.Id);
                //SelectionManager.SetSelectionToChildUi(symbolChildUi, instance);
                
                //FitViewToSelectionHandling.FitViewToSelection();
                //ClipSelection.Select(timeClip);
                var primaryGraphWindow = GraphWindow.GetVisibleInstances().FirstOrDefault();
                if (primaryGraphWindow != null && NodeOperations.TryGetUiAndInstanceInComposition(timeClip.Id, _compositionOp, out var childUi, out var instance))
                {
                    primaryGraphWindow.GraphCanvas.SetCompositionToChildInstance(instance);
                }
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

            if (wasClicked)
            {
                FitViewToSelectionHandling.FitViewToSelection();
            }
            HandleDragging(timeClip, isSelected, wasClicked, HandleDragMode.Body, position);

            var handleSize = showSizeHandles ? new Vector2(HandleWidth, LayerHeight) : Vector2.One;

            ImGui.SetCursorScreenPos(position);
            var aHandleClicked = ImGui.InvisibleButton("startHandle", handleSize);
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                _drawList.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), Color.White);
                _drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), Color.Black);
            }

            HandleDragging(timeClip, isSelected, false, HandleDragMode.Start, position);

            ImGui.SetCursorScreenPos(position + new Vector2(bodyWidth + HandleWidth, 0));
            aHandleClicked |= ImGui.InvisibleButton("endHandle", handleSize);
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                _drawList.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), Color.White);
                _drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), Color.Black);
            }
            
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
                {
                    ClipSelection.Deselect(timeClip);
                }

                return;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                }

                ClipSelection.Select(timeClip);
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

        #region implement TimeObject interface --------------------------------------------
        void ITimeObjectManipulation.ClearSelection()
        {
            ClipSelection.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            if (selectMode == SelectionFence.SelectModes.Replace)
                ClipSelection.Clear();

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
                    ClipSelection.AddSelection(matchingClips);
                    break;

                case SelectionFence.SelectModes.Remove:
                    ClipSelection.Deselect(matchingClips);
                    break;
            }
        }

        ICommand ITimeObjectManipulation.StartDragCommand()
        {
            _moveClipsCommand = new MoveTimeClipsCommand(_compositionOp, ClipSelection.SelectedClips.ToList());
            return _moveClipsCommand;
        }

        void ITimeObjectManipulation.UpdateDragCommand(double dt, double dy)
        {
            var dragContent = ImGui.GetIO().KeyAlt;

            foreach (var clip in ClipSelection.SelectedClips)
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
            foreach (var clip in ClipSelection.SelectedClips)
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
            foreach (var clip in ClipSelection.SelectedClips)
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
            foreach (var clip in ClipSelection.SelectedClips)
            {
                clip.TimeRange.Start = (float)(originU + (clip.TimeRange.Start - originU) * scaleU);
                clip.TimeRange.End = (float)Math.Max(originU + (clip.TimeRange.End - originU) * scaleU, clip.TimeRange.Start + MinDuration);
            }
        }

        private const float MinDuration = 1 / 60f; // In bars

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = TimeRange.Undefined;
            foreach (var s in ClipSelection.SelectedClips)
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
            _moveClipsCommand = null;
        }

        void ITimeObjectManipulation.DeleteSelectedElements()
        {
            Log.Assert("Deleting not implemented yet");
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
                if (ClipSelection.Contains(clip))
                    continue;

                ValueSnapHandler.CheckForBetterSnapping(targetTime, clip.TimeRange.Start, canvasScale, ref bestSnapResult);
                ValueSnapHandler.CheckForBetterSnapping(targetTime, clip.TimeRange.End, canvasScale, ref bestSnapResult);
            }

            return bestSnapResult;
        }
        #endregion

        private Vector2 _minScreenPos;

        //public readonly HashSet<ITimeClip> SelectedTimeClips = new HashSet<ITimeClip>();
        private static MoveTimeClipsCommand _moveClipsCommand;
        private const int LayerHeight = 18;
        private const float HandleWidth = 5;
        private readonly Vector2 _handleOffset = new Vector2(HandleWidth, 0);

        private ImDrawListPtr _drawList;
        private Instance _compositionOp;
        private readonly ValueSnapHandler _snapHandler;
        private Playback _playback;
        private readonly Color _timeRemappingColor = Color.Orange.Fade(0.5f);

        /// <summary>
        /// Maps selection of <see cref="ITimeClip"/>s
        /// to <see cref="SelectionManager"/> with <see cref="ISelectableNode"/>s.
        /// </summary>
        private static class ClipSelection
        {
            public static void UpdateForComposition(Instance compositionOp)
            {
                _compositionOp = compositionOp;
                _compositionTimeClips.Clear();

                // Avoiding Linq for GC reasons 
                foreach (var child in compositionOp.Children)
                {
                    foreach (var output in child.Outputs)
                    {
                        if (output is ITimeClipProvider clipProvider)
                        {
                            _compositionTimeClips[clipProvider.TimeClip.Id] = clipProvider.TimeClip;
                        }
                    }
                }

                _selectedClips.Clear();
                foreach (var selectedGraphNode in SelectionManager.Selection)
                {
                    if (_compositionTimeClips.TryGetValue(selectedGraphNode.Id, out var selectedTimeClip))
                    {
                        _selectedClips.Add(selectedTimeClip);
                    }
                }
            }

            public static IEnumerable<ITimeClip> SelectedClips => _selectedClips;
            public static int Count => _selectedClips.Count;

            public static IReadOnlyCollection<ITimeClip> AllClips => _compositionTimeClips.Values;

            public static void Clear()
            {
                foreach (var c in _selectedClips)
                {
                    SelectionManager.DeselectCompositionChild(_compositionOp, c.Id);
                }
                _selectedClips.Clear();
            }

            public static void Select(ITimeClip timeClip)
            {
                foreach (var c in _selectedClips)
                {
                    SelectionManager.DeselectCompositionChild(_compositionOp, c.Id);
                }
                SelectionManager.SelectCompositionChild(_compositionOp, timeClip.Id, replaceSelection:false);
                _selectedClips.Add(timeClip);
            }

            public static void Deselect(ITimeClip timeClip)
            {
                SelectionManager.DeselectCompositionChild(_compositionOp, timeClip.Id);
                _selectedClips.Remove(timeClip);
            }
            
            public static void Deselect(List<ITimeClip> matchingClips)
            {
                matchingClips.ForEach(Deselect);
            }

            public static void AddSelection(List<ITimeClip> matchingClips)
            {
                foreach (var timeClip in matchingClips)
                {
                    SelectionManager.SelectCompositionChild(_compositionOp, timeClip.Id, replaceSelection:false);
                    _selectedClips.Add(timeClip);
                }
            }


            public static bool Contains(ITimeClip clip)
            {
                return _selectedClips.Contains(clip);
            }

            /// <summary>
            /// Reusing static collections to avoid GC leaks
            /// </summary>
            private static readonly Dictionary<Guid, ITimeClip> _compositionTimeClips = new Dictionary<Guid, ITimeClip>(100);
            private static readonly List<ITimeClip> _selectedClips = new List<ITimeClip>(100);
            private static Instance _compositionOp;
        }
    }
}