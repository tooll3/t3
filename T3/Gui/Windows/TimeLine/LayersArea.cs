using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Snapping;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Shows a list of Layers with <see cref="TimeClip"/>s
    /// </summary>
    public class LayersArea : ITimeElementSelectionHolder, IValueSnapAttractor
    {
        public LayersArea(ValueSnapHandler snapHandler)
        {
            _snapHandler = snapHandler;
        }

        public void Draw(Instance compositionOp)
        {
            _drawList = ImGui.GetWindowDrawList();
            _compositionOp = compositionOp;

            ImGui.BeginGroup();
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, 3)); // keep some padding 
                _minScreenPos = ImGui.GetCursorScreenPos();
                DrawLayer(NodeOperations.GetAllTimeClips(_compositionOp.Symbol));
                DrawContextMenu();
            }
            ImGui.EndGroup();
        }

        bool _contextMenuIsOpen;

        private void DrawContextMenu()
        {
            if (!_contextMenuIsOpen && !ImGui.IsWindowHovered())
                return;

            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!_contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                _contextMenuIsOpen = true;
                if (ImGui.MenuItem("Delete", null, false, _selectedItems.Count > 0))
                {
                    UndoRedoStack.AddAndExecute(new TimeClipDeleteCommand(_compositionOp.Symbol, _selectedItems));
                    _selectedItems.Clear();
                }

                ImGui.EndPopup();
            }
            else
            {
                _contextMenuIsOpen = false;
            }

            ImGui.PopStyleVar();
        }

        private void DrawLayer(List<TimeClip> layerClips)
        {
            // Draw layer lines
            var min = ImGui.GetCursorScreenPos();
            var max = min + new Vector2(ImGui.GetContentRegionAvail().X, LayerHeight - 1);
            _drawList.AddRectFilled(new Vector2(min.X, max.Y),
                                    new Vector2(max.X, max.Y + 1), Color.Black);

            var layerArea = new ImRect(min, max);
            foreach (var clip in layerClips)
            {
                DrawClip(layerArea, clip);
            }

            ImGui.SetCursorScreenPos(min + new Vector2(0, LayerHeight));
        }

        private void DrawClip(ImRect layerArea, TimeClip timeClip)
        {
            var xStartTime = TimeLineCanvas.Current.TransformPositionX(timeClip.VisibleRange.Start);
            var xEndTime = TimeLineCanvas.Current.TransformPositionX(timeClip.VisibleRange.End);
            var position = new Vector2(xStartTime, layerArea.Min.Y);

            var clipWidth = xEndTime - xStartTime;
            var showSizeHandles = clipWidth > 4 * HandleWidth;
            var bodyWidth = showSizeHandles
                                ? (clipWidth - 2 * HandleWidth)
                                : clipWidth;

            var bodySize = new Vector2(bodyWidth, layerArea.GetHeight());
            var clipSize = new Vector2(clipWidth, layerArea.GetHeight());

            ImGui.PushID(timeClip.Id.GetHashCode());

            var isSelected = _selectedItems.Contains(timeClip);
            var color = new Color(0.5f);
            _drawList.AddRectFilled(position, position + clipSize - new Vector2(1, 0), color);
            if (isSelected)
                _drawList.AddRect(position, position + clipSize - new Vector2(1, 0), Color.White);

            _drawList.AddText(position + new Vector2(4, 4), isSelected ? Color.White : Color.Black, timeClip.Name);

            // Body clicked
            ImGui.SetCursorScreenPos(showSizeHandles ? (position + _handleOffset) : position);
            if (ImGui.InvisibleButton("body", bodySize))
            {
                TimeLineCanvas.Current.CompleteDragCommand();

                if (_moveClipsCommand != null)
                {
                    _moveClipsCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveClipsCommand);
                    _moveClipsCommand = null;
                }
            }

            HandleDragging(timeClip, isSelected, HandleDragMode.Body);

            var handleSize = showSizeHandles ? new Vector2(HandleWidth, LayerHeight) : Vector2.One;

            ImGui.SetCursorScreenPos(position);
            var aHandleClicked = ImGui.InvisibleButton("startHandle", handleSize);
            HandleDragging(timeClip, isSelected, HandleDragMode.Start);

            ImGui.SetCursorScreenPos(position + new Vector2(bodyWidth + HandleWidth, 0));
            aHandleClicked |= ImGui.InvisibleButton("endHandle", handleSize);
            HandleDragging(timeClip, isSelected, HandleDragMode.End);

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

        private enum HandleDragMode
        {
            Body = 0,
            Start,
            End,
        }

        private void HandleDragging(TimeClip timeClip, bool isSelected, HandleDragMode mode)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(mode == HandleDragMode.Body
                                         ? ImGuiMouseCursor.Hand
                                         : ImGuiMouseCursor.ResizeEW);
            }

            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 0f))
                return;

            if (ImGui.GetIO().KeyCtrl)
            {
                if (isSelected)
                    _selectedItems.Remove(timeClip);

                return;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                }

                _selectedItems.Add(timeClip);
            }

            if (_moveClipsCommand == null)
            {
                TimeLineCanvas.Current.StartDragCommand();
            }

            double dt = TimeLineCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta).X;

            switch (mode)
            {
                case HandleDragMode.Body:
                    var startTime = timeClip.VisibleRange.Start + dt;
                    if (_snapHandler.CheckForSnapping(ref startTime))
                        dt = startTime - timeClip.VisibleRange.Start;

                    var endTime = timeClip.VisibleRange.End + dt;
                    if (_snapHandler.CheckForSnapping(ref endTime))
                        dt = endTime - timeClip.VisibleRange.End;

                    TimeLineCanvas.Current.UpdateDragCommand(dt, 0);
                    break;

                case HandleDragMode.Start:
                    var startTime2 = timeClip.VisibleRange.Start + dt;
                    if (_snapHandler.CheckForSnapping(ref startTime2))
                        dt = startTime2 - timeClip.VisibleRange.Start;

                    TimeLineCanvas.Current.UpdateDragStartCommand(dt, 0);
                    break;

                case HandleDragMode.End:
                    var endTime2 = timeClip.VisibleRange.Start + dt;
                    if (_snapHandler.CheckForSnapping(ref endTime2))
                        dt = endTime2 - timeClip.VisibleRange.End;

                    TimeLineCanvas.Current.UpdateDragEndCommand(dt, 0);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        #region implement selection holder interface --------------------------------------------
        void ITimeElementSelectionHolder.ClearSelection()
        {
            _selectedItems.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectMode selectMode)
        {
            if (selectMode == SelectMode.Replace)
                _selectedItems.Clear();

            var startTime = TimeLineCanvas.Current.InverseTransformPositionX(screenArea.Min.X);
            var endTime = TimeLineCanvas.Current.InverseTransformPositionX(screenArea.Max.X);

            var layerMinIndex = (screenArea.Min.Y - _minScreenPos.Y) / LayerHeight - 1;
            var layerMaxIndex = (screenArea.Max.Y - _minScreenPos.Y) / LayerHeight;

            var allClips = NodeOperations.GetAllTimeClips(_compositionOp.Symbol);

            var matchingClips = allClips.FindAll(clip => clip.VisibleRange.Start <= endTime
                                                         && clip.VisibleRange.End >= startTime
                                                         && clip.LayerIndex <= layerMaxIndex
                                                         && clip.LayerIndex >= layerMinIndex);
            switch (selectMode)
            {
                case SelectMode.Add:
                case SelectMode.Replace:
                    _selectedItems.UnionWith(matchingClips);
                    break;
                case SelectMode.Remove:
                    _selectedItems.ExceptWith(matchingClips);
                    break;
            }
        }

        ICommand ITimeElementSelectionHolder.StartDragCommand()
        {
            _moveClipsCommand = new MoveTimeClipCommand(_compositionOp.Symbol.Id, _selectedItems.ToList());
            return _moveClipsCommand;
        }

        void ITimeElementSelectionHolder.UpdateDragCommand(double dt, double dv)
        {
            foreach (var clip in _selectedItems)
            {
                clip.VisibleRange.Start += (float)dt;
                clip.VisibleRange.End += (float)dt;
            }
        }

        void ITimeElementSelectionHolder.UpdateDragStartCommand(double dt, double dv)
        {
            foreach (var clip in _selectedItems)
            {
                // Keep 1 frame min duration
                clip.VisibleRange.Start = (float)Math.Min(clip.VisibleRange.Start + dt, clip.VisibleRange.End - MinDuration);
            }
        }

        void ITimeElementSelectionHolder.UpdateDragEndCommand(double dt, double dv)
        {
            foreach (var clip in _selectedItems)
            {
                // Keep 1 frame min duration
                clip.VisibleRange.End = (float)Math.Max(clip.VisibleRange.End + dt, clip.VisibleRange.Start + MinDuration);
            }
        }

        void ITimeElementSelectionHolder.UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var clip in _selectedItems)
            {
                clip.VisibleRange.Start = (float)(originU + (clip.VisibleRange.Start - originU) * scaleU);
                clip.VisibleRange.End = (float)Math.Max(originU + (clip.VisibleRange.End - originU) * scaleU, clip.VisibleRange.Start + MinDuration);
            }
        }

        private const float MinDuration = 1 / 60f; // In bars

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = TimeRange.Undefined;
            foreach (var s in _selectedItems)
            {
                timeRange.Unite(s.VisibleRange.Start);
                timeRange.Unite(s.VisibleRange.End);
            }

            return timeRange;
        }

        void ITimeElementSelectionHolder.CompleteDragCommand()
        {
            if (_moveClipsCommand == null)
                return;

            _moveClipsCommand.StoreCurrentValues();
            UndoRedoStack.Add(_moveClipsCommand);
            _moveClipsCommand = null;
        }

        void ITimeElementSelectionHolder.DeleteSelectedElements()
        {
            //TODO: Implement
        }
        #endregion

        #region implement snapping interface -----------------------------------
        /// <summary>
        /// Snap to all non-selected Clips
        /// </summary>
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime)
        {
            SnapResult bestSnapResult = null;
            var snapThresholdOnCanvas = TimeLineCanvas.Current.InverseTransformDirection(new Vector2(6, 0)).X;


            var allClips = NodeOperations.GetAllTimeClips(_compositionOp.Symbol);
            foreach (var clip in allClips)
            {
                if (_selectedItems.Contains(clip))
                    continue;

                KeyframeOperations.CheckForBetterSnapping(targetTime, clip.VisibleRange.Start, snapThresholdOnCanvas, ref bestSnapResult);
                KeyframeOperations.CheckForBetterSnapping(targetTime, clip.VisibleRange.End, snapThresholdOnCanvas, ref bestSnapResult);
            }

            return bestSnapResult;
        }
        #endregion

        private Vector2 _minScreenPos;

        private readonly HashSet<TimeClip> _selectedItems = new HashSet<TimeClip>();
        private static MoveTimeClipCommand _moveClipsCommand;
        private const int LayerHeight = 25;
        private const float HandleWidth = 5;
        private readonly Vector2 _handleOffset = new Vector2(HandleWidth, 0);

        private ImDrawListPtr _drawList;
        private Instance _compositionOp;
        private readonly ValueSnapHandler _snapHandler;
    }
}