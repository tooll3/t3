using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders a graphic representation of a <see cref="SymbolChild"/> within the current <see cref="GraphCanvasWindow"/>
    /// </summary>
    static class GraphOperator
    {
        public static void Draw(SymbolChildUi childUi)
        {
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                var posInWindow = GraphCanvas.Current.ChildPosFromCanvas(childUi.PosOnCanvas + new Vector2(0, 3));
                var posInApp = GraphCanvas.Current.TransformPosition(childUi.PosOnCanvas);

                // Interaction
                ImGui.SetCursorPos(posInWindow);


                ImGui.InvisibleButton("node", (childUi.Size - new Vector2(0, 6)) * GraphCanvas.Current.Scale);


                THelpers.DebugItemRect();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    T3UI.AddHoveredId(childUi.SymbolChild.Id);
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsItemClicked(0))
                    {
                        if (!GraphCanvas.Current.SelectionHandler.SelectedElements.Contains(childUi))
                        {
                            GraphCanvas.Current.SelectionHandler.SetElement(childUi);
                        }
                    }
                    if (ImGui.IsMouseDragging(0))
                    {
                        foreach (var e in GraphCanvas.Current.SelectionHandler.SelectedElements)
                        {
                            e.PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        }
                    }
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        //Logging.Log.Debug("Doubble clickked");
                        var instance = GraphCanvas.Current.CompositionOp.Children.Find(c => c.Symbol == childUi.SymbolChild.Symbol);
                        GraphCanvas.Current.CompositionOp = instance;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    GraphWidgets.Draw(childUi);
                }

                // Rendering
                GraphCanvas.DrawList.ChannelsSplit(2);
                GraphCanvas.DrawList.ChannelsSetCurrent(1);

                GraphCanvas.DrawList.AddText(posInApp, Color.White, String.Format($"{childUi.ReadableName}"));
                GraphCanvas.DrawList.ChannelsSetCurrent(0);

                var hoveredFactor = T3UI.HoveredIdsLastFrame.Contains(childUi.SymbolChild.Id) ? 1.2f : 0.8f;
                THelpers.OutlinedRect(ref GraphCanvas.DrawList, posInApp, childUi.Size * GraphCanvas.Current.Scale,
                    fill: new Color(
                            ((childUi.IsSelected || ImGui.IsItemHovered()) ? 0.3f : 0.2f) * hoveredFactor),
                    outline: childUi.IsSelected ? Color.White : Color.Black);

                DrawSlots(childUi);

                GraphCanvas.DrawList.ChannelsMerge();
            }
            ImGui.PopID();


        }

        private static void DrawSlots(SymbolChildUi symbolChildUi)
        {
            for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.OutputDefinitions.Count; slot_idx++)
            {
                Slots.DrawOutputSlot(symbolChildUi, slot_idx);
            }

            for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.InputDefinitions.Count; slot_idx++)
            {
                Slots.DrawInputSlot(symbolChildUi, slot_idx);
            }
        }
    }
}
