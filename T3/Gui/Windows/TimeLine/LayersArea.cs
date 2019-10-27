using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Interaction.Snapping;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    /// <summary>
    /// Shows a list of TimeClipLayerUi with Clip
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
                ImGui.SetCursorPos( ImGui.GetCursorPos() + new Vector2(0,3));// keep some padding 
                _minScreenPos = ImGui.GetCursorScreenPos();

                foreach (var layer in compositionOp.Symbol.Animator.Layers)
                {
                    DrawLayer(layer);
                }
                
                DrawContextMenu();
            }
            ImGui.EndGroup();
        }

        
        bool _contextMenuIsOpen = false;
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


        private void DrawLayer(Animator.Layer layer)
        {
            // Draw layer lines
            var min = ImGui.GetCursorScreenPos();
            var max = min + new Vector2(ImGui.GetContentRegionAvail().X,  LayerHeight-1);
            _drawList.AddRectFilled(new Vector2(min.X, max.Y),
                                    new Vector2(max.X, max.Y+1), Color.Black);

            var layerArea = new ImRect(min, max);
            foreach (var clip in layer.Clips)
            {
                DrawClip(layerArea, clip);
            }
            ImGui.SetCursorScreenPos(min + new Vector2(0, LayerHeight));
        }


        private void DrawClip(ImRect layerArea, Animator.Clip clip)
        {
            var xStartTime = TimeLineCanvas.Current.TransformPositionX((float)clip.StartTime);
            var xEndTime = TimeLineCanvas.Current.TransformPositionX((float)clip.EndTime);
            var position = new Vector2(xStartTime, layerArea.Min.Y);

            var clipWidth = xEndTime - xStartTime;
            var showSizeHandles = clipWidth > 4 * HandleWidth;
            var bodyWidth = showSizeHandles
                                ? (clipWidth - 2 * HandleWidth)
                                : clipWidth;

            var bodySize = new Vector2(bodyWidth, layerArea.GetHeight());
            var clipSize = new Vector2(clipWidth, layerArea.GetHeight());

            ImGui.PushID(clip.Id.GetHashCode());

            var isSelected = _selectedItems.Contains(clip);
            var color = new Color(0.5f);
            _drawList.AddRectFilled(position, position + clipSize - new Vector2(1, 0), color);
            if(isSelected) 
                _drawList.AddRect(position, position + clipSize - new Vector2(1,0), Color.White);
            
            _drawList.AddText(position + new Vector2(4,4), isSelected ? Color.White: Color.Black, clip.Name);

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

            HandleDragging(clip, isSelected, HandleDragMode.Body);

            var handleSize = showSizeHandles ? new Vector2(HandleWidth, LayerHeight) : Vector2.One;

            ImGui.SetCursorScreenPos(position);
            var aHandleClicked = ImGui.InvisibleButton("startHandle", handleSize);
            HandleDragging(clip, isSelected, HandleDragMode.Start);

            ImGui.SetCursorScreenPos(position + new Vector2(bodyWidth + HandleWidth, 0));
            aHandleClicked |= ImGui.InvisibleButton("endHandle", handleSize);
            HandleDragging(clip, isSelected, HandleDragMode.End);

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

        private void HandleDragging(Animator.Clip clip, bool isSelected, HandleDragMode mode)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(mode == HandleDragMode.Body 
                                         ? ImGuiMouseCursor.Hand 
                                         :ImGuiMouseCursor.ResizeEW);
            }

            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 0f))
                return;

            if (ImGui.GetIO().KeyCtrl)
            {
                if (isSelected)
                    _selectedItems.Remove(clip);

                return;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                    _selectedItems.Clear();

                _selectedItems.Add(clip);
            }

            if (_moveClipsCommand == null)
            {
                TimeLineCanvas.Current.StartDragCommand();
            }

            double dt = TimeLineCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta).X;

            switch (mode)
            {
                case HandleDragMode.Body:
                    var snapClipToStart = _snapHandler.CheckForSnapping(clip.StartTime + dt);
                    if (!double.IsNaN(snapClipToStart))
                        dt = snapClipToStart - clip.StartTime;

                    var snapClipToEnd = _snapHandler.CheckForSnapping(clip.EndTime + dt);
                    if (!double.IsNaN(snapClipToEnd))
                        dt = snapClipToEnd - clip.EndTime;

                    TimeLineCanvas.Current.UpdateDragCommand(dt);
                    break;

                case HandleDragMode.Start:
                    var snapToStart = _snapHandler.CheckForSnapping(clip.StartTime + dt);
                    if (!double.IsNaN(snapToStart))
                        dt = snapToStart - clip.StartTime;

                    TimeLineCanvas.Current.UpdateDragStartCommand(dt);
                    break;

                case HandleDragMode.End:
                    var snapToEnd = _snapHandler.CheckForSnapping(clip.EndTime + dt);
                    if (!double.IsNaN(snapToEnd))
                        dt = snapToEnd - clip.EndTime;

                    TimeLineCanvas.Current.UpdateDragEndCommand(dt);
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

            var index = 0;
            foreach (var layer in _compositionOp.Symbol.Animator.Layers)
            {
                if (index >= layerMinIndex && index <= layerMaxIndex)
                {
                    var matchingItems = layer.Clips.FindAll(clip => clip.StartTime <= endTime
                                                                    && clip.EndTime >= startTime);
                    switch (selectMode)
                    {
                        case SelectMode.Add:
                        case SelectMode.Replace:
                            _selectedItems.UnionWith(matchingItems);
                            break;
                        case SelectMode.Remove:
                            _selectedItems.ExceptWith(matchingItems);
                            break;
                    }
                }

                index++;
            }
        }

//        public Command DeleteSelectedElements()
//        {
//            throw new System.NotImplementedException();
//        }

        ICommand ITimeElementSelectionHolder.StartDragCommand()
        {
            _moveClipsCommand = new MoveTimeClipCommand(_compositionOp.Symbol.Id, _selectedItems.ToList());
            return _moveClipsCommand;
        }

        void ITimeElementSelectionHolder.UpdateDragCommand(double dt)
        {
            foreach (var clip in _selectedItems)
            {
                clip.StartTime += dt;
                clip.EndTime += dt;
            }
        }

        void ITimeElementSelectionHolder.UpdateDragStartCommand(double dt)
        {
            foreach (var clip in _selectedItems)
            {
                // Keep 1 frame min duration
                clip.StartTime = Math.Min(clip.StartTime + dt, clip.EndTime - 1 / 60f);
            }
        }


        void ITimeElementSelectionHolder.UpdateDragEndCommand(double dt)
        {
            foreach (var clip in _selectedItems)
            {
                // Keep 1 frame min duration
                clip.EndTime = Math.Max(clip.EndTime + dt, clip.StartTime + 1 / 60f);
            }
        }

        void ITimeElementSelectionHolder.CompleteDragCommand()
        {
            if (_moveClipsCommand == null)
                return;

            _moveClipsCommand.StoreCurrentValues();
            UndoRedoStack.Add(_moveClipsCommand);
            _moveClipsCommand = null;
        }

        #endregion


        #region implement snapping interface -----------------------------------

        private const float SnapDistance = 4;
        private double _snapThresholdOnCanvas;

        /// <summary>
        /// Snap to all non-selected Clips
        /// </summary>
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime)
        {
            _snapThresholdOnCanvas = TimeLineCanvas.Current.InverseTransformDirection(new Vector2(SnapDistance, 0)).X;
            var maxForce = 0.0;
            var bestSnapTime = double.NaN;

            foreach (var clip in _compositionOp.Symbol.Animator.GetAllTimeClips())
            {
                if (_selectedItems.Contains(clip))
                    continue;

                CheckForSnapping(targetTime, clip.StartTime, maxForce: ref maxForce, bestSnapTime: ref bestSnapTime);
                CheckForSnapping(targetTime, clip.EndTime, maxForce: ref maxForce, bestSnapTime: ref bestSnapTime);
            }

            return double.IsNaN(bestSnapTime)
                       ? null
                       : new SnapResult(bestSnapTime, maxForce);
        }

        private void CheckForSnapping(double targetTime, double anchorTime, ref double maxForce, ref double bestSnapTime)
        {
            var distance = Math.Abs(anchorTime - targetTime);
            if (distance < 0.001)
                return;

            var force = Math.Max(0, _snapThresholdOnCanvas - distance);
            if (force <= maxForce)
                return;

            bestSnapTime = anchorTime;
            maxForce = force;
        }

        #endregion

        private Vector2 _minScreenPos;

        private readonly HashSet<Animator.Clip> _selectedItems = new HashSet<Animator.Clip>();
        private static MoveTimeClipCommand _moveClipsCommand;
        private const int LayerHeight = 25;
        private const float HandleWidth = 5;
        private readonly Vector2 _handleOffset = new Vector2(HandleWidth, 0);

        private ImDrawListPtr _drawList;
        private Instance _compositionOp;
        private readonly ValueSnapHandler _snapHandler;
    }
}