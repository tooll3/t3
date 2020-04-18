using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Animation;
using T3.Gui.Styling;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.Animation.CurveEditing
{
    /// <summary>
    /// Interaction logic for CurvePointControl.xaml
    /// </summary>
    public static class CurvePoint
    {
        public static void Draw(VDefinition vDef, ICanvas curveEditCanvas, bool isSelected, TimelineCurveEditArea timelineCurveEditArea)
        {
            _drawlist = ImGui.GetWindowDrawList();
            _curveEditCanvas = curveEditCanvas;
            _vDef = vDef;

            var pCenter = _curveEditCanvas.TransformPosition(new Vector2((float)vDef.U, (float)vDef.Value));
            var pTopLeft = pCenter - ControlSizeHalf;
            
            ImGui.PushFont(Icons.IconFont);
            _drawlist.AddText(pTopLeft - new Vector2(2, 4), Color.White, isSelected ? KeyframeIconSelected : KeyframeIcon);
            ImGui.PopFont();

            if (isSelected)
            {
                UpdateTangentVectors();
                DrawLeftTangent(pCenter);
                DrawRightTangent(pCenter);
            }

            // Interaction
            ImGui.SetCursorPos(pTopLeft - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("key" + vDef.GetHashCode(), ControlSize);
            THelpers.DebugItemRect();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            timelineCurveEditArea?.HandleCurvePointDragging(_vDef, isSelected);
        }

        private static void DrawLeftTangent(Vector2 pCenter)
        {
            var leftTangentCenter = pCenter + _leftTangentInScreen;
            ImGui.SetCursorPos(leftTangentCenter - TangentHandleSizeHalf - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("keyLT" + _vDef.GetHashCode(), TangentHandleSize);
            var isHovered = ImGui.IsItemHovered();
            if (isHovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            _drawlist.AddRectFilled(leftTangentCenter - TangentSizeHalf, leftTangentCenter + TangentSize,
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

                    _rightTangentInScreen = new Vector2(-_leftTangentInScreen.X, -_leftTangentInScreen.Y);
                    _vDef.OutTangentAngle = _vDef.InTangentAngle + Math.PI;
                }
            }
        }

        private static void DrawRightTangent(Vector2 pCenter)
        {
            var rightTangentCenter = pCenter + _rightTangentInScreen;
            ImGui.SetCursorPos(rightTangentCenter - TangentHandleSizeHalf - _curveEditCanvas.WindowPos);
            ImGui.InvisibleButton("keyRT" + _vDef.GetHashCode(), TangentHandleSize);
            var isHovered = ImGui.IsItemHovered();
            if (isHovered)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            _drawlist.AddRectFilled(rightTangentCenter - TangentSizeHalf, rightTangentCenter + TangentSize,
                                    isHovered ? Color.Red : Color.White);
            _drawlist.AddLine(pCenter, rightTangentCenter, TangentHandleColor);

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

                    _leftTangentInScreen = new Vector2(-_rightTangentInScreen.X, -_rightTangentInScreen.Y);
                    _vDef.OutTangentAngle = _vDef.InTangentAngle + Math.PI;
                }
            }
        }

        /// <summary>
        /// Update tangent orientation after changing the scale of the CurveEditor
        /// </summary>
        private static void UpdateTangentVectors()
        {
            if (_curveEditCanvas == null)
                return;

            var normVector = new Vector2((float)-Math.Cos(_vDef.InTangentAngle),
                                         (float)Math.Sin(_vDef.InTangentAngle));

            _leftTangentInScreen = NormalizeTangentLength(
                                                          new Vector2(
                                                                      normVector.X * _curveEditCanvas.Scale.X,
                                                                      -_curveEditCanvas.TransformDirection(normVector).Y)
                                                         );

            normVector = new Vector2((float)-Math.Cos(_vDef.OutTangentAngle),
                                     (float)Math.Sin(_vDef.OutTangentAngle));

            _rightTangentInScreen = NormalizeTangentLength(
                                                           new Vector2(
                                                                       normVector.X * _curveEditCanvas.Scale.X,
                                                                       -_curveEditCanvas.TransformDirection(normVector).Y));
        }

        private static Vector2 NormalizeTangentLength(Vector2 tangent)
        {
            var s = (1f / tangent.Length() * NonWeightTangentLength);
            return tangent * s;
        }

        private static ICanvas _curveEditCanvas;
        private static VDefinition _vDef;
        private static ImDrawListPtr _drawlist;

        private static Vector2 _leftTangentInScreen;
        private static Vector2 _rightTangentInScreen;

        // Look & style
        private static readonly Vector2 ControlSize = new Vector2(10, 10);
        private static readonly Vector2 ControlSizeHalf = ControlSize * 0.5f;

        private const float NonWeightTangentLength = 50;
        private static readonly Color TangentHandleColor = new Color(0.1f);

        private static readonly Vector2 TangentHandleSize = new Vector2(10, 10);
        private static readonly Vector2 TangentHandleSizeHalf = TangentHandleSize * 0.5f;
        private static readonly Vector2 TangentSize = new Vector2(2, 2);
        private static readonly Vector2 TangentSizeHalf = TangentSize * 0.5f;

        private static readonly string KeyframeIcon = "" + (char)(int)Icon.CurveKeyframe;
        private static readonly string KeyframeIconSelected = "" + (char)(int)Icon.CurveKeyframeSelected;

        /* TODO: MoveDirection needs to be implemented eventually
        private enum MoveDirection
        {
            Undecided = 0,
            Vertical,
            Horizontal,
            Both
        }
        private MoveDirection _moveDirection = MoveDirection.Undecided;
        */
    }
}