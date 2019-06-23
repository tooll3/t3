using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="DraftConnection"/> 
    /// create new connections with it.
    /// </summary>
    static class OutputNodes
    {
        public static void DrawAll()
        {
            var outputUisForSymbol = OutputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var index = 0;
            foreach (var outputDef in GraphCanvas.Current.CompositionOp.Symbol.OutputDefinitions)
            {
                var outputUi = outputUisForSymbol[outputDef.Id];
                Draw(outputDef, outputUi);
                index++;
            }
        }


        private static void Draw(Symbol.OutputDefinition outputDef, IOutputUi outputUi)
        {
            ImGui.PushID(outputDef.Id.GetHashCode());
            {
                var posInWindow = GraphCanvas.Current.ChildPosFromCanvas(outputUi.PosOnCanvas + new Vector2(0, 3));
                var posInApp = GraphCanvas.Current.TransformPosition(outputUi.PosOnCanvas);

                // Interaction
                ImGui.SetCursorPos(posInWindow);
                ImGui.InvisibleButton("node", (outputUi.Size - new Vector2(0, 6)) * GraphCanvas.Current.Scale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsItemClicked(0))
                    {
                        if (!GraphCanvas.Current.SelectionHandler.SelectedElements.Contains(outputUi))
                        {
                            GraphCanvas.Current.SelectionHandler.SetElement(outputUi);
                        }
                    }

                    if (ImGui.IsMouseDragging(0))
                    {
                        foreach (var e in GraphCanvas.Current.SelectionHandler.SelectedElements)
                        {
                            e.PosOnCanvas += ImGui.GetIO().MouseDelta;
                        }
                    }
                }


                // Rendering
                var dl = ImGui.GetWindowDrawList();
                dl.ChannelsSplit(2);
                dl.ChannelsSetCurrent(1);

                dl.AddText(posInApp, Color.White, String.Format($"{outputDef.Name}"));
                dl.ChannelsSetCurrent(0);

                // Draw slot 
                {
                    var virtualRectInCanvas = ImRect.RectWithSize(
                        new Vector2(outputUi.PosOnCanvas.X + 1, outputUi.PosOnCanvas.Y + outputUi.Size.Y),
                        new Vector2(outputUi.Size.X - 2, 6));

                    var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);

                    ImGui.SetCursorScreenPos(rInScreen.Min);
                    ImGui.InvisibleButton("output", rInScreen.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                    //Note: isItemHovered will not work
                    var hovered = DraftConnection.TempConnection != null ? rInScreen.Contains(ImGui.GetMousePos())
                        : ImGui.IsItemHovered();

                    if (DraftConnection.IsOutputNodeCurrentConnectionTarget(outputDef))
                    {
                        Log.Debug("current connection target");
                        GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            DraftConnection.Update();
                        }
                    }
                    //else if (ImGui.IsItemHovered())   // ToDo: Find out, why IsItemHovered is not working during drag
                    else if (hovered)
                    {
                        if (DraftConnection.IsMatchingInputType(outputDef.ValueType))
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                DraftConnection.CompleteAtSymbolOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                        else
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                DraftConnection.StartFromOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                    }
                    else
                    {
                        GraphCanvas.Current.DrawRectFilled(
                            ImRect.RectWithSize(
                                new Vector2(outputUi.PosOnCanvas.X + 1 + 3, outputUi.PosOnCanvas.Y + outputUi.Size.Y - 1),
                                new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                            , DraftConnection.IsMatchingInputType(outputDef.ValueType) ? Color.White : color);
                    }
                }

                dl.ChannelsMerge();
            }
            ImGui.PopID();
        }
    }
}
