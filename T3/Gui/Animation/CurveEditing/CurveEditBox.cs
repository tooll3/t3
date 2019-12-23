using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Graph;
using T3.Gui.Selection;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Animation.CurveEditing
{
    public class CurveEditBox
    {

        public CurveEditBox(ICanvas canvas)
        {
            _canvas = canvas;
            //_selectionHandler = selectionHandler;
        }

        private void ManipulateKeyframeValue(ref VDefinition vDef, float value)
        {
            
        }

        private HashSet<VDefinition> _selectedKeyframes;
        
        public void Draw(HashSet<VDefinition> selectedKeyframes )
        {
            if (selectedKeyframes.Count <= 1)
                return;
            
            _selectedKeyframes = selectedKeyframes;
            var drawlist = ImGui.GetWindowDrawList();

            _bounds = GetBoundingBox();
            var minU = _bounds.Min.X;
            var maxU = _bounds.Max.X;
            var minV = _bounds.Min.Y;
            var maxV = _bounds.Max.Y;

            var boundsOnScreen = _canvas.TransformRect(_bounds);
            drawlist.AddRect(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderColor);
            drawlist.AddRectFilled(boundsOnScreen.Min, boundsOnScreen.Max, SelectBoxBorderFill);

            var deltaOnCanvas = _canvas.InverseTransformDirection(ImGui.GetIO().MouseDelta);

            /*
            ScaleHandle(
                id: "##top",
                screenPos: boundsOnScreen.Min - VerticalHandleOffset,
                size: new Vector2(boundsOnScreen.GetWidth(), DragHandleSize),
                Direction.Vertical,
                scale: (maxV + deltaOnCanvas.Y - minV) / (maxV - minV),
                (scale, ep) => { ep.ManipulateV(minV + (ep.PosOnCanvas.Y - minV) * scale); }
                //(scale, ep) => { ManipulateKeyframeValue(ep, minV + (ep.PosOnCanvas.Y - minV) * scale); }
                );

            ScaleHandle(
                id: "##bottom",
                screenPos: new Vector2(boundsOnScreen.Min.X, boundsOnScreen.Max.Y),
                size: new Vector2(boundsOnScreen.GetWidth(), DragHandleSize),
                Direction.Vertical,
                scale: (maxV - deltaOnCanvas.Y - minV) / (maxV - minV),
                (scale, ep) => { ep.ManipulateV(maxV - (maxV - ep.PosOnCanvas.Y) * scale); }
                );

            ScaleHandle(
                id: "##right",
                screenPos: new Vector2(boundsOnScreen.Max.X, boundsOnScreen.Min.Y),
                size: new Vector2(DragHandleSize, boundsOnScreen.GetHeight()),
                Direction.Horizontal,
                scale: (maxU + deltaOnCanvas.X - minU) / (maxU - minU),
                (scale, ep) => { ep.ManipulateU(minU + (ep.PosOnCanvas.X - minU) * scale); }
                );

            ScaleHandle(
                id: "##left",
                screenPos: boundsOnScreen.Min - HorizontalHandleOffset,
                size: new Vector2(DragHandleSize, boundsOnScreen.GetHeight()),
                Direction.Horizontal,
                scale: (maxU - deltaOnCanvas.X - minU) / (maxU - minU),
                (scale, ep) => { ep.ManipulateU(maxU - (maxU - ep.PosOnCanvas.X) * scale); }
                );
                */

            var combined = (MoveRingInnerRadius + MoveRingOuterRadius);
            var center = _canvas.TransformPositionFloored(_bounds.GetCenter());

            MoveHandle(
                "<##moveLeft", Direction.Horizontal,
                screenPos: center + new Vector2(-MoveRingOuterRadius, -0.5f * combined),
                size: new Vector2(MoveRingOuterRadius - MoveRingInnerRadius, combined));


            MoveHandle(
                "^##moveUp", Direction.Vertical,
                screenPos: center + new Vector2(-0.5f * combined, -MoveRingOuterRadius),
                size: new Vector2(combined, MoveRingOuterRadius - MoveRingInnerRadius));

            MoveHandle(
                ">##moveRight", Direction.Horizontal,
                screenPos: center + new Vector2(MoveRingInnerRadius, -0.5f * combined),
                size: new Vector2(MoveRingOuterRadius - MoveRingInnerRadius, combined));

            MoveHandle(
                "v##moveDown", Direction.Vertical,
                screenPos: center + new Vector2(-0.5f * combined, MoveRingInnerRadius),
                size: new Vector2(combined, MoveRingOuterRadius - MoveRingInnerRadius));

            MoveHandle(
                "+##both", Direction.Both,
                screenPos: center - new Vector2(1, 1) * MoveRingInnerRadius,
                size: Vector2.One * MoveRingInnerRadius * 2);


        }

        private const float MoveRingOuterRadius = 25;
        private const float MoveRingInnerRadius = 10;

        private enum Direction
        {
            Horizontal,
            Vertical,
            Both,
        }

        private void ScaleHandle(string id, Vector2 screenPos, Vector2 size, Direction direction, float scale, Action<float, VDefinition> scaleFunction)
        {
            if ((direction == Direction.Vertical && _bounds.GetHeight() <= 0.001f)
             || (direction == Direction.Horizontal && _bounds.GetWidth() <= 0.001f))
                return;

            ImGui.SetCursorScreenPos(screenPos);

            ImGui.Button(id, size);

            if (ImGui.IsItemActive() || ImGui.IsItemHovered())
                ImGui.SetMouseCursor(
                    direction == Direction.Horizontal
                        ? ImGuiMouseCursor.ResizeEW
                        : ImGuiMouseCursor.ResizeNS);

            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0))
                return;

            if (Double.IsNaN(scale) || Math.Abs(scale) > 10000)
                return;

            if (scale < 0)
                scale = 0;

            
            foreach (var ep in _selectedKeyframes)
            {
                //scaleFunction(scale, ep);
            }
        }


        private void MoveHandle(string labelAndId, Direction direction, Vector2 screenPos, Vector2 size)
        {
            ImGui.SetCursorScreenPos(screenPos);

            ImGui.Button(labelAndId, size);

            if (ImGui.IsItemActive() || ImGui.IsItemHovered())
            {
                switch (direction)
                {
                    case Direction.Horizontal:
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW); break;
                    case Direction.Vertical:
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS); break;
                    case Direction.Both:
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll); break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
            }

            if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0))
                return;

            var delta = _canvas.InverseTransformDirection(ImGui.GetIO().MouseDelta);
            switch (direction)
            {
                case Direction.Horizontal:
                    delta.Y = 0;
                    break;
                case Direction.Vertical:
                    delta.X = 0;
                    break;
            }


            foreach (var ep in _selectedKeyframes)
            {
                //ep.PosOnCanvas += delta;
            }
        }

        // private IEnumerable<CurvePointUi> CurvePointsControls =>
        //     from selectedElement in _selectionHandler.SelectedElements
        //     let curvePoint = selectedElement as CurvePointUi
        //     where curvePoint != null
        //     select curvePoint;

        private ImRect GetBoundingBox()
        {
            var minU = float.PositiveInfinity;
            var minV = float.PositiveInfinity;
            var maxU = float.NegativeInfinity;
            var maxV = float.NegativeInfinity;

            if (_selectedKeyframes.Count > 1)
            {
                foreach (var selected in _selectedKeyframes)
                {
                    minU = Math.Min(minU, (float)selected.U);
                    maxU = Math.Max(maxU, (float)selected.U);
                    minV = Math.Min(minV, (float)selected.Value);
                    maxV = Math.Max(maxV, (float)selected.Value);
                }
                return new ImRect(minU, minV, maxU, maxV);
            }

            if (_selectedKeyframes.Count == 1)
            {
                var vDef = _selectedKeyframes.First();
                minU = maxU = (float)vDef.U;
                minV = maxV = (float)vDef.Value;

                return new ImRect(minU, minV, maxU, maxV);
            }
            return new ImRect(100, 100, 200, 200);
        }
        
        private ImRect _bounds;
        //private readonly SelectionHandler _selectionHandler;
        private readonly ICanvas _canvas;


        // Styling
        private const float DragHandleSize = 10;
        private static readonly Vector2 VerticalHandleOffset = new Vector2(0, DragHandleSize);
        private static readonly Vector2 HorizontalHandleOffset = new Vector2(DragHandleSize, 0);
        // private static Vector2 _handleOffset = new Vector2(DragHandleSize, DragHandleSize);

        private static readonly Color SelectBoxBorderColor = new Color(1, 1, 1, 0.2f);
        private static readonly Color SelectBoxBorderFill = new Color(1, 1, 1, 0.05f);
    }
}


