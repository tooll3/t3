using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Animation.Curve;
using T3.Gui.Graph;
using T3.Gui.Selection;
using static T3.Core.Animation.Curve.Utils;

namespace T3.Gui.Animation
{
    /// <summary>
    /// A stub window to collect curve editing functionality during implementation.
    /// ToDo:
    /// [x] Generate a mock curve with some random keyframes
    /// [x] Render Curve
    /// [X] Zoom and pan timeline-range
    /// [x] Mock random-keyframes
    /// [ ] Render value area
    /// [ ] Render time-line ticks
    /// [ ] Selection of keyframes
    /// [ ] Select with Fence
    /// [ ] Edit Keyframes-tangent editing
    /// [ ] Implement Curve-Edit-Box
    /// </summary>
    public class CurveEditor : ICanvas
    {
        public CurveEditor()
        {
            InitiailizeMockCurves();
            _selectionFence = new SelectionFence(this);
        }


        public bool Draw(ref bool opened)
        {
            _io = ImGui.GetIO();
            if (ImGui.Begin("Curve Editor", ref opened))
            {
                ImGui.BeginGroup();
                {
                    _mouse = ImGui.GetMousePos();
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Color(60, 60, 70, 200).Rgba);

                    // Damp scaling
                    const float _dampSpeed = 20f;
                    _scale = Im.Lerp(_scale, _scaleTarget, _io.DeltaTime * _dampSpeed);
                    UScale = _scale;
                    _scroll = Im.Lerp(_scroll, _scrollTarget, _io.DeltaTime * _dampSpeed);
                    UOffset = _scroll.X;

                    THelpers.DebugWindowRect("window");
                    ImGui.BeginChild("scrolling_region", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                    {
                        var mousePosInWindow = ImGui.GetIO().MousePos - WindowPos;
                        var uv = new Vector2((float)xToU(mousePosInWindow.X), (float)yToV(mousePosInWindow.Y));
                        var duv = new Vector2((float)dxToU(1), (float)dyToV(1));

                        ImGui.Text($"dX:{_scrollTarget.X:0.00} sX:{_scaleTarget:0.00}   x:{mousePosInWindow.X:0.00} u:{uv.X:0.00} dU:{duv.X:0.00}  dV:{duv.Y:0.00}");

                        DrawList = ImGui.GetWindowDrawList();

                        THelpers.DebugWindowRect("window.scrollingRegion");
                        WindowPos = ImGui.GetWindowPos();
                        _size = ImGui.GetWindowSize();
                        DrawList.PushClipRect(WindowPos, WindowPos + _size);

                        // Canvas interaction --------------------------------------------
                        if (ImGui.IsWindowHovered())
                        {
                            if (ImGui.IsMouseDragging(1))
                                _scrollTarget.X -= dxToU(_io.MouseDelta.X);

                            if (_io.MouseWheel != 0)
                                HandleZoomViewWithMouseWheel();
                        }

                        _selectionFence.Draw();
                        DrawCurves();
                        DrawList.PopClipRect();
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar(2);
                }
                ImGui.EndGroup();
            }

            ImGui.End();
            return opened;
        }

        private void HandleZoomViewWithMouseWheel()
        {
            const float zoomSpeed = 1.1f;
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

            var luWindow = (xToU(_size.X) - xToU(0));
            var luMouse = xToU(_mouse.X);
            var dx = (1 - zoomSum) * luWindow * _mouse.X / _size.X;

            _scrollTarget.X -= dx;
            _scrollTarget.X -= 10f * _mouse.X / _size.X;    // Weird offset hack to prevent slipping.
            _scaleTarget *= zoomSum;
        }


        private void DrawCurves()
        {
            foreach (var c in _curvesWithUi.Values)
            {
                c.Draw();
            }
        }


        private void InitiailizeMockCurves()
        {
            //_curvesWithCurvePointUi = new SortedList<Curve, List<CurvePointUi>>();
            _curvesWithUi = new Dictionary<Curve, CurveUi>();
            var random = new Random();

            for (int u = 0; u < 1; u++)
            {
                var newCurve = new Curve();
                for (int i = 0; i < 100; i++)
                {
                    newCurve.AddOrUpdateV(i * 20, new VDefinition()
                    {
                        Value = random.NextDouble() * 10,
                        InType = VDefinition.Interpolation.Spline,
                        OutType = VDefinition.Interpolation.Spline,

                        //U = i * 20,
                        InTangentAngle = 30.0,
                        OutTangentAngle = 20.0,
                    });
                }

                var newCurveUi = new CurveUi(newCurve, this);
                //_curvesWithCurvePointUi[newCurve]
                _curvesWithUi[newCurve] = newCurveUi;
            }
        }


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


        private void CurveChangedHandler(object o, EventArgs e)
        {
            Curve curve = o as Curve;
            RebuildCurve(curve);
        }



        #region update children
        const float CURVE_VALUE_PADDING = 0.6f;


        public void FitValueRange()
        {
            ViewAllKeys(KeeyURange: true);
        }


        private void ViewAllKeys(bool KeeyURange = false)
        {
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

            double scaleV = ActualHeight / (maxV - minV);

            if (minV != maxV)
            {
                MinV = (float)(minV - CURVE_VALUE_PADDING * (maxV - minV));
                MaxV = (float)(maxV + CURVE_VALUE_PADDING * (maxV - minV));
            }
            else
            {
                MinV = (float)minV - 1.0f;
                MaxV = (float)maxV + 1.0f;
            }

            if (!KeeyURange)
            {
                if (maxU != minU)
                {
                    UScale = (float)((ActualWidth) / ((maxU - minU) * (1 + 2 * CURVE_VALUE_PADDING)));
                    UOffset = (float)(minU - CURVE_VALUE_PADDING * (maxU - minU));
                }
                else
                {
                    UOffset = (float)(0.5f * (minU + maxU));
                }
            }
        }

        public float MinV = -20;
        public float MaxV = 20;
        public float UScale = 1;
        public float UOffset = 0;


        public float ActualHeight { get { return ImGui.GetWindowHeight(); } }
        public float ActualWidth { get { return ImGui.GetWindowWidth(); } }

        public double yToV(double y) { return (ActualHeight - y) * (MaxV - MinV) / ActualHeight + MinV; }
        public float yToV(float y) { return (ActualHeight - y) * (MaxV - MinV) / ActualHeight + MinV; }
        public double dyToV(double dy) { return -dy / ActualHeight * (MaxV - MinV); }
        public float dyToV(float dy) { return -dy / ActualHeight * (MaxV - MinV); }
        public double vToY(double v) { return ActualHeight - (v - MinV) / (MaxV - MinV) * ActualHeight; }
        public float vToY(float v) { return ActualHeight - (v - MinV) / (MaxV - MinV) * ActualHeight; }
        public double xToU(double x) { return x / UScale + UOffset; }
        public float xToU(float x) { return x / UScale + UOffset; }
        public double dxToU(double dx) { return dx / UScale; }
        public float dxToU(float dx) { return dx / UScale; }
        public double UToX(double t) { return (t - UOffset) * UScale; }
        public float UToX(float t) { return (t - UOffset) * UScale; }
        #endregion


        #region context menu handlers

        public float CurrentU;


        private void OnAddKeyframe()
        {
            //double time = App.Current.Model.GlobalTime;
            var curvesToUpdate = InsertCurvePoint(CurrentU);

            //foreach (var curve in curvesToUpdate)
            //{
            //    RebuildCurve(curve);
            //}
        }


        private List<Curve> InsertCurvePoint(double u)
        {
            var curvesToUpdate = new List<Curve>();

            //_updatingCurveEnabled = false;
            foreach (var curve in _curvesWithUi.Keys)
            {
                if (!curve.HasVAt(u))
                {
                    var newKey = new VDefinition();
                    double? prevU = curve.GetPreviousU(u);
                    if (prevU != null)
                        newKey = curve.GetV(prevU.Value).Clone();

                    newKey.Value = curve.GetSampledValue(u);
                    //var command = new AddOrUpdateKeyframeCommand(u, newKey, curve);
                    //App.Current.UndoRedoStack.AddAndExecute(command);

                    curvesToUpdate.Add(curve);
                }
            }
            //_updatingCurveEnabled = true;
            return curvesToUpdate;
        }


        #region set interpolation types
        //private void OnFocusSelected(object sender, RoutedEventArgs e)
        //{
        //    ViewAllKeys();
        //}

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
            //UpdateCurveLinesAndEditBox();
            //CheckmarkSelectedInterpolationTypes();
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
            //UpdateCurveLinesAndEditBox();
            //CheckmarkSelectedInterpolationTypes();
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
            //UpdateCurveLinesAndEditBox();
            //CheckmarkSelectedInterpolationTypes();
        }

        private void OnConstant()
        {
            ForSelectedOrAllPointsDo((vDef) =>
            {
                vDef.BrokenTangents = true;
                vDef.OutType = VDefinition.Interpolation.Constant;
                vDef.OutEditMode = VDefinition.EditMode.Constant;
            });
            //UpdateCurveLinesAndEditBox();
            //CheckmarkSelectedInterpolationTypes();
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
            //UpdateCurveLinesAndEditBox();
            //CheckmarkSelectedInterpolationTypes();
        }

        private IEnumerable<VDefinition.EditMode> SelectedKeyframeInterpolationTypes
        {
            get
            {
                var checkedInterpolationTypes = new HashSet<VDefinition.EditMode>();
                foreach (var pair in getSelectedOrAllVDefinitions())
                {
                    var curve = pair.Key;
                    foreach (var vDefinition in pair.Value.Select(curve.GetV))
                    {
                        checkedInterpolationTypes.Add(vDefinition.OutEditMode);
                        checkedInterpolationTypes.Add(vDefinition.InEditMode);
                    }
                }
                return checkedInterpolationTypes;
            }
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


        protected void DuplicateKeyframesToU(double minU)
        {
            //_updatingCurveEnabled = false;

            // duplicate values
            SortedList<Curve, List<double>> newCurveUPoints = new SortedList<Curve, List<double>>();
            foreach (var curveVdefPair in getSelectedOrAllVDefinitions())
            {
                var curve = curveVdefPair.Key;
                var newUPoints = new List<double>();
                newCurveUPoints[curve] = newUPoints;

                foreach (var u in curveVdefPair.Value)
                {
                    var newU = u + CurrentU - minU;
                    curve.AddOrUpdateV(newU, curve.GetV(u).Clone());
                    newUPoints.Add(newU);
                }
            }
            //_updatingCurveEnabled = true;
            RebuildCurrentCurves();

            // select new keys
            SelectionHandler.SelectedElements.Clear();
            foreach (var curveUListPair in newCurveUPoints)
            {
                var curve = curveUListPair.Key;
                var uList = curveUListPair.Value;

                foreach (var curvePoint in _curvesWithUi[curve].CurvePoints)
                {
                    if (uList.Contains(curvePoint.Key.U))
                    {
                        SelectionHandler.AddElement(curvePoint);
                    }
                }
            }
        }

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

        /**
        * Helper function to extract vdefs from all or selected UI controls across all curves in CurveEditor
        * 
        * Returns a list curves with a list of vDefs...
        * 
        */
        protected Dictionary<Curve, List<double>> getSelectedOrAllVDefinitions()
        {
            var curveUs = new Dictionary<Curve, List<double>>();

            if (SelectionHandler.SelectedElements.Count > 0)
            {
                foreach (CurvePointUi cp in SelectionHandler.SelectedElements)
                {

                    if (curveUs.ContainsKey(cp.Curve))
                    {
                        curveUs[cp.Curve].Add(cp.Key.U);
                    }
                    else
                    {
                        var list = new List<double>();
                        list.Add(cp.Key.U);
                        curveUs[cp.Curve] = list;
                    }
                }
            }
            else
            {
                foreach (var curve in _curvesWithUi.Keys)
                {
                    var list = new List<double>();

                    foreach (var pair in curve.GetPoints())
                    {
                        var u = pair.Key;
                        list.Add(u);
                    }
                    curveUs[curve] = list;
                }
            }
            return curveUs;
        }

        delegate void DoSomethingDelegate(VDefinition v);

        private void ForSelectedOrAllPointsDo(DoSomethingDelegate doFunc)
        {
            //_updatingCurveEnabled = false;
            //UpdateCurveAndMakeUpdateKeyframeCommands(doFunc);
            //_updatingCurveEnabled = true;
            RebuildCurrentCurves();
        }

        #endregion

        //public ValueSnapHandler _USnapHandler = new ValueSnapHandler();
        //public ValueSnapHandler _ValueSnapHandler = new ValueSnapHandler();

        private void OnOptimizeKeyframes()
        {
            var curves = _curvesWithUi.Select(pair => pair.Key).OfType<Curve>().ToList();
            var optimizer = new CurveOptimizer(curves);
            optimizer.OptimizeCurves(30);
        }




        #region implement ICanvas =================================================================

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll + WindowPos;
        }

        /// <summary>
        /// Get screen position applying canas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 InverseTransformPosition(Vector2 screenPos)
        {
            return (screenPos - _scroll - WindowPos) / _scale;
        }


        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 TransformDirection(Vector2 vectorInCanvas)
        {
            return vectorInCanvas * _scale;
        }


        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 InverseTransformDirection(Vector2 vectorInScreen)
        {
            return vectorInScreen / _scale;
        }


        public ImRect TransformRect(ImRect canvasRect)
        {
            return new ImRect(TransformPosition(canvasRect.Min), TransformPosition(canvasRect.Max));
        }

        public ImRect InverseTransformRect(ImRect screenRect)
        {
            return new ImRect(InverseTransformPosition(screenRect.Min), InverseTransformPosition(screenRect.Max));
        }


        /// <summary>
        /// Get relative position within canvas by applying zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * _scale + _scroll;
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
        #endregion

        public ImDrawListPtr DrawList;
        public Vector2 WindowPos;    // Position of the canvas window-panel within Application window

        public List<ISelectable> SelectableChildren { get; set; }
        public SelectionHandler SelectionHandler { get; set; } = new SelectionHandler();

        private Dictionary<Curve, CurveUi> _curvesWithUi = new Dictionary<Curve, CurveUi>();
        private List<CurvePointUi> _pointControlRecyclePool = new List<CurvePointUi>();

        private SelectionFence _selectionFence;
        private ImGuiIOPtr _io;

        private Vector2 _size;
        private Vector2 _mouse;

        private Vector2 _scroll = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);

        private float _scale = 1;            // The damped scale factor {read only}
        private float _scaleTarget = 1;


        #region t2 legacy dumpster ===================================================


        //#region properties
        //public static readonly DependencyProperty UOffsetProperty = DependencyProperty.Register(
        //  "UOffset",
        //    typeof(double),
        //    typeof(CurveEditor),
        //    new FrameworkPropertyMetadata(-0.5,
        //    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender)
        //);
        //public virtual double UOffset { get { return (double)GetValue(UOffsetProperty); } set { SetValue(UOffsetProperty, value); } }

        //public static readonly DependencyProperty UScaleProperty = DependencyProperty.Register(
        //  "UScale",
        //    typeof(double),
        //    typeof(CurveEditor),
        //    new FrameworkPropertyMetadata(100.0,
        //    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender)
        //);
        //public double UScale { get { return (double)GetValue(UScaleProperty); } set { SetValue(UScaleProperty, value); } }

        //public static readonly DependencyProperty MinVProperty = DependencyProperty.Register("MinV", typeof(double), typeof(CurveEditor), new UIPropertyMetadata(-1.5));
        //public double MinV { get { return (double)GetValue(MinVProperty); } set { SetValue(MinVProperty, value); } }

        //public static readonly DependencyProperty MaxVProperty = DependencyProperty.Register("MaxV", typeof(double), typeof(CurveEditor), new UIPropertyMetadata(1.5));
        //public double MaxV { get { return (double)GetValue(MaxVProperty); } set { SetValue(MaxVProperty, value); } }
        //#endregion

        //-------------------------------------------------------------------------------------




        //void SelectionChangedHandler(object sender, SelectionHandler.SelectionChangedEventArgs e)
        //{
        //    UpdateCurveHighlight();
        //}

        //void ValueSnapHandler_SnappedEventHandler(object sender, ValueSnapHandler.SnapEventArgs e)
        //{
        //    XValueSnapMarker.Visibility = Visibility.Visible;

        //    var _snapMarkerAnimation = new DoubleAnimation() { From = 0.8, To = 0, Duration = TimeSpan.FromSeconds(0.4) };
        //    _snapMarkerAnimation.BeginAnimation(UIElement.OpacityProperty, _snapMarkerAnimation);

        //    XValueSnapMarker.RenderTransform = new TranslateTransform(0, vToY(e.Value));
        //    XValueSnapMarker.Opacity = 1;

        //    XValueSnapMarker.BeginAnimation(UIElement.OpacityProperty, _snapMarkerAnimation);
        //}

        ///**
        // * Iterate over all curves and change thickness if any of it's ControlPoint is selected
        // * This is slow and could easily be optimized by adding an additional curve selection Handler
        // */
        //void UpdateCurveHighlight()
        //{
        //    foreach (var pair in _curvesWithPointControls)
        //    {
        //        var curve = pair.Key;
        //        var pointControls = pair.Value;

        //        bool doHighlight = false;

        //        foreach (var pc in pointControls)
        //        {
        //            if (pc.IsSelected)
        //            {
        //                doHighlight = true;
        //                break;
        //            }
        //        }

        //        var path = _curvesWithPaths[curve];
        //        path.StrokeThickness = doHighlight ? 2 : 0.8;
        //    }
        //}


        // Is called when curve visibility is changed. E.g. when another operator is selected.
        public void SetCurveOperators(List<Curve> curves)
        {
            // Disable event for this operation...
            //foreach (var curve in _curvesWithPaths.Keys)
            //    curve.ChangedEvent -= CurveChangedHandler;

            SelectionHandler.SelectedElements.Clear();

            _curvesWithUi.Clear();
            //_curvesWithCurvePointUi.Clear();
            //XCurveLineCanvas.Children.Clear();

            // Show optimization dialog for curves with too many keys...
            //if (curves.Any())
            //{
            //    var overallKeyCount = 0;
            //    foreach (var c in curves)
            //    {
            //        overallKeyCount += c.GetPoints().Count;
            //    }

            //    if (overallKeyCount > 2000)
            //    {
            //        var message = String.Format(
            //            "These curves have {0} keyframes which will be very slow to render. Do you want to optimize them?",
            //            overallKeyCount);

            //        if (MessageBox.Show(message, "Optimize", MessageBoxButton.OKCancel, MessageBoxImage.Question) ==
            //            MessageBoxResult.OK)
            //        {
            //            var curves2 = curves.Select(ic => ic as Curve).Where(c => curves != null).ToList();
            //            var optimizer = new CurveOptimizer(curves2);
            //            optimizer.OptimizeCurves(200);
            //        }
            //    }
            //}

            //foreach (var c in XCurvePointCanvas.Children)
            //{
            //    var cep = c as CurvePointControl;
            //    if (cep != null)
            //    {
            //        _pointControlRecyclePool.Add(cep);
            //    }
            //}
            //XCurvePointCanvas.Children.Clear();

            // Reenable event 
            foreach (var curve in curves)
            {
                //curve.ChangedEvent += CurveChangedHandler;
                RebuildCurve(curve);
            }
            //UpdateCurveLinesAndEditBox();
        }










        private void RebuildCurve(Curve curve)
        {
            //if (!_updatingCurveEnabled)
            //{
            //    return;
            //}

            // Keep original selection
            var selectedKeys = new List<Tuple<Curve, double>>();
            foreach (var e in SelectionHandler.SelectedElements)
            {
                if (!(e is CurvePointUi cpc))
                    continue;

                selectedKeys.Add(new Tuple<Curve, double>(cpc.Curve, cpc.Key.U));
            }

            SelectionHandler.Clear();

            //if (!_curvesWithPointControls.ContainsKey(curve))
            //    _curvesWithPointControls[curve] = new List<CurvePointControl>();

            //var curvePointControls = _curvesWithPointControls[curve];

            //int cepIndex = 0;
            //bool reusingControls = true;

            //foreach (var pair in curve.GetPoints())
            //{
            //    double u = pair.Key;
            //    var vDefinition = pair.Value;


            //    // Reuse existing control
            //    if (reusingControls && cepIndex < curvePointControls.Count)
            //    {
            //        var reusedPointControl = curvePointControls[cepIndex];
            //        reusedPointControl.U = u;
            //        reusedPointControl.InitFromVDefinition(vDefinition);
            //        reusedPointControl.Curve = curve;
            //        reusedPointControl.IsSelected = false;

            //        // Was it selected?
            //        foreach (var curveTime in selectedKeys)
            //        {
            //            if (reusedPointControl.Curve == curveTime.Item1 && reusedPointControl.U == curveTime.Item2)
            //            {
            //                reusedPointControl.IsSelected = true;
            //                _SelectionHandler.AddElement(reusedPointControl);
            //                break;
            //            }
            //        }

            //        cepIndex++;
            //    }
            //    else
            //    {
            //        reusingControls = false;
            //        CurvePointControl newPointControl;
            //        // Reuse from pool...
            //        if (_pointControlRecyclePool.Count > 0)
            //        {

            //            newPointControl = _pointControlRecyclePool[0];
            //            _pointControlRecyclePool.RemoveAt(0);

            //            newPointControl.U = u;
            //            newPointControl.InitFromVDefinition(vDefinition);
            //            newPointControl.Curve = curve;
            //            newPointControl.IsSelected = false;
            //        }
            //        // Create new control
            //        else
            //        {
            //            newPointControl = new CurvePointControl(u, vDefinition, curve, this);
            //            newPointControl.U = u;
            //        }

            //        _curvesWithPointControls[curve].Add(newPointControl);
            //        XCurvePointCanvas.Children.Add(newPointControl);
            //    }
            //}


            //// Move obsolete control points to pool
            //if (reusingControls)
            //{
            //    List<CurvePointControl> obsoletePoints = new List<CurvePointControl>();
            //    while (cepIndex < _curvesWithPointControls[curve].Count)
            //    {
            //        var obsoletePointControl = curvePointControls[cepIndex];
            //        _pointControlRecyclePool.Add(obsoletePointControl);
            //        XCurvePointCanvas.Children.Remove(obsoletePointControl);
            //        obsoletePoints.Add(obsoletePointControl);
            //        cepIndex++;
            //    }
            //    foreach (var removeThis in obsoletePoints)
            //    {
            //        curvePointControls.Remove(removeThis);
            //    }
            //}


            // Update curve line (Path)
            //if (_curvesWithPaths.ContainsKey(curve))
            //{
            //    XCurveLineCanvas.Children.Remove(_curvesWithPaths[curve]);
            //}
            //var newPath = new Path();
            //switch (curve.ComponentIndex)
            //{
            //    case 1:
            //        newPath.Stroke = Brushes.Firebrick;
            //        break;
            //    case 2:
            //        newPath.Stroke = Brushes.OliveDrab;
            //        break;
            //    case 3:
            //        newPath.Stroke = Brushes.DodgerBlue;
            //        break;
            //    default:
            //        newPath.Stroke = Brushes.DarkGray;
            //        break;
            //}

            //newPath.StrokeThickness = 1;
            //_curvesWithPaths[curve] = newPath;
            //XCurveLineCanvas.Children.Add(newPath);

            //UpdateCurveHighlight();
            //UpdateLine(curve);
        }

        //private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.RightButton == MouseButtonState.Pressed)
        //    {
        //        UIElement el = sender as UIElement;
        //        if (el != null)
        //        {
        //            el.CaptureMouse();
        //            m_DragStartPosition = e.GetPosition(this);
        //            m_DragStartTimeOffset = UOffset;
        //            m_DragStartMinV = MinV;
        //            m_DragStartMaxV = MaxV;
        //            m_IsRightMouseDragging = true;
        //        }
        //        this.Focus();
        //    }
        //}

        //private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    m_IsRightMouseDragging = false;
        //    UIElement thumb = sender as UIElement;
        //    if (thumb != null)
        //    {
        //        thumb.ReleaseMouseCapture();
        //        double dragDelta = Math.Abs(m_DragStartPosition.X - e.GetPosition(this).X) + Math.Abs(m_DragStartPosition.Y - e.GetPosition(this).Y);
        //        if (dragDelta > 3)
        //        {
        //            XGrid.ContextMenu = null;
        //        }
        //        else
        //        {
        //            XGrid.ContextMenu = Resources["XCurveEditContextMenu"] as System.Windows.Controls.ContextMenu;
        //            CheckmarkSelectedInterpolationTypes();
        //        }
        //    }
        //}



        //private void OnMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (m_IsRightMouseDragging)
        //    {
        //        UOffset = m_DragStartTimeOffset + (m_DragStartPosition.X - e.GetPosition(this).X) / UScale;

        //        double deltaY = (m_DragStartPosition.Y - e.GetPosition(this).Y);
        //        double deltaV = yToV(deltaY) - yToV(0);
        //        MinV = m_DragStartMinV + deltaV;
        //        MaxV = m_DragStartMaxV + deltaV;
        //        UpdateCurveLinesAndEditBox();
        //    }
        //}

        //private void OnDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        //{
        //    _SelectionFence.HandleDragStarted(sender, e);
        //    this.Focus();
        //}

        //private void OnDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        //{
        //    _SelectionFence.HandleDragDelta(sender, e);
        //}

        //private void OnDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        //{
        //    _SelectionFence.HandleDragCompleted(sender, e);

        //}


        //private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    double mouseWheelZoomSpeed = 1.15;
        //    double scale = (e.Delta > 0) ? mouseWheelZoomSpeed : 1.0 / mouseWheelZoomSpeed;


        //    if ((Keyboard.Modifiers & (ModifierKeys.Control)) == ModifierKeys.Control)
        //    {
        //        double dv = (MaxV - MinV) * (1.0 - scale);
        //        double factor = e.GetPosition(this).Y / ActualHeight;
        //        MaxV += dv * factor;
        //        MinV -= dv * (1.0 - factor);
        //    }
        //    if (Keyboard.Modifiers == ModifierKeys.None
        //        || (Keyboard.Modifiers & (ModifierKeys.Shift)) == ModifierKeys.Shift)
        //    {
        //        UScale *= scale;
        //        UOffset += (scale - 1.0) * (xToU(ActualWidth) - xToU(0)) * (e.GetPosition(this).X / ActualWidth);
        //    }
        //    UpdateCurveLinesAndEditBox();
        //    e.Handled = true;
        //}

        //private void OnKeyDown(object sender, KeyEventArgs e)
        //{

        //    if (e.Key == Key.F)
        //    {
        //        ViewAllKeys();
        //        e.Handled = true;
        //    }
        //    else if (e.Key == Key.Delete || e.Key == Key.Back)
        //    {
        //        DeleteSelectedKeys();
        //        e.Handled = true;
        //    }
        //}

        //private void DoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    double u = xToU(e.GetPosition(this).X);
        //    u = Math.Round(u, Curve.CURVE_U_PRECISION_DIGITS);
        //    var curvesToUpdate = InsertCurvePoint(u);

        //    foreach (var curve in curvesToUpdate)
        //    {
        //        RebuildCurve(curve);
        //    }

        //    var pointsToSelect = new List<ISelectable>();

        //    foreach (var pair in _curvesWithPointControls)
        //    {
        //        var controls = pair.Value;

        //        foreach (var cpc in controls)
        //        {
        //            if (cpc.U == u)
        //            {
        //                cpc.IsSelected = true;
        //                pointsToSelect.Add(cpc);
        //            }
        //        }
        //    }

        //    _SelectionHandler.SetElements(pointsToSelect);
        //    e.Handled = true;
        //}



        //private void CheckmarkSelectedInterpolationTypes()
        //{
        //    UncheckAllContextMenuItems();
        //    var menuItems = XGrid.ContextMenu.Items.OfType<MenuItem>();
        //    MenuItem menuItem;
        //    foreach (var selectedKeyframeInterpolationType in SelectedKeyframeInterpolationTypes)
        //    {
        //        switch (selectedKeyframeInterpolationType)
        //        {
        //            case VDefinition.EditMode.Horizontal:
        //                menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Horizontal");
        //                menuItem.IsChecked = true;
        //                break;
        //            case VDefinition.EditMode.Linear:
        //                menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Linear");
        //                menuItem.IsChecked = true;
        //                break;
        //            case VDefinition.EditMode.Smooth:
        //                menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Smooth");
        //                menuItem.IsChecked = true;
        //                break;
        //            case VDefinition.EditMode.Cubic:
        //                menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Cubic");
        //                menuItem.IsChecked = true;
        //                break;
        //            case VDefinition.EditMode.Constant:
        //                menuItem = menuItems.SingleOrDefault(item => item.Header.ToString() == "Constant");
        //                menuItem.IsChecked = true;
        //                break;
        //        }
        //    }
        //}



        /// <summary>
        /// A helper function that pastes a number of keyframes to the first visible curve. 
        /// This is currently used by get KeyFramesFromLogfile but might also be a first stup of copy/pasting keyframes.
        /// </summary>
        /// <param name="valuesOverTime"></param>
        //public void AddKeyframesToFirstCurve(List<KeyValuePair<double, float>> valuesOverTime)
        //{
        //    if (_curvesWithCurvePointUi.Keys.Count == 0)
        //    {
        //        //UIHelper.ShowErrorMessageBox("To add keyframes to a curve, you have to selected an animated operator.", "Cannot paste keyframes.");
        //        return;
        //    }

        //    //_updatingCurveEnabled = false;
        //    var curve = _curvesWithCurvePointUi.Keys[0];

        //    foreach (var valueAndTime in valuesOverTime)
        //    {
        //        double time = valueAndTime.Key;
        //        float value = valueAndTime.Value;

        //        curve.AddOrUpdateV(time, new VDefinition() { Value = value });
        //    }
        //    //_updatingCurveEnabled = true;
        //    RebuildCurrentCurves();
        //}




        //private void UpdateCurveAndMakeUpdateKeyframeCommands(DoSomethingDelegate doFunc)
        //{
        //    var commandList = new List<ICommand>();
        //    foreach (var pair in getSelectedOrAllVDefinitions())
        //    {
        //        var curve = pair.Key;
        //        foreach (var u in pair.Value)
        //        {
        //            var vDefinition = curve.GetV(u);
        //            commandList.Add(new AddOrUpdateKeyframeCommand(u, vDefinition, curve));
        //            doFunc(vDefinition);
        //        }
        //    }
        //    if (commandList.Any())
        //        App.Current.UndoRedoStack.AddAndExecute(new MacroCommand("ForSelectedOrAllPointsDo", commandList));
        //}

        //public void DeleteSelectedKeys()
        //{
        //    //_updatingCurveEnabled = false;
        //    var pointsToDelete = new List<ISelectable>(_SelectionHandler.SelectedElements);
        //    MakeAndExecuteCommandForDeletion(pointsToDelete);
        //    //_updatingCurveEnabled = true;
        //    RebuildCurrentCurves();
        //}

        //private void MakeAndExecuteCommandForDeletion(IEnumerable<ISelectable> pointsToDelete)
        //{
        //    var keyFramesToDelete = new Tuple<double, Curve>[pointsToDelete.Count()];
        //    for (var i = 0; i < pointsToDelete.Count(); i++)
        //    {
        //        var cpc = pointsToDelete.ElementAt(i) as CurvePointControl;
        //        if (cpc != null)
        //        {
        //            keyFramesToDelete[i] = new Tuple<double, Curve>(cpc.U, cpc.Curve);
        //        }
        //    }
        //    _SelectionHandler.Clear();
        //    App.Current.UndoRedoStack.AddAndExecute(new RemoveKeyframeCommand(keyFramesToDelete, App.Current.Model.GlobalTime));
        //}








        #endregion

    }
}
