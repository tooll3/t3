using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Interaction.WithCurves;

/// <summary>
/// Interaction logic for CurvePointControl.xaml
/// </summary>
internal static class CurvePoint
{
    public static void Draw(in Guid compositionSymbolId, VDefinition vDef, ICanvas curveEditCanvas, bool isSelected, CurveEditing curveEditing)
    {
        _drawList = ImGui.GetWindowDrawList();
        _curveEditCanvas = curveEditCanvas;
        _vDef = vDef;

        var pCenter = _curveEditCanvas.TransformPosition(new Vector2((float)vDef.U, (float)vDef.Value));
        var pTopLeft = pCenter - _controlSizeHalf;
            

        if (isSelected)
        {
            UpdateTangentVectors();
            DrawLeftTangent(pCenter);
            DrawRightTangent(pCenter);
        }

        // Interaction

        ImGui.SetCursorPos(pTopLeft - _curveEditCanvas.WindowPos + _fixOffset);
        ImGui.InvisibleButton("key" + vDef.GetHashCode(), _controlSize);
        THelpers.DebugItemRect();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
            
        ImGui.PushFont(Icons.IconFont);
        var fadeFactor = ImGui.IsItemHovered() ? 1f : 0.9f;
        var color = Color.White.Fade(fadeFactor);
        _drawList.AddText(pTopLeft + new Vector2(5,4) , color, isSelected ? _keyframeIconSelected : _keyframeIcon);
        ImGui.PopFont();

        curveEditing?.HandleCurvePointDragging(compositionSymbolId, _vDef, isSelected);
    }

    private static void DrawLeftTangent(Vector2 pCenter)
    {
        var leftTangentCenter = pCenter + _leftTangentInScreen;
            
        ImGui.SetCursorPos(leftTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos + _fixOffset);
        ImGui.InvisibleButton("keyLT" + _vDef.GetHashCode(), _tangentHandleSize);
        THelpers.DebugItemRect();
        var isHovered = ImGui.IsItemHovered();
        if (isHovered)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        _drawList.AddRectFilled(leftTangentCenter - _tangentSizeHalf, leftTangentCenter + _tangentSize,
                                isHovered ? UiColors.ForegroundFull : UiColors.Text);
        _drawList.AddLine(pCenter, leftTangentCenter, _tangentHandleColor);

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
        ImGui.SetCursorPos(rightTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos + _fixOffset);
        ImGui.InvisibleButton("keyRT" + _vDef.GetHashCode(), _tangentHandleSize);
        var isHovered = ImGui.IsItemHovered();
        if (isHovered)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        _drawList.AddRectFilled(rightTangentCenter - _tangentSizeHalf, rightTangentCenter + _tangentSize,
                                isHovered ? UiColors.ForegroundFull : UiColors.Text);
        _drawList.AddLine(pCenter, rightTangentCenter, _tangentHandleColor);

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
    private static ImDrawListPtr _drawList;

    private static Vector2 _leftTangentInScreen;
    private static Vector2 _rightTangentInScreen;

    // Look & style
    private static readonly Vector2 _controlSize = new(21, 21);
    private static readonly Vector2 _controlSizeHalf = _controlSize * 0.5f;

    private static readonly Vector2 _fixOffset = new(1, 7);  // Sadly their is a magic vertical offset probably caused by border or padding
        
    private const float NonWeightTangentLength = 50;
    private static readonly Color _tangentHandleColor = new(0.1f);

    private static readonly Vector2 _tangentHandleSize = new(21, 21);
    private static readonly Vector2 _tangentHandleSizeHalf = _tangentHandleSize * 0.5f;
    private static readonly Vector2 _tangentSize = new(2, 2);
    private static readonly Vector2 _tangentSizeHalf = _tangentSize * 0.5f;

    private static readonly string _keyframeIcon = "" + (char)(int)Icon.CurveKeyframe;
    private static readonly string _keyframeIconSelected = "" + (char)(int)Icon.CurveKeyframeSelected;


}