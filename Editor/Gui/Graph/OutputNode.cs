using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph;

/// <summary>
/// Draws published output parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
/// to create new connections with it.
/// </summary>
static class OutputNode
{
    public static void Draw(GraphWindow window, ImDrawListPtr drawList, Symbol.OutputDefinition outputDef, IOutputUi outputUi)
    {
        var canvas = window.GraphCanvas;
        ImGui.PushID(outputDef.Id.GetHashCode());
        {
            LastScreenRect = canvas.TransformRect(new ImRect(outputUi.PosOnCanvas, outputUi.PosOnCanvas + outputUi.Size));
            LastScreenRect.Floor();

            // Interaction
            ImGui.SetCursorScreenPos(LastScreenRect.Min);
            ImGui.InvisibleButton("node", LastScreenRect.GetSize());

            DrawUtils.DebugItemRect();
            var hovered = ImGui.IsItemHovered();
            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            canvas.SelectableNodeMovement.Handle(outputUi);

            // Rendering
            var typeColor = TypeUiRegistry.GetPropertiesForType(outputDef.ValueType).Color;
            // Draw output indicator
            {
                var backgroundColor = hovered
                                          ? ColorVariations.OperatorBackgroundHover.Apply(typeColor)
                                          : ColorVariations.OutputNodes.Apply(typeColor);
                    
                var halfSize = LastScreenRect.GetSize() * 0.5f;
                var paddingX = MathF.Floor( LastScreenRect.GetSize().X * 0.1f);
                var indicatorColor = ColorVariations.ConnectionLines.Apply(typeColor).Fade(0.4f);
                    
                drawList.AddRectFilled(LastScreenRect.Min, LastScreenRect.Max, backgroundColor.Fade(0.4f));
                    
                drawList.AddRectFilled(new Vector2(LastScreenRect.Min.X, LastScreenRect.Min.Y),
                                       new Vector2(LastScreenRect.Max.X - paddingX, LastScreenRect.Max.Y),
                                       indicatorColor);

                drawList.AddTriangleFilled(new Vector2(LastScreenRect.Max.X -paddingX, LastScreenRect.Min.Y),
                                           new Vector2(LastScreenRect.Max.X, LastScreenRect.Min.Y + halfSize.Y),
                                           new Vector2(LastScreenRect.Max.X-paddingX, LastScreenRect.Max.Y),
                                           indicatorColor);
            }
                
            // Label
            if(!string.IsNullOrEmpty(outputDef.Name))
            {
                var size = ImGui.CalcTextSize(outputDef.Name);
                var yPos = LastScreenRect.GetCenter().Y - size.Y / 2;
                    
                var isScaledDown = canvas.Scale.X < 1;
                drawList.PushClipRect(LastScreenRect.Min, LastScreenRect.Max, true);
                ImGui.PushFont(isScaledDown ? Fonts.FontSmall : Fonts.FontBold);

                var labelPos = new Vector2(
                                           LastScreenRect.Min.X +4,
                                           yPos);
                var label = outputDef.Name;
                drawList.AddText(labelPos, ColorVariations.OperatorLabel.Apply(typeColor), label);
                    
                ImGui.PopFont();
                drawList.PopClipRect();
            }

            if (canvas.NodeSelection.IsNodeSelected(outputUi))
            {
                drawList.AddRect(LastScreenRect.Min - Vector2.One, LastScreenRect.Max + Vector2.One, UiColors.Selection, 1);
            }

            // Draw slot 
            {
                var usableSlotArea = new ImRect(
                                                new Vector2(LastScreenRect.Min.X - GraphNode.UsableSlotThickness,
                                                            LastScreenRect.Min.Y),
                                                new Vector2(LastScreenRect.Min.X,
                                                            LastScreenRect.Max.Y)); 
                    
                ConnectionSnapEndHelper.RegisterAsPotentialTarget(window, outputUi, usableSlotArea);
                    
                    
                ImGui.SetCursorScreenPos(usableSlotArea.Min);
                ImGui.InvisibleButton("output", usableSlotArea.GetSize());
                DrawUtils.DebugItemRect();
                var color = TypeUiRegistry.GetPropertiesForType(outputDef.ValueType).Color;

                //Note: isItemHovered will not work
                var slotHovered = ConnectionMaker.GetTempConnectionsFor(window).Count > 0
                                      ? usableSlotArea.Contains(ImGui.GetMousePos())
                                      : ImGui.IsItemHovered();

                if (ConnectionMaker.IsOutputNodeCurrentConnectionTarget(window, outputDef))
                {
                    drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);
                }
                else if (ConnectionSnapEndHelper.IsNextBestTarget(outputUi) || slotHovered)
                {
                    if (ConnectionMaker.IsMatchingInputType(window, outputDef.ValueType))
                    {
                        drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);

                        if (ImGui.IsMouseReleased(0))
                        {
                            ConnectionMaker.CompleteAtSymbolOutputNode(window, window.CompositionOp.Symbol, outputDef);
                        }
                    }
                    else
                    {
                        drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, UiColors.Selection);
                        if (ImGui.IsItemClicked(0))
                        {
                            ConnectionMaker.StartFromOutputNode(window, window.CompositionOp.Symbol, outputDef);
                        }
                    }
                }
                else
                {
                    var colorWithMatching = ConnectionMaker.IsMatchingInputType(window, outputDef.ValueType) ? UiColors.Selection : color;
                    drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - GraphNode.InputSlotMargin- GraphNode.InputSlotThickness,
                                                       usableSlotArea.Min.Y), 
                                           new Vector2(
                                                       usableSlotArea.Max.X - GraphNode.InputSlotMargin , 
                                                       usableSlotArea.Max.Y), 
                                           colorWithMatching);
                }
            }
        }
        ImGui.PopID();
    }

    internal static ImRect LastScreenRect;
}