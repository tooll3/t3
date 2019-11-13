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

        public CurvePointUi(VDefinition key, Curve curve, ICanvas canvas)
        {
            Key = key;
            _curveEditCanvas = canvas;
            Curve = curve;
            createCount++;
        }
        
        public void Draw()
        {
            var pCenter = _curveEditCanvas.TransformPosition(PosOnCanvas);
            var pTopLeft = pCenter - _controlSizeHalf;
            _drawlist = ImGui.GetWindowDrawList();

            // if (!_curveEditCanvas.IsRectVisible(pTopLeft, ControlSize))
            //     return;

            _drawlist.AddRectFilled(pTopLeft, pTopLeft + ControlSize,
                IsSelected ? Color.White : Color.Blue);


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

            _drawlist.AddRectFilled(leftTangentCenter - _tangentSizeHalf, leftTangentCenter + _tangentSize,
                    isHovered ? Color.Red : Color.White);
            _drawlist.AddLine(pCenter, leftTangentCenter, _tangentHandleColor);

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

            _drawlist.AddRectFilled(righTangentCenter - _tangentSizeHalf, righTangentCenter + _tangentSize,
                    isHovered ? Color.Red : Color.White);
            _drawlist.AddLine(pCenter, righTangentCenter, _tangentHandleColor);

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
                // // TODO: add modifier keys...
                // if (!_curveEditCanvas.SelectionHandler.SelectedElements.Contains(this))
                // {
                //     _curveEditCanvas.SelectionHandler.SetElement(this);
                // }
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
        private ImDrawListPtr _drawlist;
        
        public ICanvas _curveEditCanvas;
    }

}
