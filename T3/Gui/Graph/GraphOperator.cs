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
        public static void DrawOnCanvas(SymbolChildUi childUi, Canvas canvas)
        {
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                var posInWindow = canvas.ChildPosFromCanvas(childUi.Position + new Vector2(0, 3));
                var posInApp = Canvas.ScreenPosFromCanvas(childUi.Position);

                // Interaction
                ImGui.SetCursorPos(posInWindow);
                ImGui.InvisibleButton("node", (childUi.Size - new Vector2(0, 6)) * canvas._scale);
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
                        if (!canvas.SelectionHandler.SelectedElements.Contains(childUi))
                        {
                            canvas.SelectionHandler.SetElement(childUi);
                        }
                    }
                    if (ImGui.IsMouseDragging(0))
                    {
                        foreach (var e in canvas.SelectionHandler.SelectedElements)
                        {
                            e.Position += ImGui.GetIO().MouseDelta;
                        }
                    }
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        //Logging.Log.Debug("Doubble clickked");
                        var instance = Canvas.Current.CompositionOp.Children.Find(c => c.Symbol == childUi.SymbolChild.Symbol);
                        Canvas.Current.CompositionOp = instance;
                    }
                }


                // Rendering
                canvas.DrawList.ChannelsSplit(2);
                canvas.DrawList.ChannelsSetCurrent(1);

                canvas.DrawList.AddText(posInApp, Color.White, String.Format($"{childUi.ReadableName}"));
                canvas.DrawList.ChannelsSetCurrent(0);

                var hoveredFactor = T3UI.HoveredIdsLastFrame.Contains(childUi.SymbolChild.Id) ? 1.2f : 0.8f;
                THelpers.OutlinedRect(ref canvas.DrawList, posInApp, childUi.Size * canvas._scale,
                    fill: new Color(
                            ((childUi.IsSelected || ImGui.IsItemHovered()) ? 0.3f : 0.2f) * hoveredFactor),
                    outline: childUi.IsSelected ? Color.White : Color.Black);

                DrawSlots(childUi);

                canvas.DrawList.ChannelsMerge();
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
