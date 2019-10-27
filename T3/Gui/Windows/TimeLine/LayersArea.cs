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

        private Vector2 _minScreenPos;

        public void Draw(Instance compositionOp)
        {
            var animator = compositionOp.Symbol.Animator;
            _compositionOp = compositionOp;

            ImGui.BeginGroup();
            _minScreenPos = ImGui.GetCursorScreenPos();
            
            THelpers.DebugWindowRect();

            foreach (var layer in animator.Layers)
            {
                DrawLayer(layer);
            }

            ImGui.EndGroup();
        }

        private Instance _compositionOp;


        private void DrawLayer(Animator.Layer layer)
        {
            ImGui.InvisibleButton("Layer#layer", new Vector2(0, LayerHeight - 1));

            var min = ImGui.GetItemRectMin();
            var max = ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddRectFilled(new Vector2(min.X, max.Y - 1),
                                                    new Vector2(max.X, max.Y), Color.Black);

            var layerArea = new ImRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            foreach (var clip in layer.Clips)
            {
                DrawClip(layerArea, clip);
            }
        }


        private void DrawClip(ImRect layerArea, Animator.Clip clip)
        {
            var xStartTime = TimeLineCanvas.Current.TransformPositionX((float)clip.StartTime);
            var xEndTime = TimeLineCanvas.Current.TransformPositionX((float)clip.EndTime);
            var position = new Vector2(xStartTime, layerArea.Min.Y);
            var size = new Vector2(xEndTime - xStartTime, layerArea.GetHeight());
            ImGui.SetCursorScreenPos(position);
            ImGui.PushID(clip.Id.GetHashCode());
            var isSelected = _selectedItems.Contains(clip);
            var color = isSelected ? Color.Red : Color.Gray;

            ImGui.GetWindowDrawList().AddRectFilled(position, position + size, color);

            // Item Clicked
            if (ImGui.InvisibleButton(clip.Name, size))
            {
                TimeLineCanvas.Current.CompleteDragCommand();

                if (_moveClipsCommand != null)
                {
                    _moveClipsCommand.StoreCurrentValues();
                    UndoRedoStack.Add(_moveClipsCommand);
                    _moveClipsCommand = null;
                }
            }

            HandleDragging(clip, isSelected);

            ImGui.PopID();
        }


        private void HandleDragging(Animator.Clip clip, bool isSelected)
        {
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
            var snapToStart = _snapHandler.CheckForSnapping(clip.StartTime + dt);
            if (!double.IsNaN(snapToStart))
                dt = snapToStart - clip.StartTime;

            var snapToEnd = _snapHandler.CheckForSnapping(clip.EndTime + dt);
            if (!double.IsNaN(snapToEnd))
                dt = snapToEnd - clip.EndTime;
            
            TimeLineCanvas.Current.UpdateDragCommand(dt);
        }


        void ITimeElementSelectionHolder.ClearSelection()
        {
            _selectedItems.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectMode selectMode)
        {
            if(selectMode == SelectMode.Replace)
                _selectedItems.Clear();
            
            var startTime = TimeLineCanvas.Current.InverseTransformPositionX(screenArea.Min.X);
            var endTime = TimeLineCanvas.Current.InverseTransformPositionX(screenArea.Max.X);

            var layerMinIndex = (screenArea.Min.Y - _minScreenPos.Y) / LayerHeight-1;
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

        void ITimeElementSelectionHolder.CompleteDragCommand()
        {
            if (_moveClipsCommand == null)
                return;

            _moveClipsCommand.StoreCurrentValues();
            UndoRedoStack.Add(_moveClipsCommand);
            _moveClipsCommand = null;
        }


        private const float SnapDistance = 4;
        private double _snapThresholdOnCanvas;

        /// <summary>
        /// Snap to all non-selected Clips
        /// </summary>
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime)
        {
            _snapThresholdOnCanvas = TimeLineCanvas.Current.InverseTransformDirection(new Vector2(SnapDistance,0)).X;
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

        private readonly HashSet<Animator.Clip> _selectedItems = new HashSet<Animator.Clip>();
        private static MoveTimeClipCommand _moveClipsCommand;
        private const int LayerHeight = 25;

        private ValueSnapHandler _snapHandler;
    }
}