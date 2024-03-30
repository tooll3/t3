using T3.Editor.Gui.Graph;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Animation;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Interaction.Snapping;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.TimeLine
{
    /// <summary>
    /// Shows a list of Layers with <see cref="TimeClip"/>s
    /// </summary>
    internal class LayersArea : ITimeObjectManipulation, IValueSnapAttractor
    {
        public LayersArea(ValueSnapHandler snapHandler, GraphCanvas graphCanvas)
        {
            _snapHandler = snapHandler;
            _graphCanvas = graphCanvas;
            _clipSelection = new ClipSelection(graphCanvas.NodeSelection);
        }

        public void Draw(Instance compositionOp, Playback playback)
        {
            _drawList = ImGui.GetWindowDrawList();
            _playback = playback;

            ImGui.BeginGroup();
            {
                _clipSelection.UpdateForComposition(compositionOp);
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, 3)); // keep some padding 
                _minScreenPos = ImGui.GetCursorScreenPos();
                DrawAllLayers(_clipSelection.AllClips, compositionOp);
                DrawContextMenu(compositionOp);
            }
            ImGui.EndGroup();
        }

        private bool _contextMenuIsOpen;

        private void DrawContextMenu(Instance compositionOp)
        {
            if (!_contextMenuIsOpen && !ImGui.IsWindowHovered())
                return;

            if (_clipSelection.Count == 0)
                return;

            var compositionSymbolUi = compositionOp.GetSymbolUi();

            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!_contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                _contextMenuIsOpen = true;
                if (ImGui.MenuItem("Delete", null, false, _clipSelection.Count > 0))
                {
                    UndoRedoStack.AddAndExecute(new TimeClipDeleteCommand(compositionOp, _clipSelection.SelectedClips));
                    _clipSelection.Clear();
                }

                if (ImGui.MenuItem("Clear Time Stretch", null, false, _clipSelection.Count > 0))
                {
                    var moveTimeClipCommand = new MoveTimeClipsCommand(compositionOp, _clipSelection.SelectedClips.ToList());
                    foreach (var clip in _clipSelection.SelectedClips)
                    {
                        clip.SourceRange = clip.TimeRange.Clone();
                    }

                    moveTimeClipCommand.StoreCurrentValues();
                    UndoRedoStack.AddAndExecute(moveTimeClipCommand);
                    _clipSelection.Clear();
                }

                if (ImGui.MenuItem("Cut at time"))
                {
                    // FIXME: This command is incomplete and like to lead to inconsistent data.
                    // On the other hand, added it to the UndoRedoQueue prevents an obvious crash...
                    var commands = new List<ICommand>();

                    var timeInBars = _playback.TimeInBars;
                    var matchingClips = _clipSelection.AllOrSelectedClips.Where(clip => clip.TimeRange.Contains(timeInBars)).ToList();

                    foreach (var clip in matchingClips)
                    {
                        var symbolChildUi = compositionSymbolUi.ChildUis[clip.Id];

                        var originalName = symbolChildUi.SymbolChild.ReadableName;
                        Vector2 newPos = symbolChildUi.PosOnCanvas;
                        newPos.Y += symbolChildUi.Size.Y + 5.0f;
                        var cmd = new CopySymbolChildrenCommand(compositionSymbolUi, 
                                                                new[] { symbolChildUi },
                                                                null,
                                                                compositionSymbolUi, 
                                                                newPos);
                        commands.Add(cmd);
                        cmd.Do();

                        // Set new end to the original time clip
                        float orgTimeRangeEnd = clip.TimeRange.End;
                        float originalSourceDuration = clip.SourceRange.Duration;
                        float normalizedCutPosition = ((float)_playback.TimeInBars - clip.TimeRange.Start) / clip.TimeRange.Duration;
                        
                        clip.TimeRange.End = (float)_playback.TimeInBars;
                        
                        // Apply new time range to newly added instance
                        Guid newChildId = cmd.OldToNewIdDict[clip.Id];
                        var newInstance = compositionOp.Children[newChildId];
                        var newTimeClip = newInstance.Outputs.OfType<ITimeClipProvider>().Single().TimeClip;
                        
                        var newSymbolUiChild = compositionSymbolUi.ChildUis[newChildId];
                        var renameCommand = new ChangeSymbolChildNameCommand(newSymbolUiChild, compositionSymbolUi.Symbol);
                        renameCommand.NewName = originalName;
                        renameCommand.Do();
                        commands.Add(renameCommand);
                        
                        newSymbolUiChild.SymbolChild.Name = originalName;
                            
                        newTimeClip.TimeRange = new TimeRange((float)_playback.TimeInBars, orgTimeRangeEnd);
                        newTimeClip.SourceRange.Start = newTimeClip.SourceRange.Start + originalSourceDuration * normalizedCutPosition;
                        newTimeClip.SourceRange.End = clip.SourceRange.End;

                        clip.SourceRange.Duration = originalSourceDuration * normalizedCutPosition;
                    }

                    var macroCommands = new MacroCommand("split clip", commands);
                    UndoRedoStack.Add(macroCommands);
                }
                ImGui.Separator();

                ImGui.EndPopup();
            }
            else
            {
                _contextMenuIsOpen = false;
            }
        }

        private int _minLayerIndex = int.MaxValue;
        private int _maxLayerIndex = int.MinValue;

        public float LastHeight;

        private void DrawAllLayers(IReadOnlyCollection<ITimeClip> clips, Instance compositionOp)
        {
            if (clips.Count == 0)
            {
                LastHeight = 0;
                return;
            }

            _minLayerIndex = int.MaxValue;
            _maxLayerIndex = int.MinValue;

            foreach (var clip in clips)
            {
                _minLayerIndex = Math.Min(clip.LayerIndex, _minLayerIndex);
                _maxLayerIndex = Math.Max(clip.LayerIndex, _maxLayerIndex);
            }

            // Draw layer lines
            var min = ImGui.GetCursorScreenPos() + new Vector2(0,5);
            var max = min + new Vector2(ImGui.GetContentRegionAvail().X, LayerHeight * (_maxLayerIndex - _minLayerIndex + 1) + 1);
            var layerArea = new ImRect(min, max);
            LastHeight = max.Y - min.Y + 5;

            _drawList.AddRectFilled(new Vector2(min.X, max.Y - 4+ 5),
                                    new Vector2(max.X, max.Y - 1 + 5), new Color(0, 0, 0, 0.4f));

            var compositionSymbolUi = compositionOp.GetSymbolUi();
            foreach (var clip in clips)
            {
                DrawClip(clip, layerArea, _minLayerIndex, compositionOp, compositionSymbolUi);
            }

            ImGui.SetCursorScreenPos(min + new Vector2(0, LayerHeight));
        }

        private void DrawClip(ITimeClip timeClip, ImRect layerArea, int minLayerIndex, Instance compositionOp, SymbolUi compositionSymbolUi)
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

            var bodySize = new Vector2(bodyWidth, LayerHeight-2);
            var clipSize = new Vector2(clipWidth, LayerHeight - 2);

            var symbolChildUi = compositionSymbolUi.ChildUis[timeClip.Id];

            ImGui.PushID(symbolChildUi.Id.GetHashCode());

            var isSelected = _clipSelection.SelectedClips.Contains(timeClip);


            var color = new Color(0.6f, 0.6f, 0.3f, 0.6f);
            var itemRectMax = position + clipSize - new Vector2(1, 0);
            _drawList.AddRectFilled(position, itemRectMax, color);
            
            var shadowColor = new Color(0.0f, 0.0f, 0.0f, 0.4f);
            _drawList.AddRect(position - Vector2.One, itemRectMax + Vector2.One, shadowColor);

            var timeRemapped = timeClip.TimeRange != timeClip.SourceRange;
            var timeStretched = Math.Abs(timeClip.TimeRange.Duration - timeClip.SourceRange.Duration) > 0.001;
            if (timeStretched)
            {
                _drawList.AddRectFilled(position + new Vector2(0, clipSize.Y - 2), 
                                        position + new Vector2(clipSize.X - 1, clipSize.Y), 
                                        UiColors.StatusAttention);
            }
            else if (timeRemapped)
            {
                _drawList.AddRectFilled(position + new Vector2(0, clipSize.Y - 1), 
                                        position + new Vector2(clipSize.X - 1, clipSize.Y), 
                                        UiColors.StatusAnimated);
            }

            if (isSelected)
                _drawList.AddRect(position - Vector2.One, itemRectMax + Vector2.One, UiColors.Selection);

            ImGui.PushClipRect(position, itemRectMax, true);
            var label = timeStretched
                            ? symbolChildUi.SymbolChild.ReadableName + $" ({GetSpeed(timeClip)}%)"
                            : symbolChildUi.SymbolChild.ReadableName;
            ImGui.PushFont(Fonts.FontSmall);
            _drawList.AddText(position + new Vector2(4, 1), isSelected ? UiColors.Selection : UiColors.BackgroundFull, label);
            ImGui.PopFont();
            ImGui.PopClipRect();

            if (isSelected && timeRemapped && _clipSelection.Count == 1)
            {
                //var verticalOffset = 100;
                var verticalOffset = ImGui.GetContentRegionMax().Y + ImGui.GetWindowPos().Y - position.Y - LayerHeight;
                var horizontalOffset =  TimeLineCanvas.Current.TransformDirection(new Vector2(timeClip.SourceRange.Start - timeClip.TimeRange.Start,0)).X;
                var startPosition = position + new Vector2(0, LayerHeight);
                _drawList.AddBezierCubic(startPosition, 
                                         startPosition + new Vector2(0,verticalOffset),
                                         startPosition + new Vector2(horizontalOffset,0),
                                         startPosition + new Vector2(horizontalOffset,verticalOffset), 
                                         _timeRemappingColor,1);
                
                horizontalOffset =  TimeLineCanvas.Current.TransformDirection(new Vector2(timeClip.SourceRange.End - timeClip.TimeRange.End,0)).X;
                var endPosition = position + new Vector2(clipSize.X, LayerHeight);
                _drawList.AddBezierCubic(endPosition, 
                                         endPosition + new Vector2(0,verticalOffset),
                                         endPosition + new Vector2(horizontalOffset,0),
                                         endPosition + new Vector2(horizontalOffset,verticalOffset), 
                                         _timeRemappingColor,1);
                
            }
            
            ImGui.SetCursorScreenPos(showSizeHandles ? (position + _handleOffset) : position);

            var wasClicked = ImGui.InvisibleButton("body", bodySize);

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                {
                    ImGui.TextUnformatted($"In: {timeClip.TimeRange.Start}");
                    ImGui.TextUnformatted($"Out: {timeClip.TimeRange.End}");
                    if (timeRemapped)
                    {
                        ImGui.TextUnformatted($"Source In: {timeClip.SourceRange.Start}");
                        ImGui.TextUnformatted($"Source Out: {timeClip.SourceRange.End}");
                    }

                    if (timeStretched)
                    {
                        var speed = GetSpeed(timeClip);
                        ImGui.TextUnformatted($"Speed: {speed:0.}%");
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
                if (Structure.TryGetUiAndInstanceInComposition(timeClip.Id, compositionOp, out var childUi, out var instance))
                {
                    _graphCanvas.SetCompositionToChildInstance(instance);
                }
            }

            if (ImGui.IsItemHovered())
            {
                _graphCanvas.NodeSelection.HoveredIds.Add(symbolChildUi.Id);
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
            HandleDragging(compositionOp, timeClip, isSelected, wasClicked, HandleDragMode.Body, position);

            var handleSize = showSizeHandles ? new Vector2(HandleWidth, LayerHeight) : Vector2.One;

            ImGui.SetCursorScreenPos(position);
            var aHandleClicked = ImGui.InvisibleButton("startHandle", handleSize);
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                _drawList.AddRectFilled(ImGui.GetItemRectMin(),
                                        ImGui.GetItemRectMax() - new Vector2(0,2), UiColors.ForegroundFull.Fade(0.5f));
                _drawList.AddRect(ImGui.GetItemRectMin(),
                                  ImGui.GetItemRectMax() - new Vector2(0,2), UiColors.BackgroundFull);
            }

            HandleDragging(compositionOp, timeClip, isSelected, false, HandleDragMode.Start, position);

            ImGui.SetCursorScreenPos(position + new Vector2(bodyWidth + HandleWidth, 0));
            aHandleClicked |= ImGui.InvisibleButton("endHandle", handleSize);
            if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            {
                _drawList.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax() - new Vector2(0,2), UiColors.ForegroundFull.Fade(0.5f));
                _drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax() - new Vector2(0,2), UiColors.BackgroundFull);
            }
            
            HandleDragging(compositionOp, timeClip, isSelected, false, HandleDragMode.End, position);

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

        private void HandleDragging(Instance compositionOp, ITimeClip timeClip, bool isSelected, bool wasClicked, HandleDragMode mode, Vector2 position)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(mode == HandleDragMode.Body
                                         ? ImGuiMouseCursor.Hand
                                         : ImGuiMouseCursor.ResizeEW);
            }

            if (!wasClicked && (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, UserSettings.Config.ClickThreshold)))
                return;

            if (ImGui.GetIO().KeyCtrl)
            {
                if (isSelected)
                {
                    _clipSelection.Deselect(timeClip);
                }

                return;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                }

                _clipSelection.Select(timeClip);
            }

            var mousePos = ImGui.GetIO().MousePos;
            var dragContent = ImGui.GetIO().KeyAlt;
            var referenceRange = (dragContent ? timeClip.SourceRange : timeClip.TimeRange);
            var scale = 1f;
            if (dragContent && timeClip.SourceRange.Duration != 0 && timeClip.SourceRange.Duration != 0)
                scale = timeClip.TimeRange.Duration / timeClip.SourceRange.Duration;

            if (_moveClipsCommand == null)
            {
                var dragStartedAtTime = TimeLineCanvas.Current.InverseTransformX(mousePos.X);
                _timeWithinDraggedClip = dragStartedAtTime - referenceRange.Start;
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

                    if (_snapHandler.CheckForSnapping(ref newStartTime, TimeLineCanvas.Current.Scale.X * scale))
                    {
                        TimeLineCanvas.Current.UpdateDragCommand(newStartTime - referenceRange.Start, dy);
                        return;
                    }

                    var newEndTime = newStartTime + referenceRange.Duration;
                    _snapHandler.CheckForSnapping(ref newEndTime, TimeLineCanvas.Current.Scale.X * scale);
                    TimeLineCanvas.Current.UpdateDragCommand(newEndTime - referenceRange.End, dy);
                    break;

                case HandleDragMode.Start:
                    var newDragStartTime = TimeLineCanvas.Current.InverseTransformX(mousePos.X);
                    _snapHandler.CheckForSnapping(ref newDragStartTime, TimeLineCanvas.Current.Scale.X * scale);
                    TimeLineCanvas.Current.UpdateDragAtStartPointCommand(newDragStartTime - timeClip.TimeRange.Start, 0);
                    break;

                case HandleDragMode.End:
                    var newDragTime = TimeLineCanvas.Current.InverseTransformX(mousePos.X);
                    _snapHandler.CheckForSnapping(ref newDragTime, TimeLineCanvas.Current.Scale.X * scale);
                    TimeLineCanvas.Current.UpdateDragAtEndPointCommand(newDragTime - timeClip.TimeRange.End, 0);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        #region implement TimeObject interface --------------------------------------------
        public void ClearSelection()
        {
            _clipSelection.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            var compositionOp = GraphWindow.Focused?.CompositionOp;
            if (compositionOp == null)
                return;
            
            if (selectMode == SelectionFence.SelectModes.Replace)
                _clipSelection.Clear();

            var startTime = TimeLineCanvas.Current.InverseTransformX(screenArea.Min.X);
            var endTime = TimeLineCanvas.Current.InverseTransformX(screenArea.Max.X);

            var layerMinIndex = (screenArea.Min.Y - _minScreenPos.Y) / LayerHeight + _minLayerIndex;
            var layerMaxIndex = (screenArea.Max.Y - _minScreenPos.Y) / LayerHeight + _minLayerIndex;

            foreach (var clip in Structure.GetAllTimeClips(compositionOp))
            {
                 var matches = clip.TimeRange.Start <= endTime
                         && clip.TimeRange.End >= startTime
                         && clip.LayerIndex <= layerMaxIndex
                         && clip.LayerIndex >= layerMinIndex - 1;

                 if (!matches)
                     continue;
                 
                 switch (selectMode)
                 {
                     case SelectionFence.SelectModes.Add:
                     case SelectionFence.SelectModes.Replace:
                         _clipSelection.AddSelection(clip);
                         break;

                     case SelectionFence.SelectModes.Remove:
                         _clipSelection.Deselect(clip);
                         break;
                 }
            }
        }

        public ICommand StartDragCommand()
        {
            var composition = GraphWindow.Focused?.CompositionOp;
            if (composition == null)
                return null;
            
            _moveClipsCommand = new MoveTimeClipsCommand(composition, _clipSelection.SelectedClips.ToList());
            return _moveClipsCommand;
        }

        void ITimeObjectManipulation.UpdateDragCommand(double dt, double dy)
        {
            var dragContent = ImGui.GetIO().KeyAlt;

            foreach (var clip in _clipSelection.SelectedClips)
            {
                if (dragContent)
                {
                    //TODO: fix continuous dragging
                    clip.TimeRange.Start += (float)dt;
                    clip.TimeRange.End += (float)dt;
                    
                    // clip.SourceRange.Start += (float)dt;
                    // clip.SourceRange.End += (float)dt;
                }
                else
                {
                    clip.TimeRange.Start += (float)dt;
                    clip.TimeRange.End += (float)dt;
                    clip.SourceRange.Start += (float)dt;
                    clip.SourceRange.End += (float)dt;

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

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
            var trim = !ImGui.GetIO().KeyAlt;
            foreach (var clip in _clipSelection.SelectedClips)
            {
                // Keep 1 frame min duration
                var org = clip.TimeRange.Start;
                clip.TimeRange.Start = (float)Math.Min(clip.TimeRange.Start + dt, clip.TimeRange.End - MinDuration);
                var d = clip.TimeRange.Start - org;
                if (trim)
                    clip.SourceRange.Start += d;
            }
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
            var trim = !ImGui.GetIO().KeyAlt;
            foreach (var clip in _clipSelection.SelectedClips)
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
            foreach (var clip in _clipSelection.SelectedClips)
            {
                clip.TimeRange.Start = (float)(originU + (clip.TimeRange.Start - originU) * scaleU);
                clip.TimeRange.End = (float)Math.Max(originU + (clip.TimeRange.End - originU) * scaleU, clip.TimeRange.Start + MinDuration);
            }
        }

        private const float MinDuration = 1 / 60f; // In bars

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = TimeRange.Undefined;
            foreach (var s in _clipSelection.SelectedClips)
            {
                // fix broken time ranges
                // FIXME: make sure these don't happen at all
                if (s.TimeRange.Duration <= 0
                    || float.IsNaN(s.TimeRange.Start)
                    || float.IsNaN(s.TimeRange.End))
                {
                    s.TimeRange.Start = 0;
                    s.TimeRange.End = s.TimeRange.Start + 1;
                }

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

        void ITimeObjectManipulation.DeleteSelectedElements(Instance _)
        {
            //TODO: Implement deleting of layers with delete key
        }
        #endregion

        #region implement snapping interface -----------------------------------
        /// <summary>
        /// Snap to all non-selected Clips
        /// </summary>
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime, float canvasScale)
        {
            var currentComp = GraphWindow.Focused?.CompositionOp;
            SnapResult bestSnapResult = null;

            var allClips = Structure.GetAllTimeClips(currentComp);

            foreach (var clip in allClips)
            {
                if (_clipSelection.Contains(clip))
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
        private const float HandleWidth = 7;
        private readonly Vector2 _handleOffset = new(HandleWidth, 0);

        private ImDrawListPtr _drawList;
        private readonly ValueSnapHandler _snapHandler;
        private Playback _playback;
        private readonly Color _timeRemappingColor = UiColors.StatusAnimated.Fade(0.5f);
        private readonly GraphCanvas _graphCanvas;
        private readonly ClipSelection _clipSelection;

        /// <summary>
        /// Maps selection of <see cref="ITimeClip"/>s
        /// to <see cref="NodeSelection"/> with <see cref="ISelectableCanvasObject"/>s.
        /// </summary>
        private class ClipSelection
        {
            public ClipSelection(NodeSelection nodeSelection)
            {
                _nodeSelection = nodeSelection;
            }
            
            public void UpdateForComposition(Instance compositionOp)
            {
                _compositionOp = compositionOp;
                _compositionTimeClips.Clear();

                // Avoiding Linq for GC reasons 
                foreach (var child in compositionOp.Children.Values)
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
                foreach (var selectedGraphNode in _nodeSelection.Selection)
                {
                    if (_compositionTimeClips.TryGetValue(selectedGraphNode.Id, out var selectedTimeClip))
                    {
                        _selectedClips.Add(selectedTimeClip);
                    }
                }
            }

            public List<ITimeClip> SelectedClips => _selectedClips;
            public int Count => _selectedClips.Count;
            public IReadOnlyCollection<ITimeClip> AllOrSelectedClips => _selectedClips.Count > 0 ? _selectedClips : AllClips;

            public IReadOnlyCollection<ITimeClip> AllClips => _compositionTimeClips.Values;

            public void Clear()
            {
                foreach (var c in _selectedClips)
                {
                    _nodeSelection.DeselectCompositionChild(_compositionOp, c.Id);
                }
                _selectedClips.Clear();
            }

            public void Select(ITimeClip timeClip)
            {
                foreach (var c in _selectedClips)
                {
                    _nodeSelection.DeselectCompositionChild(_compositionOp, c.Id);
                }
                _nodeSelection.SelectCompositionChild(_compositionOp, timeClip.Id, replaceSelection:false);
                _selectedClips.Add(timeClip);
            }

            public void Deselect(ITimeClip timeClip)
            {
                _nodeSelection.DeselectCompositionChild(_compositionOp, timeClip.Id);
                _selectedClips.Remove(timeClip);
            }
            
            public void Deselect(List<ITimeClip> matchingClips)
            {
                matchingClips.ForEach(Deselect);
            }

            public void AddSelection(ITimeClip matchingClip)
            {
                _nodeSelection.SelectCompositionChild(_compositionOp, matchingClip.Id, replaceSelection:false);
                _selectedClips.Add(matchingClip);
            }
            
            public void AddSelection(List<ITimeClip> matchingClips)
            {
                foreach (var timeClip in matchingClips)
                {
                    _nodeSelection.SelectCompositionChild(_compositionOp, timeClip.Id, replaceSelection:false);
                    _selectedClips.Add(timeClip);
                }
            }


            public bool Contains(ITimeClip clip)
            {
                return _selectedClips.Contains(clip);
            }

            /// <summary>
            /// Reusing static collections to avoid GC leaks
            /// </summary>
            private readonly Dictionary<Guid, ITimeClip> _compositionTimeClips = new(100);
            private readonly List<ITimeClip> _selectedClips = new(100);
            private Instance _compositionOp;
            private readonly NodeSelection _nodeSelection;
        }
    }
}