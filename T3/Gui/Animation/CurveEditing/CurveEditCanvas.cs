using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.Selection;
using UiHelpers;
using static T3.Core.Animation.Utils;

namespace T3.Gui.Animation.CurveEditing
{
    /// <summary>
    /// This class is currently obsolete 
    /// </summary>
    public class CurveEditCanvas : ICanvas
    {
        /// <summary>
        /// Creates at new CurveEditCanvas
        /// </summary>
        /// <param name="clipTime">
        /// An optional ClipTime that allows modifying time marker and regions
        /// </param>
        public CurveEditCanvas(ClipTime clipTime = null)
        {
            _clipTime = clipTime;
            _selectionFence = new SelectionFence(this);
            _curveEditBox = new CurveEditBox(this, SelectionHandler);
            _horizontalScaleLines = new HorizontalScaleLines(this);
        }

        /// <summary>
        /// Update visible curves
        /// </summary>
        public void SetCurves(List<Curve> newCurveSelection)
        {
            var existingCurves = _curvesWithUi.Keys.ToArray();
            var someCurvesUnselected = false;
            var someNewCurvesSelected = false;

            foreach (var c in existingCurves)
            {
                if (!newCurveSelection.Contains(c))
                {
                    _curvesWithUi[c].CurvePoints.ForEach(cpc => SelectionHandler.RemoveElement(cpc));
                    _curvesWithUi.Remove(c);
                    someCurvesUnselected = true;
                }
            }

            if (newCurveSelection.Count == 0)
                return;

            foreach (var newCurve in newCurveSelection)
            {
                if (!_curvesWithUi.ContainsKey(newCurve))
                {
                    _curvesWithUi[newCurve] = new CurveUi(newCurve, this);
                    someNewCurvesSelected = true;
                }
            }

            if (someCurvesUnselected || someNewCurvesSelected)
            {
                ViewAllOrSelectedKeys(keepURange: true);
            }
        }

        public void ToggleKeyframes()
        {
            SelectionHandler.Clear();
            foreach (var pair in _curvesWithUi)
            {
                var curve = pair.Key;
                var curveUi = pair.Value;

                if (curve.HasVAt(_clipTime.Time))
                {
                }
                else
                {
                    var value = curve.GetSampledValue(_clipTime.Time);
                    var previousU = curve.GetPreviousU(_clipTime.Time);

                    var key = (previousU != null)
                                  ? curve.GetV(previousU.Value).Clone()
                                  : new VDefinition();

                    key.Value = value;
                    key.U = _clipTime.Time;

                    curve.AddOrUpdateV(_clipTime.Time, key);
                    var newCurvePointUi = new CurvePointUi(key, curve, this);
                    newCurvePointUi.IsSelected = true;
                    curveUi.CurvePoints.Add(newCurvePointUi);
                    SelectionHandler.AddElement(newCurvePointUi);
                }
            }
        }

        public void JumpToNextKeyframe()
        {
        }


        #region --- draw ui ---------------------------------------------------

        public void Draw()
        {
            _io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();

            WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
            WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);

            DrawList = ImGui.GetWindowDrawList();

            // Damp scaling
            const float _dampSpeed = 30f;
            var damping = _io.DeltaTime * _dampSpeed;
            if (!float.IsNaN(damping) && damping > 0.001f && damping <= 1.0f)
            {
                Scale = Im.Lerp(Scale, _scaleTarget, damping);
                Scroll = Im.Lerp(Scroll, _scrollTarget, damping);
            }


            ImGui.BeginChild("scrolling_region2", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                DrawList = ImGui.GetWindowDrawList();

                HandleInteraction();
                _horizontalScaleLines.Draw();
                DrawCurves();
                _selectionFence.Draw();
                DrawList.PopClipRect();
                DrawContextMenu();
                DrawTimeRange();
                DrawCurrentTimeMarker();
                DrawDragTimeArea();
                _curveEditBox.Draw();
            }
            ImGui.EndChild();
        }


        bool _contextMenuIsOpen = false;

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
                if (ImGui.MenuItem("Smooth", null, selectedInterpolations.Contains(VDefinition.EditMode.Smooth)))
                    OnSmooth();

                if (ImGui.MenuItem("Cubic", null, selectedInterpolations.Contains(VDefinition.EditMode.Cubic)))
                    OnCubic();

                if (ImGui.MenuItem("Horizontal", null, selectedInterpolations.Contains(VDefinition.EditMode.Horizontal)))
                    OnHorizontal();

                if (ImGui.MenuItem("Contant", null, selectedInterpolations.Contains(VDefinition.EditMode.Constant)))
                    OnConstant();

                if (ImGui.MenuItem("Linear", null, selectedInterpolations.Contains(VDefinition.EditMode.Linear)))
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

        private void HandleInteraction()
        {
            if (!ImGui.IsWindowHovered())
                return;

            if (ImGui.IsMouseDragging(1))
            {
                _scrollTarget -= InverseTransformDirection(_io.MouseDelta);
            }

            if (_io.MouseWheel != 0)
                HandleZoomViewWithMouseWheel();
        }

        private void HandleZoomViewWithMouseWheel()
        {
            float zoomDelta = ComputeZoomDeltaFromMouseWheel();

            var uAtTopLeft = InverseTransformPosition(WindowPos);
            var uAtMouse = InverseTransformDirection(_mouse - WindowPos);
            var u = uAtMouse - uAtTopLeft;
            var uScaled = uAtMouse / zoomDelta;
            var deltaU = uScaled - uAtMouse;

            if (_io.KeyShift)
            {
                _scrollTarget.Y -= deltaU.Y;
                _scaleTarget.Y *= zoomDelta;
            }
            else
            {
                _scrollTarget.X -= deltaU.X;
                _scaleTarget.X *= zoomDelta;
            }
        }

        private float ComputeZoomDeltaFromMouseWheel()
        {
            const float zoomSpeed = 1.2f;
            var zoomSum = 1f;
            if (_io.MouseWheel < 0.0f)
            {
                for (float zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                {
                    zoomSum /= zoomSpeed;
                }
            }

            if (_io.MouseWheel > 0.0f)
            {
                for (float zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                {
                    zoomSum *= zoomSpeed;
                }
            }

            zoomSum = Im.Clamp(zoomSum, 0.01f, 100f);
            return zoomSum;
        }

        private void DrawCurves()
        {
            foreach (var c in _curvesWithUi.Values)
            {
                c.Draw();
            }
        }

        private void DrawCurrentTimeMarker()
        {
            if (_clipTime == null)
                return;

            var p = new Vector2(TransformPositionX((float)_clipTime.Time), 0);
            DrawList.AddRectFilled(p, p + new Vector2(1, 2000), Color.Red);
        }

        private void DrawDragTimeArea()
        {
            if (_clipTime == null)
                return;

            var max = ImGui.GetContentRegionMax();
            var clamp = max;
            clamp.Y = Im.Min(TimeLineDragHeight, max.Y - 1);

            var min = Vector2.Zero;
            //Im.DrawContentRegion();

            ImGui.SetCursorPos(new Vector2(0, max.Y - clamp.Y));
            ImGui.InvisibleButton("##TimeDrag", clamp);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
            }

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0) || ImGui.IsItemClicked())
            {
                _clipTime.Time = InverseTransformPosition(_io.MousePos).X;
            }

            ImGui.SetCursorPos(Vector2.Zero);
        }

        private static Vector2 TimeRangeShadowSize = new Vector2(5, 9999);
        private static Color TimeRangeShadowColor = new Color(0, 0, 0, 0.5f);
        private static Color TimeRangeOutsideColor = new Color(0.0f, 0.0f, 0.0f, 0.3f);
        private static Color TimeRangeMarkerColor = new Color(1f, 1, 1f, 0.3f);

        private void DrawTimeRange()
        {
            if (_clipTime == null)
                return;

            ImGui.PushStyleColor(ImGuiCol.Button, TimeRangeMarkerColor.Rgba);

            // Range start
            {
                var xRangeStart = TransformPositionX((float)_clipTime.TimeRangeStart);
                var rangeStartPos = new Vector2(xRangeStart, 0);

                // Shade outside
                DrawList.AddRectFilled(
                                       new Vector2(0, 0),
                                       new Vector2(xRangeStart, TimeRangeShadowSize.Y),
                                       TimeRangeOutsideColor);

                // Shadow
                DrawList.AddRectFilled(
                                       rangeStartPos - new Vector2(TimeRangeShadowSize.X - 1, 0),
                                       rangeStartPos + new Vector2(0, TimeRangeShadowSize.Y),
                                       TimeRangeShadowColor);

                // Line
                DrawList.AddRectFilled(rangeStartPos, rangeStartPos + new Vector2(1, 9999), TimeRangeShadowColor);

                SetCursorToBottom(
                                  xRangeStart - TimeRangeHandleSize.X,
                                  TimeRangeHandleSize.Y);

                ImGui.Button("##StartPos", TimeRangeHandleSize);


                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    _clipTime.TimeRangeStart += InverseTransformDirection(_io.MouseDelta).X;
                }
            }

            // Range end
            {
                var rangeEndX = TransformPositionX((float)_clipTime.TimeRangeEnd);
                var rangeEndPos = new Vector2(rangeEndX, 0);

                // Shade outside
                var windowMaxX = ImGui.GetContentRegionAvail().X + WindowPos.X;
                if (rangeEndX < windowMaxX)
                    DrawList.AddRectFilled(
                                           rangeEndPos,
                                           rangeEndPos + new Vector2(windowMaxX - rangeEndX, TimeRangeShadowSize.Y),
                                           TimeRangeOutsideColor);

                // Shadow
                DrawList.AddRectFilled(
                                       rangeEndPos,
                                       rangeEndPos + TimeRangeShadowSize,
                                       TimeRangeShadowColor);

                // Line
                DrawList.AddRectFilled(rangeEndPos, rangeEndPos + new Vector2(1, 9999), TimeRangeShadowColor);

                SetCursorToBottom(
                                  rangeEndX,
                                  TimeRangeHandleSize.Y);

                ImGui.Button("##EndPos", TimeRangeHandleSize);

                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                {
                    _clipTime.TimeRangeEnd += InverseTransformDirection(_io.MouseDelta).X;
                }
            }

            ImGui.PopStyleColor();
        }


        private static Vector2 TimeRangeHandleSize = new Vector2(10, 20);

        private void SetCursorToBottom(float xInScreen, float paddingFromBottom)
        {
            var max = ImGui.GetWindowContentRegionMax() + ImGui.GetWindowPos();

            var p = new Vector2(xInScreen, max.Y - paddingFromBottom);
            //UiHelpers.THelpers.DebugRect(p, p + new Vector2(3, 3));
            ImGui.SetCursorScreenPos(p);
        }

        #endregion

        public void RebuildCurrentCurves()
        {
            // Copy list first, because RebuildCurve modifies the collection
            var curvesToRebuild = new List<Curve>();
            foreach (var curve in _curvesWithUi.Keys)
            {
                curvesToRebuild.Add(curve);
            }

            foreach (var curve in curvesToRebuild)
            {
                RebuildCurve(curve);
            }
        }

//        private void CurveChangedHandler(object o, EventArgs e)
//        {
//            Curve curve = o as Curve;
//            RebuildCurve(curve);
//        }

        #region update children

        public void ViewAllOrSelectedKeys(bool keepURange = false)
        {
            const float CURVE_VALUE_PADDING = 0.3f;
            const float CURVE_VALUE_PADDINGY = 2f;

            double minU = double.PositiveInfinity;
            double maxU = double.NegativeInfinity;
            double minV = double.PositiveInfinity;
            double maxV = double.NegativeInfinity;
            int numPoints = 0;

            if (SelectionHandler.SelectedElements.Count == 0)
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
            }
            else if (SelectionHandler.SelectedElements.Count == 1)
            {
                return;
            }
            else
            {
                foreach (var element in SelectionHandler.SelectedElements)
                {
                    var cpc = element as CurvePointUi;
                    if (cpc != null)
                    {
                        numPoints++;
                        minU = Math.Min(minU, cpc.Key.U);
                        maxU = Math.Max(maxU, cpc.Key.U);
                        minV = Math.Min(minV, cpc.Key.Value);
                        maxV = Math.Max(maxV, cpc.Key.Value);
                    }
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
                maxU += -1;
                minU -= 1;
            }

            if (maxV == minU)
            {
                maxV += -1;
                minV -= 1;
            }

            if (keepURange)
            {
                _scaleTarget = new Vector2(
                                           _scaleTarget.X,
                                           (float)(WindowSize.Y / ((minV - maxV) * (1 + 2 * CURVE_VALUE_PADDINGY))));

                _scrollTarget = new Vector2(
                                            _scrollTarget.X,
                                            (float)(maxV - CURVE_VALUE_PADDINGY * (minV - maxV)));
            }
            else
            {
                _scaleTarget = new Vector2(
                                           (float)(WindowSize.X / ((maxU - minU) * (1 + 2 * CURVE_VALUE_PADDING))),
                                           (float)(WindowSize.Y / ((minV - maxV) * (1 + 2 * CURVE_VALUE_PADDINGY))));

                _scrollTarget = new Vector2(
                                            (float)(minU - CURVE_VALUE_PADDING * (maxU - minU)),
                                            (float)(maxV - CURVE_VALUE_PADDINGY * (minV - maxV)));
            }
        }

        #endregion


        #region context menu handlers

        public float CurrentU;


//        private void OnAddKeyframe()
//        {
//            var curvesToUpdate = InsertCurvePoint(CurrentU);
//        }


//        private List<Curve> InsertCurvePoint(double u)
//        {
//            var curvesToUpdate = new List<Curve>();
//
//            foreach (var curve in _curvesWithUi.Keys)
//            {
//                if (!curve.HasVAt(u))
//                {
//                    var newKey = new VDefinition();
//                    double? prevU = curve.GetPreviousU(u);
//                    if (prevU != null)
//                        newKey = curve.GetV(prevU.Value).Clone();
//
//                    newKey.Value = curve.GetSampledValue(u);
//
//                    curvesToUpdate.Add(curve);
//                }
//            }
//            return curvesToUpdate;
//        }


        #region set interpolation types

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


        #region before after

        private void OnBeforeConstant()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PreCurveMapping = OutsideCurveBehavior.Constant;
            }

            RebuildCurrentCurves();
        }

        private void OnBeforePingPong()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PreCurveMapping = OutsideCurveBehavior.Oscillate;
            }

            RebuildCurrentCurves();
        }

        private void OnBeforeRepeat()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PreCurveMapping = OutsideCurveBehavior.Cycle;
            }

            RebuildCurrentCurves();
        }

        private void OnBeforeRepeatContinously()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PreCurveMapping = Utils.OutsideCurveBehavior.CycleWithOffset;
            }

            RebuildCurrentCurves();
        }

        private void OnAfterConstant()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PostCurveMapping = Utils.OutsideCurveBehavior.Constant;
            }

            RebuildCurrentCurves();
        }

        private void OnAfterPingPong()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PostCurveMapping = Utils.OutsideCurveBehavior.Oscillate;
            }

            RebuildCurrentCurves();
        }

        private void OnAfterRepeat()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PostCurveMapping = Utils.OutsideCurveBehavior.Cycle;
            }

            RebuildCurrentCurves();
        }

        private void OnAfterRepeatContinously()
        {
            foreach (var curve in GetAllOrSelectedCurves())
            {
                curve.PostCurveMapping = OutsideCurveBehavior.CycleWithOffset;
            }

            RebuildCurrentCurves();
        }


        //protected void DuplicateKeyframesToU(double minU)
        //{
        //    //_updatingCurveEnabled = false;

        //    // duplicate values
        //    SortedList<Curve, List<double>> newCurveUPoints = new SortedList<Curve, List<double>>();
        //    foreach (var curveVdefPair in GetSelectedOrAllVDefinitions())
        //    {
        //        var curve = curveVdefPair.Key;
        //        var newUPoints = new List<double>();
        //        newCurveUPoints[curve] = newUPoints;

        //        foreach (var u in curveVdefPair.Value)
        //        {
        //            var newU = u + CurrentU - minU;
        //            curve.AddOrUpdateV(newU, curve.GetV(u).Clone());
        //            newUPoints.Add(newU);
        //        }
        //    }
        //    //_updatingCurveEnabled = true;
        //    RebuildCurrentCurves();

        //    // select new keys
        //    SelectionHandler.SelectedElements.Clear();
        //    foreach (var curveUListPair in newCurveUPoints)
        //    {
        //        var curve = curveUListPair.Key;
        //        var uList = curveUListPair.Value;

        //        foreach (var curvePoint in _curvesWithUi[curve].CurvePoints)
        //        {
        //            if (uList.Contains(curvePoint.Key.U))
        //            {
        //                SelectionHandler.AddElement(curvePoint);
        //            }
        //        }
        //    }
        //}

        #endregion

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
        }

        #endregion


        #region helper functions     

        /// <summary>
        /// Helper function to extract vdefs from all or selected UI controls across all curves in CurveEditor
        /// </summary>
        /// <returns>a list curves with a list of vDefs</returns>
        protected List<CurvePointUi> GetSelectedOrAllPoints()
        {
            var result = new List<CurvePointUi>();

            if (SelectionHandler.SelectedElements.Count > 0)
            {
                foreach (CurvePointUi cp in SelectionHandler.SelectedElements)
                {
                    result.Add(cp);
                }
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


//        private void OnOptimizeKeyframes()
//        {
//            var curves = _curvesWithUi.Select(pair => pair.Key).OfType<Curve>().ToList();
//            var optimizer = new CurveOptimizer(curves);
//            optimizer.OptimizeCurves(30);
//        }

        public ImDrawListPtr DrawList { get; private set; }


        #region implement ICanvas =================================================================

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            return (posOnCanvas - Scroll) * Scale + WindowPos;
        }

        public Vector2 TransformPositionFloored(Vector2 posOnCanvas)
        {
            return Im.Floor((posOnCanvas - Scroll) * Scale + WindowPos);
        }

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public float TransformPositionX(float xOnCanvas)
        {
            return (xOnCanvas - Scroll.X) * Scale.X + WindowPos.X;
        }

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public float TransformPositionY(float yOnCanvas)
        {
            return (yOnCanvas - Scroll.Y) * Scale.Y + WindowPos.Y;
        }


        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public Vector2 InverseTransformPosition(Vector2 posOnScreen)
        {
            return (posOnScreen - WindowPos) / Scale + Scroll;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public float InverseTransformPositionX(float xOnScreen)
        {
            return (xOnScreen - WindowPos.X) / Scale.X + Scroll.X;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public float InverseTransformPositionY(float yOnScreen)
        {
            return (yOnScreen - WindowPos.Y) / Scale.Y + Scroll.Y;
        }


        /// <summary>
        /// Convert direction on canvas to delta in screen space
        /// </summary>
        public Vector2 TransformDirection(Vector2 vectorInCanvas)
        {
            return vectorInCanvas * Scale;
        }


        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 InverseTransformDirection(Vector2 vectorInScreen)
        {
            return vectorInScreen / Scale;
        }


        /// <summary>
        /// Convert rectangle on canvas to screen space
        /// </summary>
        public ImRect TransformRect(ImRect canvasRect)
        {
            var r = new ImRect(TransformPositionFloored(canvasRect.Min), TransformPositionFloored(canvasRect.Max));
            if (r.Min.Y > r.Max.Y)
            {
                var t = r.Min.Y;
                r.Min.Y = r.Max.Y;
                r.Max.Y = t;
            }

            return r;
        }

        public ImRect InverseTransformRect(ImRect screenRect)
        {
            var r = new ImRect(InverseTransformPosition(screenRect.Min), InverseTransformPosition(screenRect.Max));
            if (r.Min.Y > r.Max.Y)
            {
                var t = r.Min.Y;
                r.Min.Y = r.Max.Y;
                r.Max.Y = t;
            }

            return r;
        }


        /// <summary>
        /// Get relative position within canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale - Scroll;
        }


        IEnumerable<ISelectable> ICanvas.SelectableChildren
        {
            get
            {
                List<ISelectable> result = new List<ISelectable>();
                foreach (var curveUi in _curvesWithUi.Values)
                {
                    result.AddRange(curveUi.CurvePoints);
                }

                return result;
            }
        }


        private void UpdateCurveAndMakeUpdateKeyframeCommands(DoSomethingWithVdefDelegate doFunc)
        {
            //var commandList = new List<ICommand>();

            foreach (var point in GetSelectedOrAllPoints())
            {
                doFunc(point.Key);
            }

            //foreach (var pair in GetSelectedOrAllVDefinitions())
            //{
            //    var curve = pair.Key;
            //    foreach (var u in pair.Value)
            //    {
            //        var vDefinition = curve.GetV(u);
            //        commandList.Add(new AddOrUpdateKeyframeCommand(u, vDefinition, curve));
            //        doFunc(vDefinition);
            //    }
            //}
            //if (commandList.Any())
            //    App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("ForSelectedOrAllPointsDo", commandList));
        }

        private void RebuildCurve(Curve curve)
        {
            // Keep original selection
            var selectedKeys = new List<Tuple<Curve, double>>();
            foreach (var e in SelectionHandler.SelectedElements)
            {
                if (!(e is CurvePointUi cpc))
                    continue;

                selectedKeys.Add(new Tuple<Curve, double>(cpc.Curve, cpc.Key.U));
            }

            SelectionHandler.Clear();
        }

        public bool IsRectVisible(Vector2 pos, Vector2 size)
        {
            return pos.X + size.X >= WindowPos.X
                   && pos.Y + size.Y >= WindowPos.Y
                   && pos.X < WindowPos.X + WindowSize.X
                   && pos.Y < WindowPos.Y + WindowSize.Y;
        }

        /// <summary>
        /// Damped scale factors for u and v
        /// </summary>
        public Vector2 Scale { get; set; } = new Vector2(1, -1);

        public Vector2 WindowPos { get; set; }
        public Vector2 WindowSize { get; set; }
        public Vector2 Scroll { get; set; } = new Vector2(0, 0.0f);
        private Vector2 _scrollTarget = new Vector2(-1.0f, 0.0f);
        public List<ISelectable> SelectableChildren { get; set; }
        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();

        #endregion

        private ClipTime _clipTime;

        private Dictionary<Curve, CurveUi> _curvesWithUi = new Dictionary<Curve, CurveUi>();

        //private List<CurvePointUi> _pointControlRecyclePool = new List<CurvePointUi>();
        private HorizontalScaleLines _horizontalScaleLines;

        private SelectionFence _selectionFence;
        private CurveEditBox _curveEditBox;
        private ImGuiIOPtr _io;
        private Vector2 _mouse;
        private Vector2 _scaleTarget = new Vector2(100, -1);

        // Styling
        public static float TimeLineDragHeight = 20;
    }
}