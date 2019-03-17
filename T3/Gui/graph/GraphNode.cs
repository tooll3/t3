using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using t3.iuhelpers;

namespace t3.graph
{
    static class GraphNode
    {
        public static void DrawOnCanvas(Node node, GraphCanvas canvas)
        {
            ImGui.PushID(node.ID);
            canvas._drawList.ChannelsSplit(2);
            canvas._drawList.ChannelsSetCurrent((int)Channels.Foreground);

            var nodeTopLeft = canvas.GetChildPosFrom(node.Pos);

            ImGui.SetCursorPos(nodeTopLeft);
            ImGui.Text(String.Format($"{node.Name}"));

            canvas._drawList.ChannelsSetCurrent((int)Channels.Background);

            DrawSlots(node, canvas);

            ImGui.SetCursorPos(nodeTopLeft);
            ImGui.InvisibleButton("node", node.Size * canvas._scale);
            THelpers.OutlinedRect(ref canvas._drawList, nodeTopLeft + canvas._canvasPos, node.Size * canvas._scale,
                fill: new Color(node.IsSelected || ImGui.IsItemHovered() ? 0.3f : 0.2f),
                outline: node.IsSelected ? Color.White : Color.Black);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            if (ImGui.IsItemActive())
            {
                if (ImGui.IsMouseDragging(0))
                {
                    node.Pos = node.Pos + ImGui.GetIO().MouseDelta;
                }
                node.IsSelected = true;
            }

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
                //THelpers.DebugItemRect();

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


        // // Open context menu
        // if (!ImGui.IsAnyItemHovered() && ImGui.IsWindowHovered() && ImGui.IsMouseClicked(1))
        // {
        //     _selectedNodeID = _hoveredListNodeIndex = _hoveredSceneNodeIndex = -1;
        //     _contextMenuOpened = true;
        // }

        // if (_contextMenuOpened)
        // {
        //     ImGui.OpenPopup("context_menu");
        //     if (_hoveredListNodeIndex != -1)
        //         _selectedNodeID = _hoveredListNodeIndex;

        //     if (_hoveredSceneNodeIndex != -1)
        //         _selectedNodeID = _hoveredSceneNodeIndex;
        // }

        // // Draw context menu
        // ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
        // if (ImGui.BeginPopup("context_menu"))
        // {
        //     Vector2 scene_pos = ImGui.GetMousePosOnOpeningCurrentPopup() - scrollOffset;
        //     var isANodeSelected = _selectedNodeID != -1;
        //     if (isANodeSelected)
        //     {
        //         var node = _nodes[_selectedNodeID];
        //         ImGui.Text("Node '{node.Name}'");
        //         ImGui.Separator();
        //         if (ImGui.MenuItem("Rename..", null, false, false)) { }
        //         if (ImGui.MenuItem("Delete", null, false, false)) { }
        //         if (ImGui.MenuItem("Copy", null, false, false)) { }
        //     }
        //     else
        //     {
        //         if (ImGui.MenuItem("Add")) { _nodes.Add(new Node(_nodes.Count, "New node", scene_pos, 0.5f, new Vector4(0.5f, 0.5f, 0.5f, 1), 2, 2)); }
        //         if (ImGui.MenuItem("Paste", null, false, false)) { }
        //     }
        //     ImGui.EndPopup();
        // }
        // ImGui.PopStyleVar();

        // Scrolling
        // if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemActive() && ImGui.IsMouseDragging(2, 0.0f))
        //     _scroll = _scroll + ImGui.GetIO().MouseDelta;

        // ImGui.PopItemWidth();
    }
}
