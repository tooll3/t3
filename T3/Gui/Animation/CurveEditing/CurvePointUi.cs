using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Animation.Curves;
using T3.Core.Logging;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Animation.CurveEditing
{
    /// <summary>
    /// Interaction logic for CurvePointControl.xaml
    /// </summary>
    public class CurvePointUi : ISelectable
    {
        public Vector2 Size { get; set; } = new Vector2(0, 0);
        private static Vector2 ControlSize = new Vector2(10, 10);
        private static Vector2 _controlSizeHalf = ControlSize * 0.5f;

        public bool IsSelected { get; set; }
        public VDefinition Key;
        public Curve Curve { get; set; }
        public Guid Id { get; private set; } = Guid.NewGuid();

        public static int createCount = 0;
        private const float NON_WEIGHT_TANGENT_LENGTH = 50;
        private Color _tangentHandleColor = new Color(0.1f);

        public CurvePointUi(VDefinition key, Curve curve, CurveEditCanvas curveEditor)
        {
            Key = key;
            _curveEditCanvas = curveEditor;
            Curve = curve;
            createCount++;
        }


        public void Draw()
        {
            var pCenter = _curveEditCanvas.TransformPosition(PosOnCanvas);
            var pTopLeft = pCenter - _controlSizeHalf;

            if (!_curveEditCanvas.IsRectVisible(pTopLeft, ControlSize))
                return;

            _curveEditCanvas.DrawList.AddRectFilled(pTopLeft, pTopLeft + ControlSize,
                IsSelected ? Color.White : Color.TBlue);


            if (IsSelected)
            {
                UpdateTangentVectors();
                DrawLeftTangent(pCenter);
                DrawRightTangent(pCenter);
            }

            // Interaction
            ImGui.SetCursorPos(pTopLeft - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("key" + Id.GetHashCode(), ControlSize);
            UiHelpers.THelpers.DebugItemRect();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            HandleInteraction();
        }


        private void DrawLeftTangent(Vector2 pCenter)
        {
            var leftTangentCenter = pCenter + LeftTangentInScreen;
            ImGui.SetCursorPos(leftTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("keyLT" + Id.GetHashCode(), _tangentHandleSize);
            var isHovered = ImGui.IsItemHovered();
            if (isHovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            _curveEditCanvas.DrawList.AddRectFilled(leftTangentCenter - _tangentSizeHalf, leftTangentCenter + _tangentSize,
                    isHovered ? Color.Red : Color.White);
            _curveEditCanvas.DrawList.AddLine(pCenter, leftTangentCenter, _tangentHandleColor);

            // Dragging
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
            {
                Key.InType = VDefinition.Interpolation.Spline;
                Key.InEditMode = VDefinition.EditMode.Tangent;

                var vectorInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetMousePos() - pCenter);
                Key.InTangentAngle = (float)(Math.PI / 2 - Math.Atan2(-vectorInCanvas.X, -vectorInCanvas.Y));

                if (ImGui.GetIO().KeyCtrl)
                    Key.BrokenTangents = true;

                if (!Key.BrokenTangents)
                {
                    Key.OutType = VDefinition.Interpolation.Spline;
                    Key.OutEditMode = VDefinition.EditMode.Tangent;

                    RightTangentInScreen = new Vector2(-LeftTangentInScreen.X, -LeftTangentInScreen.Y);
                    Key.OutTangentAngle = Key.InTangentAngle + Math.PI;
                }
            }
        }


        private void DrawRightTangent(Vector2 pCenter)
        {
            var righTangentCenter = pCenter + RightTangentInScreen;
            ImGui.SetCursorPos(righTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("keyRT" + Id.GetHashCode(), _tangentHandleSize);
            var isHovered = ImGui.IsItemHovered();
            if (isHovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            _curveEditCanvas.DrawList.AddRectFilled(righTangentCenter - _tangentSizeHalf, righTangentCenter + _tangentSize,
                    isHovered ? Color.Red : Color.White);
            _curveEditCanvas.DrawList.AddLine(pCenter, righTangentCenter, _tangentHandleColor);

            // Dragging
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
            {
                Key.OutType = VDefinition.Interpolation.Spline;
                Key.OutEditMode = VDefinition.EditMode.Tangent;

                var vectorInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetMousePos() - pCenter);
                Key.InTangentAngle = (float)(Math.PI / 2 - Math.Atan2(vectorInCanvas.X, vectorInCanvas.Y));

                if (ImGui.GetIO().KeyCtrl)
                    Key.BrokenTangents = true;

                if (!Key.BrokenTangents)
                {
                    Key.InType = VDefinition.Interpolation.Spline;
                    Key.InEditMode = VDefinition.EditMode.Tangent;

                    LeftTangentInScreen = new Vector2(-RightTangentInScreen.X, -RightTangentInScreen.Y);
                    Key.OutTangentAngle = Key.InTangentAngle + Math.PI;
                }
            }
        }


        private void HandleInteraction()
        {
            if (!ImGui.IsItemActive())
                return;

            if (ImGui.IsItemClicked(0))
            {
                // TODO: add modifier keys...
                if (!_curveEditCanvas.SelectionHandler.SelectedElements.Contains(this))
                {
                    _curveEditCanvas.SelectionHandler.SetElement(this);
                }
            }

            if (ImGui.IsMouseDragging(0))
            {
                if (ImGui.GetIO().MouseDelta.Length() > 0)
                {
                    var dInScreen = ImGui.GetIO().MouseDelta;
                    var dInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetIO().MouseDelta);

                    PosOnCanvas += dInCanvas;
                }
            }
        }
        /// <summary>
        /// Tangent in ScreenSspace
        /// </summary>
        private Vector2 LeftTangentInScreen;
        private Vector2 RightTangentInScreen;

        public Vector2 PosOnCanvas
        {
            get
            {
                return new Vector2(
                    (float)Key.U,
                    (float)Key.Value
                );
            }
            set
            {
                Curve.MoveKey(Key.U, value.X);
                Key.Value = value.Y;
            }
        }




        #region moving event handlers


        private enum MoveDirection
        {
            Undecided = 0,
            Vertical,
            Horizontal,
            Both
        }
        // private MoveDirection _moveDirection = MoveDirection.Undecided;


        public void ManipulateV(double newV)
        {
            Key.Value = newV;
        }

        /// <summary>
        /// Important: the caller has to handle undo/redo and make sure to remove/restore potentially overwritten keyframes
        /// </summary>
        public void ManipulateU(double newU)
        {
            // FIXME: This casting to float drastically reduces time precisions for keyframes
            PosOnCanvas = new Vector2((float)newU, PosOnCanvas.Y);
        }

        private static double RoundU(double u)
        {
            return Math.Round(u, 6);
        }


        #endregion


        /// <summary>
        /// Update tanget orientation after changing the scale of the CurveEditor
        /// </summary>
        public void UpdateTangentVectors()
        {
            if (_curveEditCanvas == null)
                return;

            var normVector = new Vector2((float)-Math.Cos(Key.InTangentAngle),
                                          (float)Math.Sin(Key.InTangentAngle));

            LeftTangentInScreen = NormalizeTangentLength(
                                            new Vector2(
                                                normVector.X * _curveEditCanvas.Scale.X,
                                                -_curveEditCanvas.TransformDirection(normVector).Y)
                                            );


            normVector = new Vector2((float)-Math.Cos(Key.OutTangentAngle),
                                     (float)Math.Sin(Key.OutTangentAngle));

            RightTangentInScreen = NormalizeTangentLength(
                                        new Vector2(
                                            normVector.X * _curveEditCanvas.Scale.X,
                                            -_curveEditCanvas.TransformDirection(normVector).Y));
        }

        private Vector2 NormalizeTangentLength(Vector2 tangent)
        {
            var s = (1f / tangent.Length() * NON_WEIGHT_TANGENT_LENGTH);
            return tangent * s;
        }


        // Some constant static vectors to reduce heap impact
        private static Vector2 _tangentHandleSize = new Vector2(10, 10);
        private static Vector2 _tangentHandleSizeHalf = _tangentHandleSize * 0.5f;
        private static Vector2 _tangentSize = new Vector2(2, 2);
        private static Vector2 _tangentSizeHalf = _tangentSize * 0.5f;

        public CurveEditCanvas _curveEditCanvas;





        #region ==== T2 legacy dumpster ====================================

        //static VToYConverter m_VToYConverter = new VToYConverter();
        //static UToXConverter m_UToXConverter = new UToXConverter();


        //private void createBindingsForPositioning()
        //{
        //    MultiBinding multiBinding = new MultiBinding();
        //    multiBinding.Converter = m_VToYConverter;

        //    multiBinding.Bindings.Add(new Binding("V") { Source = curveEditPoint });
        //    multiBinding.Bindings.Add(new Binding("MinV") { Source = CurveEditor });
        //    multiBinding.Bindings.Add(new Binding("MaxV") { Source = CurveEditor });
        //    multiBinding.Bindings.Add(new Binding("ActualHeight") { Source = CurveEditor });
        //    BindingOperations.SetBinding(XTranslateTransform, TranslateTransform.YProperty, multiBinding);

        //    MultiBinding multiBinding2 = new MultiBinding();
        //    multiBinding2.Converter = m_UToXConverter;

        //    multiBinding2.Bindings.Add(new Binding("U") { Source = curveEditPoint });
        //    multiBinding2.Bindings.Add(new Binding("UScale") { Source = CurveEditor });
        //    multiBinding2.Bindings.Add(new Binding("UOffset") { Source = CurveEditor });
        //    BindingOperations.SetBinding(XTranslateTransform, TranslateTransform.XProperty, multiBinding2);
        //}

        //private static readonly DependencyProperty m_IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(CurvePointControl), new UIPropertyMetadata(false));
        //public bool IsSelected { get { return (bool)GetValue(m_IsSelectedProperty); } set { SetValue(m_IsSelectedProperty, value); } }

        //private static readonly DependencyProperty UProperty = DependencyProperty.Register("U", typeof(Double), typeof(CurvePointControl), new UIPropertyMetadata(0.0));
        //public Double U { get { return (Double)GetValue(UProperty); } set { SetValue(UProperty, value); } }

        //private static readonly DependencyProperty VProperty = DependencyProperty.Register("V", typeof(Double), typeof(CurvePointControl), new UIPropertyMetadata(0.0));
        //public Double V
        //{
        //    get { return (Double)GetValue(VProperty); }
        //    set
        //    {
        //        SetValue(VProperty, value);
        //        m_vdef.Value = value;
        //    }
        //}

        //private static readonly DependencyProperty StrokeColorProperty = DependencyProperty.Register("StrokeColor", typeof(Brush), typeof(CurvePointControl), new UIPropertyMetadata(Brushes.Azure));
        //public Brush StrokeColor { get { return (Brush)GetValue(StrokeColorProperty); } set { SetValue(StrokeColorProperty, value); } }

        //private static readonly DependencyProperty TangentVisibilityProperty = DependencyProperty.Register("TangentVisibility", typeof(Visibility), typeof(CurvePointControl), new UIPropertyMetadata(Visibility.Visible));
        //public Visibility TangentVisibility { get { return (Visibility)GetValue(TangentVisibilityProperty); } set { SetValue(TangentVisibilityProperty, value); } }


        //private static readonly DependencyProperty LeftTangentPositionProperty = DependencyProperty.Register("LeftTangentPosition", typeof(Point), typeof(CurvePointControl), new UIPropertyMetadata(new Point(-NON_WEIGHT_TANGENT_LENGTH, 0)));
        //public Point LeftTangentPosition { get { return (Point)GetValue(LeftTangentPositionProperty); } set { SetValue(LeftTangentPositionProperty, value); } }

        //private static readonly DependencyProperty RightTangentPositionProperty = DependencyProperty.Register("RightTangentPosition", typeof(Point), typeof(CurvePointControl), new UIPropertyMetadata(new Point(NON_WEIGHT_TANGENT_LENGTH, 0)));
        //public Point RightTangentPosition { get { return (Point)GetValue(RightTangentPositionProperty); } set { SetValue(RightTangentPositionProperty, value); } }

        //private static readonly DependencyProperty LeftInterpolationTypeProperty = DependencyProperty.Register("LeftInterpolationType", typeof(VDefinition.EditMode), typeof(CurvePointControl), new UIPropertyMetadata(VDefinition.EditMode.Linear));
        //public VDefinition.EditMode LeftInterpolationType { get { return (VDefinition.EditMode)GetValue(LeftInterpolationTypeProperty); } set { SetValue(LeftInterpolationTypeProperty, value); } }
        //private VDefinition.EditMode LeftInterpolationType { get; set; }

        //private static readonly DependencyProperty RightInterpolationTypeProperty = DependencyProperty.Register("RightInterpolationType", typeof(VDefinition.EditMode), typeof(CurvePointControl), new UIPropertyMetadata(VDefinition.EditMode.Linear));
        //public VDefinition.EditMode RightInterpolationType { get { return (VDefinition.EditMode)GetValue(RightInterpolationTypeProperty); } set { SetValue(RightInterpolationTypeProperty, value); } }


        //public void InitFromVDefinition(VDefinition vdef)
        //{
        //    m_vdef = vdef;
        //    V = vdef.Value;
        //    LeftInterpolationType = vdef.InEditMode;
        //    RightInterpolationType = vdef.OutEditMode;
        //    UpdateControlTangents();
        //}



        //private void OnDragStart(object sender, DragStartedEventArgs e)
        //{
        //    XCenterThumb.Cursor = Cursors.Cross;
        //    m_MoveDirection = MoveDirection.Undecided;
        //    if (Keyboard.Modifiers != ModifierKeys.Shift)
        //    {
        //        var alreadySelected = CurveEditor._SelectionHandler.SelectedElements.Count == 1 &&
        //                              Equals(CurveEditor._SelectionHandler.SelectedElements.First(), this);

        //        if (!alreadySelected)
        //            CurveEditor._SelectionHandler.Clear();
        //    }
        //    CurveEditor._SelectionHandler.AddElement(this);
        //    _addOrUpdateKeyframeCommand = new AddOrUpdateKeyframeCommand(U, key, Curve);
        //    _moveKeyframeCommand = new MoveKeyframeCommand(U, U, Curve);
        //}

        //private AddOrUpdateKeyframeCommand _addOrUpdateKeyframeCommand;
        //private MoveKeyframeCommand _moveKeyframeCommand;

        //const double DRAG_THRESHOLD = 4;
        //private void OnDragDelta(object sender, DragDeltaEventArgs e)
        //{
        //    var delta = new Vector(e.HorizontalChange, e.VerticalChange);

        //    double deltaU = CurveEditor.xToU(delta.X) - CurveEditor.xToU(0);
        //    double deltaV = CurveEditor.yToV(delta.Y) - CurveEditor.yToV(0);

        //    if (m_MoveDirection == MoveDirection.Undecided)
        //    {
        //        if (Math.Abs(delta.X) + Math.Abs(delta.Y) > DRAG_THRESHOLD)
        //        {
        //            if (Math.Abs(delta.X) > Math.Abs(delta.Y))
        //            {
        //                m_MoveDirection = MoveDirection.Horizontal;
        //                XCenterThumb.Cursor = Cursors.ScrollWE;
        //            }
        //            else
        //            {
        //                m_MoveDirection = MoveDirection.Vertical;
        //                XCenterThumb.Cursor = Cursors.ScrollNS;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        CurveEditor.DisableRebuildOnCurveChangeEvents();

        //        if (m_MoveDirection == MoveDirection.Vertical)
        //        {
        //            V += deltaV;
        //        }

        //        if (m_MoveDirection == MoveDirection.Horizontal)
        //        {
        //            // Snap when pressing Shift
        //            if (TV != null &&
        //                TV.TimeSnapHandler != null &&
        //                Keyboard.Modifiers == ModifierKeys.Shift)
        //            {
        //                var snapU = TV.TimeSnapHandler.CheckForSnapping(U + deltaU);
        //                if (!Double.IsNaN(snapU))
        //                {
        //                    deltaU = snapU - U;
        //                }
        //            }

        //            // Prevent overwriting existing keys
        //            ManipulateU(U + deltaU);
        //        }

        //        switch (m_MoveDirection)
        //        {
        //            case MoveDirection.Vertical:
        //                _addOrUpdateKeyframeCommand.KeyframeValue = key;
        //                _addOrUpdateKeyframeCommand.Do();
        //                break;
        //            case MoveDirection.Horizontal:
        //                _moveKeyframeCommand.NewTime = U;
        //                //_moveKeyframeCommand.Do();
        //                break;
        //        }

        //        key = Curve.GetV(U);     // since SetOrUpdateV clones vdef, we have to get a new value

        //        if (TV != null)
        //            TV.TriggerRepaint();

        //        UpdateControlTangents();
        //        CurveEditor.EnableRebuildOnCurveChangeEvents();
        //        App.Current.UpdateRequiredAfterUserInteraction = true;
        //        CurveEditor.UpdateLine(Curve);
        //        //CurveEditor.UpdateEditBox();
        //    }
        //}
        //private void OnDragCompleted(object sender, DragCompletedEventArgs e)
        //{
        //    XCenterThumb.Cursor = Cursors.Arrow;
        //    if (m_MoveDirection == MoveDirection.Vertical)
        //    {
        //        App.Current.UndoRedoStack.Add(_addOrUpdateKeyframeCommand);
        //    }
        //    else if (m_MoveDirection == MoveDirection.Horizontal)
        //    {
        //        App.Current.UndoRedoStack.Add(_moveKeyframeCommand);
        //    }
        //    _addOrUpdateKeyframeCommand = null;
        //    _moveKeyframeCommand = null;
        //    m_CE.RebuildCurrentCurves();
        //}



        //private void OnDragTangentDeltaStarted(object sender, DragStartedEventArgs e)
        //{
        //    _addOrUpdateKeyframeCommand = new AddOrUpdateKeyframeCommand(U, Key, Curve);
        //}

        //private void OnDragLeftTangentDelta(object sender, DragDeltaEventArgs e)
        //{
        //    CurveEditor.DisableRebuildOnCurveChangeEvents();
        //    LeftTangentPosition += new Vector(e.HorizontalChange, e.VerticalChange);
        //    var v = LimitWeightTanget(new Vector(Math.Min(LeftTangentPosition.X, 0), LeftTangentPosition.Y));
        //    LeftTangentPosition = new Point(v.X, v.Y);
        //    Key.InType = VDefinition.Interpolation.Spline;
        //    Key.InEditMode = VDefinition.EditMode.Tangent;

        //    double angleIn = Math.PI / 2 - Math.Atan2(CurveEditor.xToU(0.0) - CurveEditor.xToU(v.X), CurveEditor.yToV(0.0) - CurveEditor.yToV(v.Y));

        //    Key.InTangentAngle = angleIn;

        //    if (Keyboard.Modifiers == ModifierKeys.Control)
        //    {
        //        Key.BrokenTangents = true;
        //    }

        //    if (!Key.BrokenTangents)
        //    {
        //        Key.OutType = VDefinition.Interpolation.Spline;
        //        Key.OutEditMode = VDefinition.EditMode.Tangent;

        //        RightTangentPosition = new Point(-v.X, -v.Y);
        //        Key.OutTangentAngle = angleIn + Math.PI;
        //    }

        //    _addOrUpdateKeyframeCommand.KeyframeValue = Key;
        //    _addOrUpdateKeyframeCommand.Do();

        //    if (TV != null)
        //        TV.TriggerRepaint();
        //    CurveEditor.EnableRebuildOnCurveChangeEvents();
        //    CurveEditor.UpdateLine(Curve);
        //}

        //private void OnDragRightTangentDelta(object sender, DragDeltaEventArgs e)
        //{
        //    CurveEditor.DisableRebuildOnCurveChangeEvents();

        //    RightTangentPosition += new Vector(e.HorizontalChange, e.VerticalChange);
        //    var v = LimitWeightTanget(new Vector(Math.Max(RightTangentPosition.X, 0), RightTangentPosition.Y));
        //    RightTangentPosition = new Point(v.X, v.Y);
        //    Key.OutType = VDefinition.Interpolation.Spline;
        //    Key.OutEditMode = VDefinition.EditMode.Tangent;

        //    double angleOut = Math.PI / 2 - Math.Atan2(CurveEditor.xToU(0.0) - CurveEditor.xToU(v.X), CurveEditor.yToV(0.0) - CurveEditor.yToV(v.Y));
        //    Key.OutTangentAngle = angleOut;
        //    if (Keyboard.Modifiers == ModifierKeys.Control)
        //    {
        //        Key.BrokenTangents = true;
        //    }

        //    if (!Key.BrokenTangents)
        //    {
        //        Key.InType = VDefinition.Interpolation.Spline;
        //        Key.InEditMode = VDefinition.EditMode.Tangent;
        //        LeftTangentPosition = new Point(-v.X, -v.Y);
        //        Key.InTangentAngle = angleOut - Math.PI;
        //    }

        //    _addOrUpdateKeyframeCommand.KeyframeValue = Key;
        //    _addOrUpdateKeyframeCommand.Do();

        //    if (TV != null)
        //        TV.TriggerRepaint();

        //    CurveEditor.EnableRebuildOnCurveChangeEvents();
        //    CurveEditor.UpdateLine(Curve);
        //}

        //private void OnDragTangentDeltaCompleted(object sender, DragCompletedEventArgs e)
        //{
        //    App.Current.UndoRedoStack.Add(_addOrUpdateKeyframeCommand);
        //    _addOrUpdateKeyframeCommand = null;
        //}





        //#region Value converter
        //public class UToXConverter : IMultiValueConverter
        //{
        //    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        if (values.Count() != 3 || values.Contains(DependencyProperty.UnsetValue))
        //        {
        //            return 0.0;
        //        }

        //        double u = (double)values[0];
        //        double timeScale = (double)values[1];
        //        double timeOffset = (double)values[2];
        //        return (u - timeOffset) * timeScale;
        //    }

        //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}


        ///**
        // * Binds U, minY, maxY and actualHeight
        // */
        //public class VToYConverter : IMultiValueConverter
        //{
        //    public object Convert(object[] values, Type targetType, object parameter,
        //        System.Globalization.CultureInfo culture)
        //    {
        //        if (values.Count() != 4 || values.Contains(DependencyProperty.UnsetValue))
        //        {
        //            return 10.0;
        //        }

        //        double v = (double)values[0];
        //        double minV = (double)values[1];
        //        double maxV = (double)values[2];
        //        double height = (double)values[3];
        //        double y = height - (v - minV) / (maxV - minV) * height;
        //        return y;
        //    }

        //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
        //        System.Globalization.CultureInfo culture)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}



        //public class SelectedToVisibilityConverter : IValueConverter
        //{
        //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        if ((bool)value == true)
        //        {
        //            return Visibility.Visible;
        //        }
        //        else
        //        {
        //            return Visibility.Hidden;
        //        }
        //    }

        //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        if ((Visibility)value == Visibility.Visible)
        //        {
        //            return true;
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //}


        //public class LeftInterpolationTypeToPathDataConverter : IValueConverter
        //{
        //    static private Geometry linearFace = Geometry.Parse("M 0, 5 L -5,0 0,-5");
        //    static private Geometry splineFace = Geometry.Parse("M 0, 5 L -3.4,3.4 -5,0 -3.4,-3.4 0,-5");
        //    static private Geometry horizontalFace = Geometry.Parse("M 0, 5 L -5,5 -5,-5 0,-5 ");

        //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        VDefinition.EditMode type = (VDefinition.EditMode)value;
        //        switch (type)
        //        {
        //            case VDefinition.EditMode.Linear:
        //                return linearFace;
        //            case VDefinition.EditMode.Tangent:
        //            case VDefinition.EditMode.Smooth:
        //                return splineFace;
        //            default:
        //                return horizontalFace;
        //        }
        //    }

        //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        return VDefinition.Interpolation.Linear;
        //    }

        //}

        //public class RightInterpolationTypeToPathDataConverter : IValueConverter
        //{
        //    static private Geometry linearFace = Geometry.Parse("M 0, 5 L 5,0 0,-5");
        //    static private Geometry splineFace = Geometry.Parse("M 0, 5 L 3.4,3.4   5,0   3.4,-3.4   0,-5");
        //    static private Geometry horizontalFace = Geometry.Parse("M 0, 5 L 5,5 5,-5 0,-5 ");

        //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        VDefinition.EditMode type = (VDefinition.EditMode)value;
        //        switch (type)
        //        {
        //            case VDefinition.EditMode.Linear:
        //                return linearFace;
        //            case VDefinition.EditMode.Tangent:
        //            case VDefinition.EditMode.Smooth:
        //                return splineFace;
        //            default:
        //                return horizontalFace;
        //        }
        //    }

        //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        //    {
        //        return VDefinition.Interpolation.Linear;
        //    }

        //}

        //#endregion
        #endregion

    }

}
