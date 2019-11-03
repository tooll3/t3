using ImGuiNET;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;
using T3.Gui.TypeColors;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
    /// create new connections with it.
    /// </summary>
    static class OutputNode
    {
        public static void Draw(Symbol.OutputDefinition outputDef, IOutputUi outputUi)
        {
            ImGui.PushID(outputDef.Id.GetHashCode());
            {
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(outputUi.PosOnCanvas, outputUi.PosOnCanvas + outputUi.Size));
                _lastScreenRect.Floor();

                // Interaction
                ImGui.SetCursorScreenPos(_lastScreenRect.Min);
                ImGui.InvisibleButton("node", _lastScreenRect.GetSize());

                THelpers.DebugItemRect();
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                SelectableMovement.Handle(outputUi);

                // Rendering
                var typeColor = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                var drawList = GraphCanvas.Current.DrawList;
                drawList.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.OutputNodes.Apply(typeColor));

                drawList.AddRectFilled(new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                                 new Vector2(_lastScreenRect.Max.X,
                                             _lastScreenRect.Max.Y + GraphNode._inputSlotThickness + GraphNode._inputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));

                var label = string.Format($"{outputDef.Name}");

                drawList.AddText(_lastScreenRect.Min, ColorVariations.OperatorLabel.Apply(typeColor), label);

                if (outputUi.IsSelected)
                {
                    drawList.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }

                // Draw slot 
                {
                    var usableSlotArea = new ImRect(
                                                    new Vector2(_lastScreenRect.Min.X - GraphNode._usableSlotThickness,
                                                                _lastScreenRect.Min.Y),
                                                    new Vector2(_lastScreenRect.Min.X,
                                                                _lastScreenRect.Max.Y)); 
                    

                    ImGui.SetCursorScreenPos(usableSlotArea.Min);
                    ImGui.InvisibleButton("output", usableSlotArea.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                    //Note: isItemHovered will not work
                    var slotHovered = ConnectionMaker.TempConnection != null
                                          ? usableSlotArea.Contains(ImGui.GetMousePos())
                                          : ImGui.IsItemHovered();

                    if (ConnectionMaker.IsOutputNodeCurrentConnectionTarget(outputDef))
                    {
                        drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            ConnectionMaker.Update();
                        }
                    }
                    else if (slotHovered)
                    {
                        if (ConnectionMaker.IsMatchingInputType(outputDef.ValueType))
                        {
                            drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                ConnectionMaker.CompleteAtSymbolOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                        else
                        {
                            drawList.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                ConnectionMaker.StartFromOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                    }
                    else
                    {
                        var colorWithMatching = ConnectionMaker.IsMatchingInputType(outputDef.ValueType) ? Color.White : color;
                        drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - GraphNode._inputSlotMargin- GraphNode._inputSlotThickness,
                                                           usableSlotArea.Min.Y), 
                                               new Vector2(
                                                           usableSlotArea.Max.X - GraphNode._inputSlotMargin , 
                                                           usableSlotArea.Max.Y), 
                                               colorWithMatching);
                    }
                }
            }
            ImGui.PopID();
        }

        internal static ImRect _lastScreenRect;
    }
}