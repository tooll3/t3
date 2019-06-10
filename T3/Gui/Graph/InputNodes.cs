
using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="DraftConnection"/> 
    /// create new connections with it.
    /// </summary>
    static class InputNodes
    {
        public static void DrawAll()
        {
            var inputUisForSymbol = InputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var index = 0;
            foreach (var inputDef in GraphCanvas.Current.CompositionOp.Symbol.InputDefinitions)
            {
                var inputUi = inputUisForSymbol[inputDef.Id];
                Draw(inputDef, inputUi);
                index++;
            }
        }


        private static void Draw(Symbol.InputDefinition inputDef, IInputUi inputUi)
        {
            ImGui.PushID(inputDef.Id.GetHashCode());
            {
                var posInWindow = GraphCanvas.Current.ChildPosFromCanvas(inputUi.Position + new Vector2(0, 3));
                var posInApp = GraphCanvas.Current.TransformPosition(inputUi.Position);

                // Interaction
                ImGui.SetCursorPos(posInWindow);
                ImGui.InvisibleButton("node", (inputUi.Size - new Vector2(0, 6)) * GraphCanvas.Current.Scale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsItemClicked(0))
                    {
                        if (!GraphCanvas.Current.SelectionHandler.SelectedElements.Contains(inputUi))
                        {
                            GraphCanvas.Current.SelectionHandler.SetElement(inputUi);
                        }
                    }
                    if (ImGui.IsMouseDragging(0))
                    {
                        foreach (var e in GraphCanvas.Current.SelectionHandler.SelectedElements)
                        {
                            e.Position += ImGui.GetIO().MouseDelta;
                        }
                    }
                }


                // Rendering
                GraphCanvas.DrawList.ChannelsSplit(2);
                GraphCanvas.DrawList.ChannelsSetCurrent(1);

                GraphCanvas.DrawList.AddText(posInApp, Color.White, String.Format($"{inputDef.Name}"));
                GraphCanvas.DrawList.ChannelsSetCurrent(0);

                //THelpers.OutlinedRect(ref Canvas.DrawList, posInApp, inputUi.Size * Canvas.Current._scale,
                //    fill: new Color(
                //            ((inputUi.IsSelected || ImGui.IsItemHovered()) ? 0.3f : 0.2f)),
                //    outline: inputUi.IsSelected ? Color.White : Color.Black);


                // Draw slot 
                {
                    var virtualRectInCanvas = ImRect.RectWithSize(
                        new Vector2(inputUi.Position.X + 1, inputUi.Position.Y - 3),
                        new Vector2(inputUi.Size.X - 2, 6));

                    var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);

                    ImGui.SetCursorScreenPos(rInScreen.Min);
                    ImGui.InvisibleButton("output", rInScreen.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                    if (DraftConnection.IsInputNodeCurrentConnectionSource(inputDef))
                    {
                        GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            DraftConnection.Update();
                        }
                    }
                    else if (ImGui.IsItemHovered())
                    {
                        if (DraftConnection.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                DraftConnection.CompleteAtSymbolInputNode(GraphCanvas.Current.CompositionOp.Symbol, inputDef);
                            }
                        }
                        else
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                DraftConnection.StartFromInputNode(inputDef);
                            }
                        }
                    }
                    else
                    {
                        GraphCanvas.Current.DrawRectFilled(
                            ImRect.RectWithSize(
                                new Vector2(inputUi.Position.X + 1 + 3, inputUi.Position.Y - 1),
                                new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                            , color);
                    }
                }

                GraphCanvas.DrawList.ChannelsMerge();
            }
            ImGui.PopID();
        }
    }
}
