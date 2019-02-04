using System;
using System.Numerics;
using ImGuiNET;
using imHelpers;
using t3.iuhelpers;

namespace t3.graph
{
    static class GraphNode
    {
        public static void DrawOnCanvas(Node node, GraphCanvas canvas)
        {
            canvas._drawList.ChannelsSplit(2);
            canvas._drawList.ChannelsSetCurrent(1); // Foreground
            var NODE_WINDOW_PADDING = new Vector2(10.0f, 2.0f);

            ImGui.PushID(node.ID);

            var minPos = canvas.GetChildPosFrom(node.Pos);

            ImGui.SetCursorPos(minPos);
            ImGui.Text(String.Format($"{node.Name}"));

            canvas._drawList.ChannelsSetCurrent(0); // Background

            DrawSlots(node, canvas);

            ImGui.SetCursorPos(minPos);


            ImGui.InvisibleButton("node", node.Size * canvas._scale);


            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(0))
                {
                    node.Pos = node.Pos + ImGui.GetIO().MouseDelta;
                }
                // node.IsSelected = true;
            }

            THelpers.OutlinedRect(ref canvas._drawList, minPos + canvas._canvasPos, node.Size * canvas._scale,
                background: node.IsSelected ? new Color(0.3f).ToUint() : new Color(0.2f).ToUint(),
                outline: node.IsSelected ? Color.Blue.ToUint() : Color.Black.ToUint());


            ImGui.PopID();
            canvas._drawList.ChannelsMerge();
        }


        private static void DrawSlots(Node node, GraphCanvas canvas)
        {
            const float NODE_SLOT_RADIUS = 4.0f;

            for (int slot_idx = 0; slot_idx < node.InputsCount; slot_idx++)
            {
                var pOnCanvas = node.GetInputSlotPos(slot_idx);
                var itemPos = canvas.GetChildPosFrom(pOnCanvas);
                ImGui.SetCursorPos(itemPos);
                ImGui.InvisibleButton("input" + slot_idx, new Vector2(10, 10));
                THelpers.DebugItemRect();

                if (ImGui.IsItemHovered() && ImGui.IsItemClicked(0))
                {
                    canvas.StartLinkFromInput(node, slot_idx);
                }

                if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(0))
                {
                    canvas.CompleteLinkToInput(node, slot_idx);
                }


                var col = ImGui.IsItemHovered() ? TColors.ToUint(150, 150, 150, 150) : Color.Red.ToUint();
                canvas._drawList.AddCircleFilled(canvas.GetScreenPosFrom(pOnCanvas), NODE_SLOT_RADIUS, col);
            }

            for (int slot_idx = 0; slot_idx < node.OutputsCount; slot_idx++)
            {
                var pOnCanvas = node.GetOutputSlotPos(slot_idx);
                var itemPos = canvas.GetChildPosFrom(pOnCanvas);
                ImGui.SetCursorPos(itemPos);
                ImGui.InvisibleButton("input" + slot_idx, new Vector2(10, 10));
                THelpers.DebugItemRect();

                if (ImGui.IsItemHovered() && ImGui.IsItemClicked(0))
                {
                    canvas.StartLinkFromOutput(node, slot_idx);
                }

                if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(0))
                {
                    canvas.CompleteLinkToOutput(node, slot_idx);
                }

                var col = ImGui.IsItemHovered() ? TColors.ToUint(150, 150, 150, 150) : Color.Red.ToUint();
                canvas._drawList.AddCircleFilled(canvas.GetScreenPosFrom(pOnCanvas), NODE_SLOT_RADIUS, col);
            }
        }
    }
}
