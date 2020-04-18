using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Interaction.WithCurves;
using T3.Gui.UiHelpers;
using UiHelpers;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Windows.TimeLine
{
    public class TimelineCurveEditArea : AnimationParameterEditing, ITimeObjectManipulation, IValueSnapAttractor
    {
        public TimelineCurveEditArea(TimeLineCanvas timeLineCanvas, ValueSnapHandler snapHandler)
        {
            _snapHandler = snapHandler;
            TimeLineCanvas = timeLineCanvas;
        }

        
        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters, bool bringCurvesIntoView = false)
        {
            _compositionOp = compositionOp;
            AnimationParameters = animationParameters;

            if (bringCurvesIntoView)
                ViewAllOrSelectedKeys();

            ImGui.BeginGroup();
            {
                if (KeyboardBinding.Triggered(UserActions.FocusSelection))
                    ViewAllOrSelectedKeys(alsoChangeTimeRange:true);
                
                if(KeyboardBinding.Triggered(UserActions.Duplicate))
                    DuplicateSelectedKeyframes(TimeLineCanvas.Playback.TimeInBars);                
                
                foreach (var param in animationParameters)
                {
                    foreach (var curve in param.Curves)
                    {
                        DrawCurveLine(curve, TimeLineCanvas);
                    }
                }

                foreach (var keyframe in GetAllKeyframes().ToArray())
                {
                    CurvePoint.Draw(keyframe, TimeLineCanvas, SelectedKeyframes.Contains(keyframe), this);
                }

                DrawContextMenu();
            }
            ImGui.EndGroup();

            RebuildCurveTables();
        }

        protected internal override void HandleCurvePointDragging(VDefinition vDef, bool isSelected)
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
                TimeLineCanvas.Current.StartDragCommand();
            }


            var newDragPosition = TimeLineCanvas.Current.InverseTransformPosition(ImGui.GetIO().MousePos);
            double u = newDragPosition.X;
            _snapHandler.CheckForSnapping(ref u);
            
            var dY = newDragPosition.Y - vDef.Value;
            TimeLineCanvas.Current.UpdateDragCommand(u - vDef.U, dY);
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

            _changeKeyframesCommand.StoreCurrentValues();
            UndoRedoStack.Add(_changeKeyframesCommand);
            _changeKeyframesCommand = null;
        }

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
        }

        #region  implement snapping -------------------------
        SnapResult IValueSnapAttractor.CheckForSnap(double targetTime)
        {
            _snapThresholdOnCanvas = TimeLineCanvas.Current.InverseTransformDirection(new Vector2(SnapDistance, 0)).X;
            var maxForce = 0.0;
            var bestSnapTime = double.NaN;

            foreach (var vDefinition in GetAllKeyframes())
            {
                if (SelectedKeyframes.Contains(vDefinition))
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

        private double _snapThresholdOnCanvas;
        private const float SnapDistance = 4;
        #endregion


        public static void DrawCurveLine(Curve curve, ICanvas canvas)
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

            ImGui.GetWindowDrawList().AddPolyline(ref _curveLinePoints[0], steps, Color.Gray, false, 1);
        }

        
        private static ChangeKeyframesCommand _changeKeyframesCommand;
        private static Vector2[] _curveLinePoints = new Vector2[0];
        private Instance _compositionOp;
        private readonly ValueSnapHandler _snapHandler;
    }
}