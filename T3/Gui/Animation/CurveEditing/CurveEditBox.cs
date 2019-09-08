using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Logging;
using T3.Gui.Selection;
using UiHelpers;

using static ImGuiNET.ImGui;


namespace T3.Gui.Animation.CurveEditing
{
    public class CurveEditBox
    {

        public CurveEditBox(CurveEditCanvas curveCanvas)
        {
            _curveCanvas = curveCanvas;
        }
        private CurveEditCanvas _curveCanvas;

        public void Draw()
        {
            if (_curveCanvas.SelectionHandler.SelectedElements.Count <= 1)
                return;

            _bounds = GetBoundingBox();
            MinU = _bounds.Min.X;
            MaxU = _bounds.Max.X;
            MinV = _bounds.Min.Y;
            MaxV = _bounds.Max.Y;

            var boundsOnScreen = _curveCanvas.TransformRect(_bounds);
            //var min = _curveCanvas.TransformPosition(bounds.Min);
            //var max = _curveCanvas.TransformPosition(bounds.Max);
            _curveCanvas.DrawList.AddRect(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderColor);
            _curveCanvas.DrawList.AddRectFilled(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderFill);

            // Top
            {
                SetCursorScreenPos(boundsOnScreen.Min - VerticalHandleOffset);
                Button("##top", new Vector2(boundsOnScreen.GetWidth(), DragHandleSize));
                if (IsItemActive() || IsItemHovered())
                {
                    SetMouseCursor(ImGuiNET.ImGuiMouseCursor.ResizeNS);
                }

                if (IsItemActive() && IsMouseDragging(0))
                {
                    var deltaOnCanvas = _curveCanvas.InverseTransformDirection(GetIO().MouseDelta);
                    ScaleAtTop(deltaOnCanvas.Y);
                }
            }

            // Bottom
            {
                SetCursorScreenPos(new Vector2(boundsOnScreen.Min.X, boundsOnScreen.Max.Y));
                Button("##bottom", new Vector2(boundsOnScreen.GetWidth(), DragHandleSize));
                if (IsItemActive() || IsItemHovered())
                {
                    SetMouseCursor(ImGuiNET.ImGuiMouseCursor.ResizeNS);
                }
                if (IsItemActive() && IsMouseDragging(0))
                {
                    var deltaOnCanvas = _curveCanvas.InverseTransformDirection(GetIO().MouseDelta);
                    ScaleAtBottom(deltaOnCanvas.Y);
                }
            }
        }

        private static float DragHandleSize = 10;
        private static Vector2 VerticalHandleOffset = new Vector2(0, DragHandleSize);
        private static Vector2 HorizontalHandleOffset = new Vector2(DragHandleSize, 0);
        private static Vector2 HandleOffset = new Vector2(DragHandleSize, DragHandleSize);

        //private bool DrawDragHandle(ImRect rect, ImGuiNET.ImGuiMouseCursor curor, Action action)
        //{
        //    SetCursorScreenPos(rect.Min);
        //    return Button("##dragLeft", rect.GetSize());
        //}



        private static Color SelectBoxBorderColor = new Color(1, 1, 1, 0.2f);
        private static Color SelectBoxBorderFill = new Color(1, 1, 1, 0.05f);

        //#region dependency properties
        //public static readonly DependencyProperty MinVProperty = DependencyProperty.Register("MinV", typeof(double), typeof(CurveEditBox), new UIPropertyMetadata(-100.0));
        //public double MinV { get { return (double)GetValue(MinVProperty); } set { SetValue(MinVProperty, value); } }

        //public static readonly DependencyProperty MaxVProperty = DependencyProperty.Register("MaxV", typeof(double), typeof(CurveEditBox), new UIPropertyMetadata(100.0));
        //public double MaxV { get { return (double)GetValue(MaxVProperty); } set { SetValue(MaxVProperty, value); } }

        //public static readonly DependencyProperty MinUProperty = DependencyProperty.Register("MinU", typeof(double), typeof(CurveEditBox), new UIPropertyMetadata(-100.0));
        //public double MinU { get { return (double)GetValue(MinUProperty); } set { SetValue(MinUProperty, value); } }

        //public static readonly DependencyProperty MaxUProperty = DependencyProperty.Register("MaxU", typeof(double), typeof(CurveEditBox), new UIPropertyMetadata(100.0));
        //public double MaxU { get { return (double)GetValue(MaxUProperty); } set { SetValue(MaxUProperty, value); } }
        //#endregion


        //public SelectionHandler m_SelectionHandler { get; set; }
        //private CurveEditor CurveEditor { get; set; }

        //#region constructors
        //public CurveEditBox()
        //{
        //    InitializeComponent();
        //}


        //public CurveEditBox(SelectionHandler se, CurveEditor ce)
        //{
        //    InitializeComponent();
        //    m_SelectionHandler = se;
        //    CurveEditor = ce;
        //    createBindingsForPositioning();
        //    UpdateShapeAndLines();
        //    m_SelectionHandler.SelectionChanged += m_SelectionHandler_SelectionChanged;

        //}

        //void m_SelectionHandler_SelectionChanged(object sender, SelectionHandler.SelectionChangedEventArgs e)
        //{
        //    UpdateShapeAndLines();
        //}
        //#endregion

        //protected override Geometry GetLayoutClip(Size layoutSlotSize)
        //{
        //    return ClipToBounds ? base.GetLayoutClip(layoutSlotSize) : null;
        //}


        ///**
        // * This method needs to be called everytime the curve editor gets scrolled, or otherwise modified
        // */
        //public void UpdateShapeAndLines()
        //{
        //    if (m_SelectionHandler.SelectedElements.Count <= 0)
        //    {
        //        this.Visibility = System.Windows.Visibility.Collapsed;
        //    }
        //    else
        //    {
        //        UpdateEditBoxShape();

        //        // Refresh vdef after AddOrUpdateValue cloned the definition
        //        foreach (var e in CurveEditor._SelectionHandler.SelectedElements)
        //        {
        //            var cpc = e as CurvePointControl;
        //            if (cpc == null) continue;

        //            cpc.m_vdef = cpc.Curve.GetV(cpc.U);
        //        }
        //        CurveEditor.UpdateLines();
        //    }
        //}

        private static float MinU;
        private static float MaxU;
        private static float MinV;
        private static float MaxV;

        private ImRect _bounds;

        public void UpdateEditBoxShape()
        {
            //this.Visibility = System.Windows.Visibility.Visible;

            //this.Height = Math.Abs(CurveEditor.vToY(MaxV) - CurveEditor.vToY(MinV));
            //this.Width = Math.Abs(CurveEditor.UToX(MaxU) - CurveEditor.UToX(MinU));
            //App.Current.UpdateRequiredAfterUserInteraction = true;
            //XMinULabel.Text = String.Format("{0:F3}", MinU);
            //XMaxULabel.Text = String.Format("{0:F3}", MaxU);
            //XMinVLabel.Text = String.Format("{0:F3}", MaxV);
            //XMaxVLabel.Text = String.Format("{0:F3}", MinV);

            //XDragHandle.Visibility = m_SelectionHandler.SelectedElements.Count == 1
            //    ? System.Windows.Visibility.Collapsed
            //    : System.Windows.Visibility.Visible;

            //XMinULabel.Visibility = (MinU == MaxU)
            //    ? System.Windows.Visibility.Collapsed
            //    : System.Windows.Visibility.Visible;

            //XMaxVLabel.Visibility = (MinV == MaxV)
            //    ? System.Windows.Visibility.Collapsed
            //    : System.Windows.Visibility.Visible;
        }

        private static void ScaleAtBottom(double deltaInPixel)
        {
            //var position = Mouse.GetPosition(CurveEditor);
            //var bottomV = CurveEditor.yToV(position.Y);

            //CurveEditor.DisableRebuildOnCurveChangeEvents();
            //if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            //{
            //    var snapBottomValue = CurveEditor._ValueSnapHandler.CheckForSnapping(bottomV);
            //    if (!Double.IsNaN(snapBottomValue))
            //    {
            //        bottomV = snapBottomValue;
            //    }
            //}

            //var scale = (bottomV - MinV) / (MaxV - MinV);

            //if (!Double.IsNaN(scale) && Math.Abs(scale) < 10000)
            //{
            //    var idx = 0;
            //    foreach (var ep in CurvePointsControls)
            //    {
            //        ep.ManipulateV(MinV + (ep.V - MinV) * scale);
            //        _addOrUpdateKeyframeCommands[idx].KeyframeValue = ep.m_vdef;
            //        ++idx;
            //    }
            //    _moveKeyframesCommand.Do();
            //}

            //CurveEditor.EnableRebuildOnCurveChangeEvents();
            //UpdateShapeAndLines();
        }


        public void ScaleAtTop(float deltaV)
        {
            var scale = (_bounds.Max.Y + deltaV - MinV) / (MaxV - MinV);

            if (!Double.IsNaN(scale) && Math.Abs(scale) < 10000)
            {
                var idx = 0;
                foreach (var ep in CurvePointsControls)
                {
                    ep.ManipulateV(MinV + (ep.PosOnCanvas.Y - MinV) * scale);
                    ++idx;
                }
            }
        }


        public void ScaleAtBottom(float deltaV)
        {
            //var scale = (MinV - (_bounds.Min.Y + deltaV)) / (MaxV - MinV);
            var scale = (_bounds.Max.Y - deltaV - MinV) / (MaxV - MinV);

            if (!Double.IsNaN(scale) && Math.Abs(scale) < 10000)
            {
                var idx = 0;
                foreach (var ep in CurvePointsControls)
                {
                    ep.ManipulateV(MaxV - (MaxV - ep.PosOnCanvas.Y) * scale);
                    ++idx;
                }
            }
        }


        /*
            public void ScaleAtRightPosition()
            {
                var position = Mouse.GetPosition(CurveEditor);
                var endU = CurveEditor.xToU(position.X);

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    var snapEnd = CurveEditor._USnapHandler.CheckForSnapping(endU);
                    if (!Double.IsNaN(snapEnd))
                    {
                        endU = snapEnd;
                    }
                }

                var scale = (endU - MinU) / (MaxU - MinU);

                if (!Double.IsNaN(scale) && Math.Abs(scale) < 10000)
                {
                    if (scale != 1.0)
                    {
                        CurveEditor.DisableRebuildOnCurveChangeEvents();
                        var idx = 0;
                        foreach (var cpc in CurvePointsControls)
                        {
                            cpc.ManipulateU(MinU + (cpc.U - MinU) * scale);
                            _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
                            ++idx;
                        }
                        _moveKeyframesCommand.Do();

                    }
                }
                CurveEditor.EnableRebuildOnCurveChangeEvents();
                UpdateShapeAndLines();
            }


            public void ScaleAtLeftPosition()
            {
                var position = Mouse.GetPosition(CurveEditor);
                var startU = CurveEditor.xToU(position.X);

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    var snapStart = CurveEditor._USnapHandler.CheckForSnapping(startU);
                    if (!Double.IsNaN(snapStart))
                    {
                        startU = snapStart;
                    }
                }

                var scale = (MaxU - startU) / (MaxU - MinU);
                if (!Double.IsNaN(scale) && Math.Abs(scale) < 10000)
                {
                    if (scale != 1.0)
                    {
                        CurveEditor.DisableRebuildOnCurveChangeEvents();

                        var idx = 0;
                        foreach (var cpc in CurvePointsControls)
                        {
                            cpc.ManipulateU(MaxU - (MaxU - cpc.U) * scale);
                            _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
                            ++idx;
                        }
                        _moveKeyframesCommand.Do();
                    }
                }
                CurveEditor.EnableRebuildOnCurveChangeEvents();
                UpdateShapeAndLines();
            }
        */

        #region XAML event handlers

        //private void DragStarted(object sender, DragStartedEventArgs e)
        //{
        //    StartMoveKeyframeCommand();
        //}


        private static void XMoveBothThumb_DragDelta()
        {
            //var idx = 0;
            //foreach (var cpc in CurvePointsControls)
            //{
            //    cpc.ManipulateV(cpc.V + CurveEditor.yToV(e.VerticalChange) - CurveEditor.yToV(0));
            //    cpc.ManipulateU(cpc.U + CurveEditor.xToU(e.HorizontalChange) - CurveEditor.xToU(0));
            //    _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
            //    _addOrUpdateKeyframeCommands[idx].KeyframeValue = cpc.m_vdef;
            //    ++idx;
            //}
            //_moveKeyframesCommand.Do();
        }

        /*
        private void XMoveVerticalThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newMinV = MinV + CurveEditor.dyToV(e.VerticalChange);
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                var snapMin = CurveEditor._ValueSnapHandler.CheckForSnapping(newMinV);
                if (!Double.IsNaN(snapMin))
                {
                    newMinV = snapMin;
                }
            }
            var deltaV = newMinV - MinV;

            CurveEditor.DisableRebuildOnCurveChangeEvents();
            var idx = 0;
            foreach (var ep in CurvePointsControls)
            {
                ep.ManipulateV(ep.V + deltaV);
                _addOrUpdateKeyframeCommands[idx].KeyframeValue = ep.m_vdef;
                ++idx;
            }
            _moveKeyframesCommand.Do();
            CurveEditor.EnableRebuildOnCurveChangeEvents();
            UpdateShapeAndLines();
        }

        private void XMoveHorizonalThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var newStartU = MinU + CurveEditor.dxToU(e.HorizontalChange);
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                var snapStart = CurveEditor._USnapHandler.CheckForSnapping(newStartU);
                if (!Double.IsNaN(snapStart))
                {
                    newStartU = snapStart;
                }
            }
            var deltaU = newStartU - MinU;

            CurveEditor.DisableRebuildOnCurveChangeEvents();
            var idx = 0;
            foreach (var cpc in CurvePointsControls)
            {
                cpc.ManipulateU(cpc.U + deltaU);
                _addOrUpdateKeyframeCommands[idx].KeyframeTime = cpc.U;
                ++idx;
            }
            _moveKeyframesCommand.Do();
            CurveEditor.EnableRebuildOnCurveChangeEvents();
            UpdateShapeAndLines();
        }
        */

        //private void DragCompleted(object sender, DragCompletedEventArgs e)
        //{
        //    CompleteMoveKeyframeCommand();
        //}

        //public void CompleteMoveKeyframeCommand()
        //{
        //    App.Current.UndoRedoStack.Add(_moveKeyframesCommand);
        //    CurveEditor.RebuildCurrentCurves();
        //}

        //public void StartMoveKeyframeCommand()
        //{
        //    _addOrUpdateKeyframeCommands.Clear();
        //    MakeUpdateCommandsForSelectedCurvePoints();
        //    _moveKeyframesCommand = new MacroCommand("Move Keyframes", _addOrUpdateKeyframeCommands);
        //}

        //private void MakeUpdateCommandsForSelectedCurvePoints()
        //{
        //    var timeCurveTuples = from curvePoint in CurvePointsControls
        //                          select new Tuple<double, ICurve>(curvePoint.U, curvePoint.Curve);
        //    foreach (var tuple in timeCurveTuples)
        //    {
        //        var keyframeTime = tuple.Item1;
        //        var curve = tuple.Item2;
        //        _addOrUpdateKeyframeCommands.Add(new AddOrUpdateKeyframeCommand(keyframeTime, curve.GetV(keyframeTime), curve));
        //    }
        //}

        //private List<AddOrUpdateKeyframeCommand> _addOrUpdateKeyframeCommands = new List<AddOrUpdateKeyframeCommand>();
        //private MacroCommand _moveKeyframesCommand;

        private IEnumerable<CurvePointUi> CurvePointsControls
        {
            get
            {
                return from selectedElement in _curveCanvas.SelectionHandler.SelectedElements
                       let curvePoint = selectedElement as CurvePointUi
                       where curvePoint != null
                       select curvePoint;
            }
        }
        #endregion

        #region private helper methods
        //private void createBindingsForPositioning()
        //{
        //    MultiBinding multiBinding2 = new MultiBinding();
        //    multiBinding2.Converter = new UToXConverter();
        //    multiBinding2.Bindings.Add(new Binding("MinU") { Source = this });
        //    multiBinding2.Bindings.Add(new Binding("UScale") { Source = CurveEditor });
        //    multiBinding2.Bindings.Add(new Binding("UOffset") { Source = CurveEditor });
        //    BindingOperations.SetBinding(XTranslateTransform, TranslateTransform.XProperty, multiBinding2);

        //    MultiBinding multiBinding = new MultiBinding();
        //    multiBinding.Converter = new VToYConverter();
        //    multiBinding.Bindings.Add(new Binding("MinV") { Source = this });
        //    multiBinding.Bindings.Add(new Binding("MinV") { Source = CurveEditor });
        //    multiBinding.Bindings.Add(new Binding("MaxV") { Source = CurveEditor });
        //    multiBinding.Bindings.Add(new Binding("ActualHeight") { Source = CurveEditor });
        //    BindingOperations.SetBinding(XTranslateTransform, TranslateTransform.YProperty, multiBinding);
        //}

        //static SelectionHandler _selectionHandler;
        private ImRect GetBoundingBox()
        {
            float minU = float.PositiveInfinity;
            float minV = float.PositiveInfinity;
            float maxU = float.NegativeInfinity;
            float maxV = float.NegativeInfinity;

            if (_curveCanvas.SelectionHandler.SelectedElements.Count > 1)
            {
                foreach (var selected in _curveCanvas.SelectionHandler.SelectedElements)
                {
                    var pc = selected as CurvePointUi;
                    if (pc != null)
                    {
                        minU = Math.Min(minU, pc.PosOnCanvas.X);
                        maxU = Math.Max(maxU, pc.PosOnCanvas.X);
                        minV = Math.Min(minV, pc.PosOnCanvas.Y);
                        maxV = Math.Max(maxV, pc.PosOnCanvas.Y);
                    }
                }
                return new ImRect(minU, minV, maxU, maxV);
            }
            else if (_curveCanvas.SelectionHandler.SelectedElements.Count == 1)
            {
                var pc = _curveCanvas.SelectionHandler.SelectedElements[0] as CurvePointUi;
                minU = maxU = pc.PosOnCanvas.X;
                minV = maxV = pc.PosOnCanvas.Y;
                return new ImRect(minU, minV, maxU, maxV);
            }
            return new ImRect(100, 100, 200, 200);
        }

        #endregion
    }
}


