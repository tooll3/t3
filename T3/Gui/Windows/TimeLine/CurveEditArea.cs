using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Graph;
using T3.Gui.Selection;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace T3.Gui.Windows.TimeLine
{
    public class CurveEditArea
    {
        public CurveEditArea(TimeLineCanvas timeLineCanvas)
        {
            _timeLineCanvas = timeLineCanvas;
            _curveEditBox = new CurveEditBox(TimeLineCanvas.Current, SelectionHandler);
        }

        public void Draw(Instance compositionOp, List<GraphWindow.AnimationParameter> animationParameters)
        {
            var curves = new List<Curve>();
            foreach (var p in animationParameters)
            {
                curves.AddRange(p.Curves);
            }

            SetCurves(curves);

            ImGui.BeginGroup();
            {
                DrawCurves();
                DrawContextMenu();
                _curveEditBox.Draw();
            }
            ImGui.EndGroup();
        }

        private void SetCurves(List<Curve> newCurveSelection)
        {
            var existingCurves = _curvesWithUi.Keys.ToArray();
            var someCurvesUnselected = false;
            var someNewCurvesSelected = false;

            foreach (var c in existingCurves)
            {
                if (newCurveSelection.Contains(c))
                    continue;

                _curvesWithUi[c].CurvePoints.ForEach(cpc => SelectionHandler.RemoveElement(cpc));
                _curvesWithUi.Remove(c);
                someCurvesUnselected = true;
            }

            if (newCurveSelection.Count == 0)
                return;

            foreach (var newCurve in newCurveSelection)
            {
                if (!_curvesWithUi.ContainsKey(newCurve))
                {
                    _curvesWithUi[newCurve] = new CurveUi(newCurve, TimeLineCanvas.Current);
                    someNewCurvesSelected = true;
                }
            }

            if (someCurvesUnselected || someNewCurvesSelected)
            {
                ViewAllOrSelectedKeys();
            }
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

                if (ImGui.MenuItem(SelectionHandler.SelectedElements.Any() ? "View Selected" : "View All", "F"))
                    ViewAllOrSelectedKeys();

                ImGui.EndPopup();
            }
            else
            {
                _contextMenuIsOpen = false;
            }

            ImGui.PopStyleVar();
        }

        private void DrawCurves()
        {
            foreach (var c in _curvesWithUi.Values)
            {
                c.Draw();
            }
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

            switch (SelectionHandler.SelectedElements.Count)
            {
                case 0:
                {
                    foreach (var pair in _curvesWithUi)
                    {
                        Curve curve = pair.Key;

                        foreach (var pair2 in curve.GetPoints())
                        {
                            numPoints++;
                            double u = pair2.Key;
                            var vDef = pair2.Value;
                            minU = Math.Min(minU, u);
                            maxU = Math.Max(maxU, u);
                            minV = Math.Min(minV, vDef.Value);
                            maxV = Math.Max(maxV, vDef.Value);
                        }
                    }

                    break;
                }
                case 1:
                    return;

                default:
                {
                    foreach (var element in SelectionHandler.SelectedElements)
                    {
                        if (!(element is CurvePointUi cpc))
                            continue;

                        numPoints++;
                        minU = Math.Min(minU, cpc.Key.U);
                        maxU = Math.Max(maxU, cpc.Key.U);
                        minV = Math.Min(minV, cpc.Key.Value);
                        maxV = Math.Max(maxV, cpc.Key.Value);
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
            var scale = (float)(height / ((maxV - minV) * (1 + 2 * curveValuePadding)));
            _timeLineCanvas.SetValueRange(scale, (float)minV - 20f / scale);
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
                checkedInterpolationTypes.Add(point.Key.OutEditMode);
                checkedInterpolationTypes.Add(point.Key.InEditMode);
            }

            return checkedInterpolationTypes;
        }
        #endregion

        /*
                private List<Curve> GetAllOrSelectedCurves()
                {
                    List<Curve> curves = new List<Curve>();
                    if (SelectionHandler.SelectedElements.Count == 0)
                    {
                        foreach (var curve in _curvesWithUi.Keys)
                        {
                            curves.Add(curve);
                        }
                    }
                    else
                    {
                        foreach (var el in SelectionHandler.SelectedElements)
                        {
                            var cpc = el as CurvePointUi;
                            if (cpc != null)
                            {
                                curves.Add(cpc.Curve);
                            }
                        }
                    }
                
                    return curves;
                }*/

        #region helper functions     
        /// <summary>
        /// Helper function to extract vdefs from all or selected UI controls across all curves in CurveEditor
        /// </summary>
        /// <returns>a list curves with a list of vDefs</returns>
        private IEnumerable<CurvePointUi> GetSelectedOrAllPoints()
        {
            var result = new List<CurvePointUi>();

            if (SelectionHandler.SelectedElements.Count > 0)
            {
                result.AddRange(SelectionHandler.SelectedElements.Cast<CurvePointUi>());
            }
            else
            {
                foreach (var curve in _curvesWithUi.Values)
                {
                    result.AddRange(curve.CurvePoints);
                }
            }

            return result;
        }

        delegate void DoSomethingWithVdefDelegate(VDefinition v);

        private void ForSelectedOrAllPointsDo(DoSomethingWithVdefDelegate doFunc)
        {
            UpdateCurveAndMakeUpdateKeyframeCommands(doFunc);
        }
        #endregion

        private void UpdateCurveAndMakeUpdateKeyframeCommands(DoSomethingWithVdefDelegate doFunc)
        {
            foreach (var point in GetSelectedOrAllPoints())
            {
                doFunc(point.Key);
            }
        }

        public List<ISelectable> SelectableChildren { get; set; }
        private SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();

        private readonly Dictionary<Curve, CurveUi> _curvesWithUi = new Dictionary<Curve, CurveUi>();
        private readonly TimeLineCanvas _timeLineCanvas;
        private readonly CurveEditBox _curveEditBox;
    }
}