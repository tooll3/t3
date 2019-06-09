
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
            var inputUisForSymbol = InputUiRegistry.Entries[Canvas.Current.CompositionOp.Symbol.Id];
            var index = 0;
            foreach (var inputDef in Canvas.Current.CompositionOp.Symbol.InputDefinitions)
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
                var posInWindow = Canvas.ChildPosFromCanvas(inputUi.Position + new Vector2(0, 3));
                var posInApp = Canvas.TransformPosition(inputUi.Position);

                // Interaction
                ImGui.SetCursorPos(posInWindow);
                ImGui.InvisibleButton("node", (inputUi.Size - new Vector2(0, 6)) * Canvas.Current._scale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsItemClicked(0))
                    {
                        if (!Canvas.Current.SelectionHandler.SelectedElements.Contains(inputUi))
                        {
                            Canvas.Current.SelectionHandler.SetElement(inputUi);
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

                Canvas.DrawList.AddText(posInApp, Color.White, String.Format($"{inputDef.Name}"));
                Canvas.DrawList.ChannelsSetCurrent(0);

                //THelpers.OutlinedRect(ref Canvas.DrawList, posInApp, inputUi.Size * Canvas.Current._scale,
                //    fill: new Color(
                //            ((inputUi.IsSelected || ImGui.IsItemHovered()) ? 0.3f : 0.2f)),
                //    outline: inputUi.IsSelected ? Color.White : Color.Black);


                // Draw slot 
                {
                    var virtualRectInCanvas = ImRect.RectWithSize(
                        new Vector2(inputUi.Position.X + 1, inputUi.Position.Y - 3),
                        new Vector2(inputUi.Size.X - 2, 6));

                    var rInScreen = Canvas.TransformRect(virtualRectInCanvas);

                    ImGui.SetCursorScreenPos(rInScreen.Min);
                    ImGui.InvisibleButton("output", rInScreen.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                    if (DraftConnection.IsInputNodeCurrentConnectionSource(inputDef))
                    {
                        Canvas.DrawRectFilled(virtualRectInCanvas, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            DraftConnection.Update();
                        }
                    }
                    else if (ImGui.IsItemHovered())
                    {
                        if (DraftConnection.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                        {
                            Canvas.DrawRectFilled(virtualRectInCanvas, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                DraftConnection.CompleteAtSymbolInputNode(Canvas.Current.CompositionOp.Symbol, inputDef);
                            }
                        }
                        else
                        {
                            Canvas.DrawRectFilled(virtualRectInCanvas, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                DraftConnection.StartFromInputNode(inputDef);
                            }
                        }
                    }
                    else
                    {
                        Canvas.DrawRectFilled(
                            ImRect.RectWithSize(
                                new Vector2(inputUi.Position.X + 1 + 3, inputUi.Position.Y - 1),
                                new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                            , color);
                    }
                }

                Canvas.DrawList.ChannelsMerge();
            }
            ImGui.PopID();
        }
    }
}
