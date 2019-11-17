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
    public static class CurvePointUi
    {
        //public Vector2 Size { get; set; } = new Vector2(0, 0);
        private static Vector2 ControlSize = new Vector2(10, 10);
        private static Vector2 _controlSizeHalf = ControlSize * 0.5f;

        //public bool IsSelected { get; set; }
        //public VDefinition Key;
        //public Curve Curve { get; set; }
        //public Guid Id { get; private set; } = Guid.NewGuid();

        //public static int createCount = 0;
        
        private const float NonWeightTangentLength = 50;
        private static readonly Color TangentHandleColor = new Color(0.1f);

        // public  CurvePointUi(VDefinition key, Curve curve, ICanvas canvas)
        // {
        //     Key = key;
        //     _curveEditCanvas = canvas;
        //     Curve = curve;
        //     createCount++;
        // }
        
        
        public static void Draw(VDefinition vDef, ICanvas curveEditCanvas, bool isSelected)
        {
            _drawlist = ImGui.GetWindowDrawList();
            _curveEditCanvas = curveEditCanvas;
            _vDef = vDef;
            
            var pCenter = _curveEditCanvas.TransformPosition(new Vector2((float)vDef.U, (float)vDef.Value));
            var pTopLeft = pCenter - _controlSizeHalf;

            // if (!_curveEditCanvas.IsRectVisible(pTopLeft, ControlSize))
            //     return;

            _drawlist.AddRectFilled(pTopLeft, pTopLeft + ControlSize,
                isSelected ? Color.White : Color.Blue);


            if (isSelected)
            {
                UpdateTangentVectors();
                DrawLeftTangent(pCenter);
                DrawRightTangent(pCenter);
            }

            // Interaction
            ImGui.SetCursorPos(pTopLeft - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("key" +  vDef.GetHashCode(), ControlSize);
            UiHelpers.THelpers.DebugItemRect();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            HandleInteraction();
        }


        private static void DrawLeftTangent(Vector2 pCenter)
        {
            var leftTangentCenter = pCenter + LeftTangentInScreen;
            ImGui.SetCursorPos(leftTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("keyLT" + _vDef.GetHashCode(), _tangentHandleSize);
            var isHovered = ImGui.IsItemHovered();
            if (isHovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            _drawlist.AddRectFilled(leftTangentCenter - _tangentSizeHalf, leftTangentCenter + _tangentSize,
                    isHovered ? Color.Red : Color.White);
            _drawlist.AddLine(pCenter, leftTangentCenter, TangentHandleColor);

            // Dragging
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
            {
                _vDef.InType = VDefinition.Interpolation.Spline;
                _vDef.InEditMode = VDefinition.EditMode.Tangent;

                var vectorInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetMousePos() - pCenter);
                _vDef.InTangentAngle = (float)(Math.PI / 2 - Math.Atan2(-vectorInCanvas.X, -vectorInCanvas.Y));

                if (ImGui.GetIO().KeyCtrl)
                    _vDef.BrokenTangents = true;

                if (!_vDef.BrokenTangents)
                {
                    _vDef.OutType = VDefinition.Interpolation.Spline;
                    _vDef.OutEditMode = VDefinition.EditMode.Tangent;

                    RightTangentInScreen = new Vector2(-LeftTangentInScreen.X, -LeftTangentInScreen.Y);
                    _vDef.OutTangentAngle = _vDef.InTangentAngle + Math.PI;
                }
            }
        }


        private static void DrawRightTangent(Vector2 pCenter)
        {
            var righTangentCenter = pCenter + RightTangentInScreen;
            ImGui.SetCursorPos(righTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("keyRT" + _vDef.GetHashCode(), _tangentHandleSize);
            var isHovered = ImGui.IsItemHovered();
            if (isHovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            _drawlist.AddRectFilled(righTangentCenter - _tangentSizeHalf, righTangentCenter + _tangentSize,
                    isHovered ? Color.Red : Color.White);
            _drawlist.AddLine(pCenter, righTangentCenter, TangentHandleColor);

            // Dragging
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
            {
                _vDef.OutType = VDefinition.Interpolation.Spline;
                _vDef.OutEditMode = VDefinition.EditMode.Tangent;

                var vectorInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetMousePos() - pCenter);
                _vDef.InTangentAngle = (float)(Math.PI / 2 - Math.Atan2(vectorInCanvas.X, vectorInCanvas.Y));

                if (ImGui.GetIO().KeyCtrl)
                    _vDef.BrokenTangents = true;

                if (!_vDef.BrokenTangents)
                {
                    _vDef.InType = VDefinition.Interpolation.Spline;
                    _vDef.InEditMode = VDefinition.EditMode.Tangent;

                    LeftTangentInScreen = new Vector2(-RightTangentInScreen.X, -RightTangentInScreen.Y);
                    _vDef.OutTangentAngle = _vDef.InTangentAngle + Math.PI;
                }
            }
        }

        private static VDefinition _vDef;

        private static void HandleInteraction()
        {
            if (!ImGui.IsItemActive())
                return;

            if (ImGui.IsItemClicked(0))
            {
                // TODO: select on click
            }

            if (ImGui.IsMouseDragging(0))
            {
                if (!(ImGui.GetIO().MouseDelta.Length() > 0))
                    return;
                
                var dInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetIO().MouseDelta);

                _vDef.U += dInCanvas.X;
                _vDef.Value += dInCanvas.Y;
            }
        }
   
        /// <summary>
        /// Tangent in ScreenSspace
        /// </summary>
        private static Vector2 LeftTangentInScreen;
        private static Vector2 RightTangentInScreen;

        // public Vector2 PosOnCanvas
        // {
        //     get
        //     {
        //         return new Vector2(
        //             (float)_Key.U,
        //             (float)_Key.Value
        //         );
        //     }
        //     set
        //     {
        //         Curve.MoveKey(_Key.U, value.X);
        //         _Key.Value = value.Y;
        //     }
        // }
        


        #region moving event handlers


        private enum MoveDirection
        {
            Undecided = 0,
            Vertical,
            Horizontal,
            Both
        }
        // private MoveDirection _moveDirection = MoveDirection.Undecided;


        // public void ManipulateV(double newV)
        // {
        //     _Key.Value = newV;
        // }

        /// <summary>
        /// Important: the caller has to handle undo/redo and make sure to remove/restore potentially overwritten keyframes
        /// </summary>
        // public void ManipulateU(double newU)
        // {
        //     // FIXME: This casting to float drastically reduces time precisions for keyframes
        //     PosOnCanvas = new Vector2((float)newU, PosOnCanvas.Y);
        // }

        private static double RoundU(double u)
        {
            return Math.Round(u, 6);
        }


        #endregion

        //private static ICanvas _curveEditCanvas; 
        
        /// <summary>
        /// Update tangent orientation after changing the scale of the CurveEditor
        /// </summary>
        private static  void UpdateTangentVectors()
        {
            if (_curveEditCanvas == null)
                return;

            var normVector = new Vector2((float)-Math.Cos(_vDef.InTangentAngle),
                                          (float)Math.Sin(_vDef.InTangentAngle));

            LeftTangentInScreen = NormalizeTangentLength(
                                            new Vector2(
                                                normVector.X * _curveEditCanvas.Scale.X,
                                                -_curveEditCanvas.TransformDirection(normVector).Y)
                                            );


            normVector = new Vector2((float)-Math.Cos(_vDef.OutTangentAngle),
                                     (float)Math.Sin(_vDef.OutTangentAngle));

            RightTangentInScreen = NormalizeTangentLength(
                                        new Vector2(
                                            normVector.X * _curveEditCanvas.Scale.X,
                                            -_curveEditCanvas.TransformDirection(normVector).Y));
        }

        private static Vector2 NormalizeTangentLength(Vector2 tangent)
        {
            var s = (1f / tangent.Length() * NonWeightTangentLength);
            return tangent * s;
        }


        // Some constant static vectors to reduce heap impact
        private static Vector2 _tangentHandleSize = new Vector2(10, 10);
        private static Vector2 _tangentHandleSizeHalf = _tangentHandleSize * 0.5f;
        private static Vector2 _tangentSize = new Vector2(2, 2);
        private static Vector2 _tangentSizeHalf = _tangentSize * 0.5f;
        private static ImDrawListPtr _drawlist;

        private static ICanvas _curveEditCanvas;
    }
}
