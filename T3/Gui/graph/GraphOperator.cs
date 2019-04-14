using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui;
using T3.Logging;
using T3.UiHelpers;
namespace T3.graph
{
    static class GraphOperator
    {
        /// <summary>
        /// This does something 
        /// </summary>
        public static void DrawOnCanvas(SymbolChildUi childUi, GraphCanvasWindow canvas)
        {
            ImGui.PushID(childUi.SymbolChild.InstanceId.ToString());
            {
                var posInWindow = canvas.ChildPosFromCanvas(childUi.Position);
                var posInApp = canvas.ScreenPosFromCanvas(childUi.Position);

                // Interaction
                ImGui.SetCursorPos(posInWindow);
                ImGui.InvisibleButton("node", childUi.Size * canvas._scale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemHovered())
                    T3UI.AddHoveredId(childUi.SymbolChild.InstanceId);

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsMouseDragging(0))
                    {
                        if (!canvas.SelectionHandler.SelectedElements.Contains(childUi))
                        {
                            canvas.SelectionHandler.SetElement(childUi);
                        }

                        foreach (var e in canvas.SelectionHandler.SelectedElements)
                        {
                            e.Position += ImGui.GetIO().MouseDelta;
                        }
                        Log.Debug($"Draw op {childUi.ReadableName}" + childUi.Position);
                    }
                    else
                    {
                        Log.Debug("Active but not clicked " + childUi.ReadableName, childUi.SymbolChild.InstanceId);
                        canvas.SelectionHandler.SetElement(childUi);
                    }
                    ImGui.Dummy(Vector2.Zero);
                    if (ImGui.IsItemClicked(0))
                    {
                        canvas.SelectionHandler.SetElement(childUi);
                    }
                    childUi.IsSelected = true;
                }

                // Rendering
                canvas._drawList.ChannelsSplit(2);
                canvas._drawList.ChannelsSetCurrent(1);
                canvas._drawList.AddText(posInApp, Color.White.UInt, String.Format($"{childUi.ReadableName}"));

                canvas._drawList.ChannelsSetCurrent(0);

                var hoveredFactor = T3UI.HoveredIdsLastFrame.Contains(childUi.SymbolChild.InstanceId) ? 1.2f : 0.8f;
                THelpers.OutlinedRect(ref canvas._drawList, posInApp, childUi.Size * canvas._scale,
                    fill: new Color((childUi.IsSelected || ImGui.IsItemHovered() ? 0.3f : 0.2f) * hoveredFactor),
                    outline: childUi.IsSelected ? Color.White : Color.Black);

                canvas._drawList.ChannelsMerge();
            }
            ImGui.PopID();
        }


        //private static void DrawSlots(Node node, GraphCanvasWindow canvas)
        //{
        //const float NODE_SLOT_RADIUS = 4.0f;

        //for (int slot_idx = 0; slot_idx < node.InputsCount; slot_idx++)
        //{
        //    var pOnCanvas = node.GetInputSlotPos(slot_idx);
        //    var itemPos = canvas.GetChildPosFrom(pOnCanvas);
        //    ImGui.SetCursorPos(itemPos);
        //    ImGui.InvisibleButton("input" + slot_idx, new Vector2(10, 10));

        //    if (ImGui.IsItemHovered() && ImGui.IsItemClicked(0))
        //    {
        //        canvas.StartLinkFromInput(node, slot_idx);
        //    }

        //    if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(0))
        //    {
        //        canvas.CompleteLinkToInput(node, slot_idx);
        //    }

        //    var col = ImGui.IsItemHovered() ? TColors.ToUint(150, 150, 150, 150) : Color.Red.ToUint();
        //    canvas._drawList.AddCircleFilled(canvas.GetScreenPosFrom(pOnCanvas), NODE_SLOT_RADIUS, col);
        //}

        //for (int slot_idx = 0; slot_idx < node.OutputsCount; slot_idx++)
        //{
        //    var pOnCanvas = node.GetOutputSlotPos(slot_idx);
        //    var itemPos = canvas.GetChildPosFrom(pOnCanvas);
        //    ImGui.SetCursorPos(itemPos);
        //    ImGui.InvisibleButton("input" + slot_idx, new Vector2(10, 10));
        //    //THelpers.DebugItemRect();

        //    if (ImGui.IsItemHovered() && ImGui.IsItemClicked(0))
        //    {
        //        canvas.StartLinkFromOutput(node, slot_idx);
        //    }

        //    if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(0))
        //    {
        //        canvas.CompleteLinkToOutput(node, slot_idx);
        //    }

        //    var col = ImGui.IsItemHovered() ? TColors.ToUint(150, 150, 150, 150) : Color.Red.ToUint();
        //    canvas._drawList.AddCircleFilled(canvas.GetScreenPosFrom(pOnCanvas), NODE_SLOT_RADIUS, col);
        //}
        //}


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
