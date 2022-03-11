using ImGuiNET;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
    /// create new connections with it.
    /// </summary>
    static class AnnotationElement
    {
        internal static void Draw(Annotation annotation)
        {
            ImGui.PushID(annotation.Id.GetHashCode());
            {
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(annotation.PosOnCanvas, annotation.PosOnCanvas + annotation.Size));
                _isVisible = ImGui.IsRectVisible(_lastScreenRect.Min, _lastScreenRect.Max);

                if (!_isVisible)
                    return;
                
                // Resize indicator
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                    ImGui.SetCursorScreenPos(_lastScreenRect.Max - new Vector2(10, 10));
                    ImGui.Button("##resize", new Vector2(10, 10));
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        var delta = GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        annotation.Size += delta;
                    }

                    ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                }                
                
                // Interaction
                ImGui.SetCursorScreenPos(_lastScreenRect.Min);
                ImGui.InvisibleButton("node", _lastScreenRect.GetSize());

                THelpers.DebugItemRect();
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                SelectableNodeMovement.Handle(annotation);

                // Rendering
                var typeColor = Color.Gray;

                var drawList = GraphCanvas.Current.DrawList;
                drawList.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                       hovered
                                           ? ColorVariations.OperatorHover.Apply(typeColor)
                                           : ColorVariations.ConnectionLines.Apply(typeColor));

                if (annotation.IsSelected)
                {
                    const float thickness = 1;
                    drawList.AddRect(_lastScreenRect.Min - Vector2.One * thickness,
                                     _lastScreenRect.Max + Vector2.One * thickness ,
                                     Color.White, 0f, 0, thickness);
                }

                
                
                // Label
                {
                    var isScaledDown = GraphCanvas.Current.Scale.X < 1;
                    ImGui.PushFont(isScaledDown ? Fonts.FontSmall : Fonts.FontBold);
                    
                    drawList.PushClipRect(_lastScreenRect.Min, _lastScreenRect.Max, true);
                    var labelPos = _lastScreenRect.Min + new Vector2(4, 4);
                    
                    drawList.AddText(labelPos,
                                     ColorVariations.OperatorLabel.Apply(typeColor),
                                     annotation.Title);
                    ImGui.PopFont();
                    drawList.PopClipRect();
                }
            }
            ImGui.PopID();
        }

        internal static ImRect _lastScreenRect;
        private static bool _isVisible;
    }
}