using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Styling;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public class DopeSheetArea : ITimeElementSelectionHolder, IValueSnapAttractor
    {
        public DopeSheetArea(ValueSnapHandler snapHandler)
        {
            _snapHandler = snapHandler;
        }

        private List<GraphWindow.AnimationParameter> _animationParameters;
        private Instance _compositionOp;
        
        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters)
        {
            _drawList = ImGui.GetWindowDrawList();
            _animationParameters = animationParameters;
            _compositionOp = compositionOp;

            ImGui.BeginGroup();
            {
                ImGui.SetCursorPos(ImGui.GetCursorPos() + new Vector2(0, 3)); // keep some padding 
                _minScreenPos = ImGui.GetCursorScreenPos();

                foreach (var parameter in animationParameters)
                {
                    DrawProperty(parameter);
                }
            }
            ImGui.EndGroup();
        }


        private void DrawProperty(GraphWindow.AnimationParameter parameter)
        {
            var min = ImGui.GetCursorScreenPos();
            var max = min + new Vector2(ImGui.GetContentRegionAvail().X, LayerHeight - 1);
            _drawList.AddRectFilled(new Vector2(min.X, max.Y),
                                    new Vector2(max.X, max.Y + 1), Color.Black);

            var layerArea = new ImRect(min, max);
            ImGui.PushFont(Fonts.FontBold);
            ImGui.Text(parameter.Instance.Symbol.Name);
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text("." + parameter.Input.Input.Name);

            foreach (var curve in parameter.Curves)
            {
                foreach (var pair in curve.GetPoints())
                {
                    DrawKeyframe(pair.Value, layerArea, parameter);
                }
            }

            ImGui.SetCursorScreenPos(min + new Vector2(0, LayerHeight)); // Next Line
        }

        private const float KeyframeIconWidth = 10;
        private void DrawKeyframe(VDefinition vDef, ImRect layerArea, GraphWindow.AnimationParameter parameter)
        {
            var posOnScreen = new Vector2(
                                          TimeLineCanvas.Current.TransformPositionX((float)vDef.U) - KeyframeIconWidth/2, 
                                          layerArea.Min.Y);
            ImGui.PushID(vDef.GetHashCode());
            {
                var isSelected = _selectedItems.Contains(vDef);
                Icons.Draw(isSelected ? Icon.KeyFrameSelected : Icon.KeyFrame, posOnScreen);
                ImGui.SetCursorScreenPos(posOnScreen);

                // Clicked
                if (ImGui.InvisibleButton("##key", new Vector2(5, 24)))
                {
                    TimeLineCanvas.Current.CompleteDragCommand();

                    if (_changeKeyframesCommand != null)
                    {
                        _changeKeyframesCommand.StoreCurrentValues();
                        UndoRedoStack.Add(_changeKeyframesCommand);
                        _changeKeyframesCommand = null;
                    }
                }

                HandleDragging(vDef, isSelected);

                ImGui.PopID();
            }
        }

        private void HandleDragging(VDefinition vDef, bool isSelected)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            }

            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 0f))
                return;

            if (ImGui.GetIO().KeyCtrl)
            {
                if (isSelected)
                    _selectedItems.Remove(vDef);

                return;
            }

            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                } 
                
                _selectedItems.Add(vDef);
            }

            if (_changeKeyframesCommand == null)
            {
                TimeLineCanvas.Current.StartDragCommand();
            }

            double dt = TimeLineCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta).X;

            var snapClipToStart = _snapHandler.CheckForSnapping(vDef.U + dt);
            if (!double.IsNaN(snapClipToStart))
                dt = snapClipToStart - vDef.U;

            TimeLineCanvas.Current.UpdateDragCommand(dt);
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
            foreach (var parameter in _animationParameters)
            {
                if (index >= layerMinIndex && index <= layerMaxIndex)
                {
                    foreach (var c in parameter.Curves)
                    {
                        var matchingItems = c.GetPoints()
                                             .Select(pair => pair.Value)
                                             .ToList()
                                             .FindAll(key => key.U <= endTime && key.U >= startTime);
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
            _changeKeyframesCommand = new ChangeKeyframesCommand(_compositionOp.Symbol.Id, _selectedItems);
            return _changeKeyframesCommand;
        }

        void ITimeElementSelectionHolder.UpdateDragCommand(double dt)
        {
            foreach (var vDefinition in _selectedItems)
            {
                vDefinition.U += dt;
            }
        }

        void ITimeElementSelectionHolder.UpdateDragStartCommand(double dt) { }

        void ITimeElementSelectionHolder.UpdateDragEndCommand(double dt) { }

        void ITimeElementSelectionHolder.CompleteDragCommand()
        {
            if (_changeKeyframesCommand == null)
                return;

            _changeKeyframesCommand.StoreCurrentValues();
            UndoRedoStack.Add(_changeKeyframesCommand);
            _changeKeyframesCommand = null;
        }

        #endregion


        #region implement snapping interface -----------------------------------

        private const float SnapDistance = 4;
        private double _snapThresholdOnCanvas;
        
        public IEnumerable<VDefinition> GetAllKeyframes()
        {
            
            foreach (var param in _animationParameters)
            {
                foreach (var curve in param.Curves)
                {
                    foreach (var pair in curve.GetPoints())
                    {
                        yield return pair.Value;    
                    }
                }
            }
        }


        /// <summary>
        /// Snap to all non-selected Clips
        /// </summary>
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime)
        {
            _snapThresholdOnCanvas = TimeLineCanvas.Current.InverseTransformDirection(new Vector2(SnapDistance, 0)).X;
            var maxForce = 0.0;
            var bestSnapTime = double.NaN;

            foreach (var vDefinition in GetAllKeyframes())
            {
                if (_selectedItems.Contains(vDefinition))
                    continue;

                CheckForSnapping(targetTime, vDefinition.U, maxForce: ref maxForce, bestSnapTime: ref bestSnapTime);
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

        private readonly HashSet<VDefinition> _selectedItems = new HashSet<VDefinition>();
        private static ChangeKeyframesCommand _changeKeyframesCommand;
        private const int LayerHeight = 25;
        private const float HandleWidth = 5;
        private readonly Vector2 _handleOffset = new Vector2(HandleWidth, 0);

        private ImDrawListPtr _drawList;
        private readonly ValueSnapHandler _snapHandler;
    }
}