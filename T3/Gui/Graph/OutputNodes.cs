using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Logging;

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
            var outputUisForSymbol = OutputUiRegistry.Entries[Canvas.Current.CompositionOp.Symbol.Id];
            var index = 0;
            foreach (var outputDef in Canvas.Current.CompositionOp.Symbol.OutputDefinitions)
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
                var posInWindow = Canvas.ChildPosFromCanvas(outputUi.Position + new Vector2(0, 3));
                var posInApp = Canvas.TransformPosition(outputUi.Position);

                // Interaction
                ImGui.SetCursorPos(posInWindow);
                ImGui.InvisibleButton("node", (outputUi.Size - new Vector2(0, 6)) * Canvas.Current._scale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsItemClicked(0))
                    {
                        if (!Canvas.Current.SelectionHandler.SelectedElements.Contains(outputUi))
                        {
                            Canvas.Current.SelectionHandler.SetElement(outputUi);
                        }
                    }
                    if (ImGui.IsMouseDragging(0))
                    {
                        foreach (var e in Canvas.Current.SelectionHandler.SelectedElements)
                        {
                            e.Position += ImGui.GetIO().MouseDelta;
                        }
                    }
                }


                // Rendering
                Canvas.DrawList.ChannelsSplit(2);
                Canvas.DrawList.ChannelsSetCurrent(1);

                Canvas.DrawList.AddText(posInApp, Color.White, String.Format($"{outputDef.Name}"));
                Canvas.DrawList.ChannelsSetCurrent(0);

                // Draw slot 
                {
                    var virtualRectInCanvas = ImRect.RectWithSize(
                        new Vector2(outputUi.Position.X + 1, outputUi.Position.Y + outputUi.Size.Y),
                        new Vector2(outputUi.Size.X - 2, 6));

                    var rInScreen = Canvas.TransformRect(virtualRectInCanvas);

                    ImGui.SetCursorScreenPos(rInScreen.Min);
                    ImGui.InvisibleButton("output", rInScreen.GetSize());
                    THelpers.DebugItemRect();
                    var color = InputUiRegistry.EntriesByType[outputDef.ValueType].Color;

                    if (DraftConnection.IsOutputNodeCurrentConnectionTarget(outputDef))
                    {
                        Log.Debug("current connection target");
                        Canvas.DrawRectFilled(virtualRectInCanvas, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            DraftConnection.Update();
                        }
                    }
                    //else if (ImGui.IsItemHovered())   // ToDo: Find out, why IsItemHovered is not working during drag
                    else if (rInScreen.Contains(ImGui.GetMousePos()))
                    {
                        Log.Debug("hovered ");
                        if (DraftConnection.IsMatchingInputType(outputDef.ValueType))
                        {
                            Log.Debug("is matching inputtype");
                            Canvas.DrawRectFilled(virtualRectInCanvas, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                DraftConnection.CompleteAtSymbolOutputNode(Canvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                        else
                        {
                            Canvas.DrawRectFilled(virtualRectInCanvas, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                DraftConnection.StartFromOutputNode(outputDef);
                            }
                        }
                    }
                    else
                    {
                        Canvas.DrawRectFilled(
                            ImRect.RectWithSize(
                                new Vector2(outputUi.Position.X + 1 + 3, outputUi.Position.Y + outputUi.Size.Y - 1),
                                new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                            , DraftConnection.IsMatchingInputType(outputDef.ValueType) ? Color.White : color);
                    }
                }

                Canvas.DrawList.ChannelsMerge();
            }
            ImGui.PopID();
        }
    }
}
