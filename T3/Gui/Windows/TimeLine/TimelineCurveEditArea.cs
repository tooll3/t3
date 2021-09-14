using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.InputUi;
using T3.Gui.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Interaction.WithCurves;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using UiHelpers;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Windows.TimeLine
{
    public class TimelineCurveEditArea : AnimationParameterEditing, ITimeObjectManipulation, IValueSnapAttractor
    {
        public TimelineCurveEditArea(TimeLineCanvas timeLineCanvas, ValueSnapHandler snapHandlerForU, ValueSnapHandler snapHandlerV)
        {
            SnapHandlerU = snapHandlerForU;
            SnapHandlerV = snapHandlerV;
            TimeLineCanvas = timeLineCanvas;
        }

        private StringBuilder _stringBuilder = new StringBuilder(100);
        
        public void Draw(Instance compositionOp, List<TimeLineCanvas.AnimationParameter> animationParameters, bool fitCurvesVertically = false)
        {
            _compositionOp = compositionOp;
            AnimationParameters = animationParameters;

            if (fitCurvesVertically)
            {
                var bounds = GetBoundsOnCanvas(GetSelectedOrAllPoints());
                TimeLineCanvas.Current.SetVerticalScopeToCanvasArea(bounds, flipY:true);
            }
                
            

            ImGui.BeginGroup();
            {
                var drawList = ImGui.GetWindowDrawList();
                drawList.ChannelsSplit(3);
                if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                    ViewAllOrSelectedKeys(alsoChangeTimeRange:true);
                
                if(KeyboardBinding.Triggered(UserActions.Duplicate))
                    DuplicateSelectedKeyframes(TimeLineCanvas.Playback.TimeInBars);                
                
                var lineStartPosition = ImGui.GetCursorPos();
                
                ImGui.PushFont(Fonts.FontSmall);
                foreach (var param in animationParameters)
                {
                    ImGui.PushID(param.Input.GetHashCode());
                    drawList.ChannelsSetCurrent(1);
                    var hash = param.Input.GetHashCode();
                    var isParamPinned = PinnedParameterComponents.ContainsKey(hash);

                    _stringBuilder.Clear();
                    _stringBuilder.Append(param.Instance.Symbol.Name);
                    _stringBuilder.Append(".");
                    _stringBuilder.Append(param.Input.Input.Name);
                    var paramName = _stringBuilder.ToString();// param.Instance.Symbol.Name + "." + param.Input.Input.Name;

                    var cursorPosition = lineStartPosition; 
                    ImGui.SetCursorPos(cursorPosition);
                    
                    if (DrawPinButton(isParamPinned, paramName))
                    {
                        if (isParamPinned)
                        {
                            PinnedParameterComponents.Remove(hash);
                            isParamPinned = false;
                        }
                        else
                        {
                            PinnedParameterComponents[hash] = 0xffff;
                            isParamPinned = true;
                        }
                    }
                    
                    cursorPosition += new Vector2(ImGui.GetItemRectSize().X,0 );
                    
                    var isParamHovered = ImGui.IsItemHovered();

                    var curveNames = param.Input.ValueType == typeof(Vector4) 
                                         ? DopeSheetArea.ColorCurveNames 
                                         : DopeSheetArea.CurveNames;
                    
                    var curveIndex = 0;
                    foreach (var curve in param.Curves)
                    {
                        //ImGui.SameLine();
                        ImGui.SetCursorPos(cursorPosition);
                        
                        var componentName = curveNames[curveIndex % curveNames.Length];
                        var bit = 1 << curveIndex;

                        var isParamComponentPinned = isParamPinned && (PinnedParameterComponents[hash] & bit) != 0; 
                        if (DrawPinButton(isParamComponentPinned, componentName))
                        {
                            if (isParamComponentPinned)
                            {
                                var flags = PinnedParameterComponents[hash] ^ bit;
                                if (flags == 0)
                                {
                                    PinnedParameterComponents.Remove(hash);
                                    isParamPinned = false;
                                }
                                else
                                {
                                    PinnedParameterComponents[hash] = flags;
                                }
                            }
                            else
                            {
                                if (isParamPinned)
                                {
                                    PinnedParameterComponents[hash] |= bit;
                                }
                                else
                                {
                                    PinnedParameterComponents[hash] = bit;
                                }
                            }

                            isParamComponentPinned = !isParamComponentPinned;
                        }
                        cursorPosition += new Vector2(ImGui.GetItemRectSize().X,0 );
                        ImGui.SetCursorPos(cursorPosition);
                        
                        var isParamComponentHovered = ImGui.IsItemHovered();

                        drawList.ChannelsSetCurrent(0);

                        var shouldDrawCurve = PinnedParameterComponents.Count == 0 || isParamComponentPinned;
                        if (shouldDrawCurve)
                        {
                            var color = DopeSheetArea.CurveColors[curveIndex % DopeSheetArea.CurveColors.Length];
                            DrawCurveLine(curve, TimeLineCanvas, color, isParamHovered || isParamComponentHovered);
                            var keepCursorPos = ImGui.GetCursorPos();
                            drawList.ChannelsSetCurrent(1);
                            foreach (var keyframe in curve.GetVDefinitions().ToList())
                            {
                                CurvePoint.Draw(keyframe, TimeLineCanvas, SelectedKeyframes.Contains(keyframe), this);
                            }
                        }
                        curveIndex++;
                    }
                    ImGui.PopID();
                    lineStartPosition += new Vector2(0, 24);
                }
                drawList.ChannelsMerge();
                ImGui.PopFont();

                // foreach (var keyframe in GetAllKeyframes().ToArray())
                // {
                //     CurvePoint.Draw(keyframe, TimeLineCanvas, SelectedKeyframes.Contains(keyframe), this);
                // }

                DrawContextMenu();
            }
            ImGui.EndGroup();

            RebuildCurveTables();
        }

        private static bool DrawPinButton(bool isParamComponentPinned, string componentName)
        {
            
            var buttonColor = isParamComponentPinned ? Color.Orange : Color.Gray;
            ImGui.PushStyleColor(ImGuiCol.Text, buttonColor.Rgba);
            var result = ImGui.Button(componentName);
            ImGui.PopStyleColor();
            return result;
        }

        protected internal override void HandleCurvePointDragging(VDefinition vDef, bool isSelected)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            }

            if (ImGui.IsItemDeactivated())
            {
                if(_changeKeyframesCommand != null)
                    TimeLineCanvas.Current.CompleteDragCommand();
            }
            
            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 0f))
                return;


            if (ImGui.GetIO().KeyCtrl && _changeKeyframesCommand == null)
            {
                if (isSelected)
                    SelectedKeyframes.Remove(vDef);

                return;
            }
            
            if (!isSelected)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    TimeLineCanvas.Current.ClearSelection();
                }

                SelectedKeyframes.Add(vDef);
            }

            if (_changeKeyframesCommand == null)
            {
                var mouseDragDelta = ImGui.GetMouseDragDelta();
                if (CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Undecided)
                {

                    if (Math.Abs(mouseDragDelta.X) > CurveInputEditing.MoveDirectionThreshold)
                    {
                        CurveInputEditing.MoveDirection = CurveInputEditing.MoveDirections.Horizontal;
                        TimeLineCanvas.Current.StartDragCommand();
                    }
                    else if (Math.Abs(mouseDragDelta.Y) > CurveInputEditing.MoveDirectionThreshold)
                    {
                        CurveInputEditing.MoveDirection = CurveInputEditing.MoveDirections.Vertical;
                        TimeLineCanvas.Current.StartDragCommand();
                    }
                }
            }
            
            var newDragPosition = TimeLineCanvas.Current.InverseTransformPosition(ImGui.GetIO().MousePos);

            var allowHorizontal = CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Both
                                   || CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Horizontal
                                   || (ImGui.GetIO().KeyCtrl);
            
            var allowVertical = CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Both
                                  || CurveInputEditing.MoveDirection == CurveInputEditing.MoveDirections.Vertical
                                  || (ImGui.GetIO().KeyCtrl);
            
            double u = allowHorizontal ? newDragPosition.X : vDef.U;
            if(!ImGui.GetIO().KeyShift)
                SnapHandlerU.CheckForSnapping(ref u, TimeLineCanvas.Scale.X);
            
            double v = allowVertical ?  newDragPosition.Y : vDef.Value;
            if(!ImGui.GetIO().KeyShift)
                SnapHandlerV.CheckForSnapping(ref v, TimeLineCanvas.Scale.Y);
            
            UpdateDragCommand(u - vDef.U, v - vDef.Value);
        }
        
        void ITimeObjectManipulation.DeleteSelectedElements()
        {
            KeyframeOperations.DeleteSelectedKeyframesFromAnimationParameters(SelectedKeyframes, AnimationParameters);
            RebuildCurveTables();
        }
        

        public void ClearSelection()
        {
            SelectedKeyframes.Clear();
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            if (selectMode == SelectionFence.SelectModes.Replace)
                SelectedKeyframes.Clear();

            THelpers.DebugRect(screenArea.Min, screenArea.Max);
            var canvasArea = TimeLineCanvas.Current.InverseTransformRect(screenArea).MakePositive();
            var matchingItems = new List<VDefinition>();

            foreach (var keyframe in GetAllKeyframes())
            {
                if (canvasArea.Contains(new Vector2((float)keyframe.U, (float)keyframe.Value)))
                {
                    matchingItems.Add(keyframe);
                }
            }

            switch (selectMode)
            {
                case SelectionFence.SelectModes.Add:
                case SelectionFence.SelectModes.Replace:
                    SelectedKeyframes.UnionWith(matchingItems);
                    break;
                case SelectionFence.SelectModes.Remove:
                    SelectedKeyframes.ExceptWith(matchingItems);
                    break;
            }
        }

        public ICommand StartDragCommand()
        {
            _changeKeyframesCommand = new ChangeKeyframesCommand(_compositionOp.Symbol.Id, SelectedKeyframes);
            return _changeKeyframesCommand;
        }

        public void UpdateDragCommand(double dt, double dv)
        {
            foreach (var vDefinition in SelectedKeyframes)
            {
                vDefinition.U += dt;
                vDefinition.Value += dv;
            }
            RebuildCurveTables();
        }

        public void CompleteDragCommand()
        {
            if (_changeKeyframesCommand == null)
                return;

            CurveInputEditing.MoveDirection = CurveInputEditing.MoveDirections.Undecided;
            // Update reference in macro command
            _changeKeyframesCommand.StoreCurrentValues();
            //UndoRedoStack.Add(_changeKeyframesCommand);
            _changeKeyframesCommand = null;
        }

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
        }

        #region  implement snapping -------------------------
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime, float canvasScale)
        {
            _snapThresholdOnCanvas = SnapDistance / canvasScale;;
            var maxForce = 0.0;
            var bestSnapTime = Double.NaN;

            foreach (var vDefinition in GetAllKeyframes())
            {
                if (SelectedKeyframes.Contains(vDefinition))
                    continue;

                CheckForSnapping(targetTime, vDefinition.U, maxForce: ref maxForce, bestSnapTime: ref bestSnapTime);
            }

            return Double.IsNaN(bestSnapTime)
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

        private double _snapThresholdOnCanvas;
        private const float SnapDistance = 4;
        #endregion


        public static void DrawCurveLine(Curve curve, ICanvas canvas, Color color, bool isParamHovered = false)
        {
            const float step = 3f;
            var width = ImGui.GetWindowWidth();

            double dU = canvas.InverseTransformDirection(new Vector2(step, 0)).X;
            double u = canvas.InverseTransformPosition(canvas.WindowPos).X;
            var x = canvas.WindowPos.X;

            var steps = (int)(width / step);
            if (_curveLinePoints.Length != steps)
            { 
                _curveLinePoints = new Vector2[steps];
            }

            for (var i = 0; i < steps; i++)
            {
                _curveLinePoints[i] = new Vector2(x, (int)canvas.TransformPosition(new Vector2(0, (float)curve.GetSampledValue(u))).Y - 0.5f);
                u += dU;
                x += step;
            }

            ImGui.GetWindowDrawList().AddPolyline(ref _curveLinePoints[0], steps, color, false, isParamHovered? 3: 1);
        }

        
        private static ChangeKeyframesCommand _changeKeyframesCommand;
        private static Vector2[] _curveLinePoints = new Vector2[0];
        private Instance _compositionOp;
        public readonly ValueSnapHandler SnapHandlerU;
        public readonly ValueSnapHandler SnapHandlerV;
        
        public readonly Dictionary<int, int> PinnedParameterComponents = new Dictionary<int, int>();
    }
}