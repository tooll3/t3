using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction.Snapping;
using UiHelpers;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Windows.TimeLine
{
    public class CurveEditArea : ITimeElementSelectionHolder, IValueSnapAttractor
    {
        public CurveEditArea(TimeLineCanvas timeLineCanvas)
        {
            _timeLineCanvas = timeLineCanvas;
            _curveEditBox = new CurveEditBox(timeLineCanvas);
        }
        
        
        private List<GraphWindow.AnimationParameter> _animationParameters;

        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters, bool bringCurvesIntoView= false)
        {
            _drawList = ImGui.GetWindowDrawList();
            _animationParameters = animationParameters;
            
            if (bringCurvesIntoView)
                ViewAllOrSelectedKeys();

            ImGui.BeginGroup();
            {
                foreach (var param in animationParameters)
                {
                    foreach (var curve in param.Curves)
                    {
                        DrawCurveLine(curve);
                    }
                }

                foreach (var keyframe in GetAllKeyframes())
                {
                    CurvePointUi.Draw(keyframe, _timeLineCanvas, _selectedKeyframes.Contains(keyframe));
                }

                DrawContextMenu();
                //_curveEditBox.Draw();
            }
            ImGui.EndGroup();

            RebuildCurveTables();
        }
        

        private bool _contextMenuIsOpen;

        private void DrawContextMenu()
        {
            // This is a horrible hack to distinguish right mouse click from right mouse drag
            var rightMouseDragDelta = (ImGui.GetIO().MouseClickedPos[1] - ImGui.GetIO().MousePos).Length();
            if (!_contextMenuIsOpen && rightMouseDragDelta > 3)
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
            if (ImGui.BeginPopupContextWindow("context_menu"))
            {
                _contextMenuIsOpen = true;
                var selectedInterpolations = GetSelectedKeyframeInterpolationTypes();

                var editModes = selectedInterpolations as VDefinition.EditMode[] ?? selectedInterpolations.ToArray();

                if (ImGui.MenuItem("Smooth", null, editModes.Contains(VDefinition.EditMode.Smooth)))
                    OnSmooth();

                if (ImGui.MenuItem("Cubic", null, editModes.Contains(VDefinition.EditMode.Cubic)))
                    OnCubic();

                if (ImGui.MenuItem("Horizontal", null, editModes.Contains(VDefinition.EditMode.Horizontal)))
                    OnHorizontal();

                if (ImGui.MenuItem("Constant", null, editModes.Contains(VDefinition.EditMode.Constant)))
                    OnConstant();

                if (ImGui.MenuItem("Linear", null, editModes.Contains(VDefinition.EditMode.Linear)))
                    OnLinear();

                if (ImGui.MenuItem(_selectedKeyframes.Count > 0 ? "View Selected" : "View All", "F"))
                    ViewAllOrSelectedKeys();

                ImGui.EndPopup();
            }
            else
            {
                _contextMenuIsOpen = false;
            }

            ImGui.PopStyleVar();
        }
        

        #region update children
        private void ViewAllOrSelectedKeys()
        {
            const float curveValuePadding = 0.3f;

            var minU = double.PositiveInfinity;
            var maxU = double.NegativeInfinity;
            var minV = double.PositiveInfinity;
            var maxV = double.NegativeInfinity;
            var numPoints = 0;

            switch (_selectedKeyframes.Count)
            {
                case 0:
                {
                    foreach (var vDef in GetAllKeyframes())
                    {
                        numPoints++;
                        minU = Math.Min(minU, vDef.U);
                        maxU = Math.Max(maxU, vDef.U);
                        minV = Math.Min(minV, vDef.Value);
                        maxV = Math.Max(maxV, vDef.Value);
                    }

                    break;
                }
                case 1:
                    return;

                default:
                {
                    foreach (var element in _selectedKeyframes)
                    {
                        numPoints++;
                        minU = Math.Min(minU, element.U);
                        maxU = Math.Max(maxU, element.U);
                        minV = Math.Min(minV, element.Value);
                        maxV = Math.Max(maxV, element.Value);
                    }

                    break;
                }
            }

            if (numPoints == 0)
            {
                minV = -3;
                maxV = +3;
                minU = -2;
                maxU = 10;
            }

            if (maxU == minU)
            {
                minU -= 1;
            }

            if (maxV == minU)
            {
                maxV += -1;
                minV -= 1;
            }

            var height = ImGui.GetWindowContentRegionMax().Y - ImGui.GetWindowContentRegionMin().Y;
            var scale = -(float)(height / ((maxV - minV) * (1 + 2 * curveValuePadding)));
            _timeLineCanvas.SetVisibleValueRange(scale, (float)maxV - 20 / scale);
        }

        private void OnSmooth()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;
                                         vDef.InEditMode = VDefinition.EditMode.Smooth;
                                         vDef.InType = VDefinition.Interpolation.Spline;
                                         vDef.OutEditMode = VDefinition.EditMode.Smooth;
                                         vDef.OutType = VDefinition.Interpolation.Spline;
                                     });
        }

        private void OnCubic()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;
                                         vDef.InEditMode = VDefinition.EditMode.Cubic;
                                         vDef.InType = VDefinition.Interpolation.Spline;
                                         vDef.OutEditMode = VDefinition.EditMode.Cubic;
                                         vDef.OutType = VDefinition.Interpolation.Spline;
                                     });
        }

        private void OnHorizontal()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;

                                         vDef.InEditMode = VDefinition.EditMode.Horizontal;
                                         vDef.InType = VDefinition.Interpolation.Spline;
                                         vDef.InTangentAngle = 0;

                                         vDef.OutEditMode = VDefinition.EditMode.Horizontal;
                                         vDef.OutType = VDefinition.Interpolation.Spline;
                                         vDef.OutTangentAngle = Math.PI;
                                     });
        }

        private void OnConstant()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = true;
                                         vDef.OutType = VDefinition.Interpolation.Constant;
                                         vDef.OutEditMode = VDefinition.EditMode.Constant;
                                     });
        }

        private void OnLinear()
        {
            ForSelectedOrAllPointsDo((vDef) =>
                                     {
                                         vDef.BrokenTangents = false;
                                         vDef.InEditMode = VDefinition.EditMode.Linear;
                                         vDef.InType = VDefinition.Interpolation.Linear;
                                         vDef.OutEditMode = VDefinition.EditMode.Linear;
                                         vDef.OutType = VDefinition.Interpolation.Linear;
                                     });
        }

        private IEnumerable<VDefinition.EditMode> GetSelectedKeyframeInterpolationTypes()
        {
            var checkedInterpolationTypes = new HashSet<VDefinition.EditMode>();
            foreach (var point in GetSelectedOrAllPoints())
            {
                checkedInterpolationTypes.Add(point.OutEditMode);
                checkedInterpolationTypes.Add(point.InEditMode);
            }

            return checkedInterpolationTypes;
        }
        #endregion

        #region helper functions     
        /// <summary>
        /// Helper function to extract vDefs from all or selected UI controls across all curves in CurveEditor
        /// </summary>
        /// <returns>a list curves with a list of vDefs</returns>
        private IEnumerable<VDefinition> GetSelectedOrAllPoints()
        {
            var result = new List<VDefinition>();

            if (_selectedKeyframes.Count > 0)
            {
                result.AddRange(_selectedKeyframes);
            }
            else
            {
                foreach (var curve in _animationParameters.SelectMany(param => param.Curves))
                {
                    result.AddRange(curve.GetVDefinitions());
                }
            }

            return result;
        }

        private delegate void DoSomethingWithKeyframeDelegate(VDefinition v);

        private void ForSelectedOrAllPointsDo(DoSomethingWithKeyframeDelegate doFunc)
        {
            UpdateCurveAndMakeUpdateKeyframeCommands(doFunc);
        }
        #endregion

        private void UpdateCurveAndMakeUpdateKeyframeCommands(DoSomethingWithKeyframeDelegate doFunc)
        {
            foreach (var keyframe in GetSelectedOrAllPoints())
            {
                doFunc(keyframe);
            }
        }

        public void ClearSelection()
        {
            _selectedKeyframes.Clear();
        }




        public void UpdateSelectionForArea(ImRect screenArea, SelectMode selectMode)
        {
            if (selectMode == SelectMode.Replace)
                _selectedKeyframes.Clear();

            var canvasArea = TimeLineCanvas.Current.InverseTransformRect(screenArea);
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
                case SelectMode.Add:
                case SelectMode.Replace:
                    _selectedKeyframes.UnionWith(matchingItems);
                    break;
                case SelectMode.Remove:
                    _selectedKeyframes.ExceptWith(matchingItems);
                    break;
            }

            //SelectionHandler.SetElements(matchingItems);
        }

        public ICommand StartDragCommand()
        {
            //throw new NotImplementedException();
            return null;
        }

        public void UpdateDragCommand(double dt,double dv)
        {
            //throw new NotImplementedException();
        }

        public void CompleteDragCommand()
        {
            // throw new NotImplementedException();
        }

        public void UpdateDragStartCommand(double dt, double dv)
        {
            //throw new NotImplementedException();
        }

        public void UpdateDragEndCommand(double dt, double dv)
        {
            //throw new NotImplementedException();
        }

        public SnapResult CheckForSnap(double value)
        {
            //throw new NotImplementedException();
            return null;
        }
        
        
        private IEnumerable<VDefinition> GetAllKeyframes()
        {
            return from param in _animationParameters 
                   from curve in param.Curves 
                   from keyframe in curve.GetVDefinitions() 
                   select keyframe;
        }

        private  void DrawCurveLine(Curve curve)
        {
            const float step = 3f;
            var width = ImGui.GetWindowWidth();

            double dU = _timeLineCanvas.InverseTransformDirection(new Vector2(step, 0)).X;
            double u = _timeLineCanvas.InverseTransformPosition(_timeLineCanvas.WindowPos).X;
            var x = _timeLineCanvas.WindowPos.X;

            var steps = (int)(width / step);
            if (_curveLinePoints.Length != steps)
            {
                _curveLinePoints = new Vector2[steps];
            }

            for (var i = 0; i < steps; i++)
            {
                _curveLinePoints[i] = new Vector2(x,_timeLineCanvas.TransformPosition(new Vector2(0, (float)curve.GetSampledValue(u))).Y);
                u += dU;
                x += step;
            }

            _drawList.AddPolyline(ref _curveLinePoints[0], steps, Color.Gray, false, 1);
        }

        
        /// <summary>
        /// A horrible hack to keep curve table-structure aligned with position stored in key definitions.
        /// </summary>
        private void RebuildCurveTables()
        {
            foreach (var param in _animationParameters)
            {
                foreach (var curve in param.Curves)
                {
                    foreach (var (u, vDef) in curve.GetPointTable())
                    {
                        if (Math.Abs(u - vDef.U) > 0.001f)
                        {
                            curve.MoveKey(u, vDef.U);
                        }
                    }
                }
            }
        }
        
        private static Vector2[] _curveLinePoints = new Vector2[0];

        private readonly HashSet<VDefinition> _selectedKeyframes = new HashSet<VDefinition>();
        private static ImDrawListPtr _drawList;
        private readonly TimeLineCanvas _timeLineCanvas;
        private readonly CurveEditBox _curveEditBox;
    }
}