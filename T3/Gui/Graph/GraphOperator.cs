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
        public static void DrawOnCanvas(SymbolChildUi childUi, GraphCanvas canvas)
        {
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                var posInWindow = canvas.ChildPosFromCanvas(childUi.Position + new Vector2(0, 3));
                var posInApp = canvas.ScreenPosFromCanvas(childUi.Position);
                _canvas = canvas;

                // Interaction
                ImGui.SetCursorPos(posInWindow);
                ImGui.InvisibleButton("node", (childUi.Size - new Vector2(0, 6)) * canvas._scale);
                THelpers.DebugItemRect();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemHovered())
                    T3UI.AddHoveredId(childUi.SymbolChild.Id);

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
                }


                // Rendering
                canvas._drawList.ChannelsSplit(2);
                canvas._drawList.ChannelsSetCurrent(1);

                canvas._drawList.AddText(posInApp, Color.White, String.Format($"{childUi.ReadableName}"));
                canvas._drawList.ChannelsSetCurrent(0);

                var hoveredFactor = T3UI.HoveredIdsLastFrame.Contains(childUi.SymbolChild.Id) ? 1.2f : 0.8f;
                THelpers.OutlinedRect(ref canvas._drawList, posInApp, childUi.Size * canvas._scale,
                    fill: new Color(
                            ((childUi.IsSelected || ImGui.IsItemHovered()) ? 0.3f : 0.2f) * hoveredFactor),
                    outline: childUi.IsSelected ? Color.White : Color.Black);

                DrawSlots(childUi);

                canvas._drawList.ChannelsMerge();
            }
            ImGui.PopID();
        }


        private static void DrawSlots(SymbolChildUi symbolChildUi)
        {
            for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.OutputDefinitions.Count; slot_idx++)
            {
                DrawOutputSlot(symbolChildUi, slot_idx);
            }

            for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.InputDefinitions.Count; slot_idx++)
            {
                DrawInputSlot(symbolChildUi, slot_idx);
            }

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
        }


        //static private SymbolChildUi _childUi;

        private static void DrawOutputSlot(SymbolChildUi ui, int outputIndex)
        {
            var outputCount = ui.SymbolChild.Symbol.OutputDefinitions.Count;
            var outputDef = ui.SymbolChild.Symbol.OutputDefinitions[outputIndex];
            var width = ui.Size.X / outputCount;   // size count must be non-zero in this method

            var mouseRectInCanvas = ImRect.RectWithSize(
                    new Vector2(ui.Position.X + width * outputIndex + 1, ui.Position.Y - 3),
                    new Vector2(width - 2, 6));

            var rInScreen = _canvas.ScreenRectFromCanvas(mouseRectInCanvas);

            ImGui.SetCursorScreenPos(rInScreen.Min);
            ImGui.InvisibleButton("output", rInScreen.GetSize());
            THelpers.DebugItemRect();


            if (DraftConnection.IsDraftConnectionSource(ui, outputIndex))
            {
                _canvas.DrawRectFilled(mouseRectInCanvas, ColorForType(outputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    DraftConnection.Update();
                }

                if (ImGui.IsMouseReleased(0))
                {
                    DraftConnection.Cancel();
                }
            }
            else if (ImGui.IsItemHovered())
            {
                _canvas.DrawRectFilled(mouseRectInCanvas, Color.White);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Color(0.2f).Rgba);
                ImGui.BeginTooltip();
                ImGui.Text($"-> .{outputDef.Name}");
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();

                if (ImGui.IsItemClicked(0))
                {
                    DraftConnection.StartFromOutput(ui, outputIndex);
                }
            }
            else
            {
                _canvas.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(ui.Position.X + width * outputIndex + 1 + 3, ui.Position.Y - 1),
                        new Vector2(width - 2 - 6, 3))
                    , Color.Gray);
            }
        }

        private static Color ColorForType(Symbol.InputDefinition inputDef)
        {
            return InputUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        private static Color ColorForType(Symbol.OutputDefinition outputDef)
        {
            return InputUiRegistry.Entries[outputDef.ValueType].Color;
        }


        private static void DrawInputSlot(SymbolChildUi ui, int inputIndex)
        {
            var inputCount = ui.SymbolChild.Symbol.InputDefinitions.Count;
            var inputDef = ui.SymbolChild.Symbol.InputDefinitions[inputIndex];
            var inputWidth = inputCount == 0 ? ui.Size.X
                : ui.Size.X / inputCount;

            var mouseRectInCanvas = ImRect.RectWithSize(
                new Vector2(ui.Position.X + inputWidth * inputIndex + 1, ui.Position.Y + ui.Size.Y - 3),
                new Vector2(inputWidth - 2, 6));

            var rInScreen = _canvas.ScreenRectFromCanvas(mouseRectInCanvas);

            var valueType = inputDef.DefaultValue.ValueType;
            var color = InputUiRegistry.Entries[valueType].Color;

            var hovered = rInScreen.Contains(ImGui.GetMousePos());

            if (hovered)
            {
                if (DraftConnection.IsMatchingInput(inputDef))
                {
                    _canvas.DrawRectFilled(mouseRectInCanvas, color);
                    if (ImGui.IsMouseReleased(0))
                    {
                        DraftConnection.CompleteToInput(_canvas.CompositionOp.Symbol, ui, inputIndex);
                    }
                }
                else
                {
                    _canvas.DrawRectFilled(mouseRectInCanvas, color);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                }
            }
            else
            {
                _canvas.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(ui.Position.X + inputWidth * inputIndex + 1 + 3,
                                    ui.Position.Y + ui.Size.Y - SlotConfig.VisibleSlotHeight),
                        new Vector2(inputWidth - 2 - 6,
                                    SlotConfig.VisibleSlotHeight))
                    , color: DraftConnection.IsMatchingInput(inputDef) ? Color.White : color);
            }
        }


        static class SlotConfig
        {
            public const float Height = GraphCanvas.GridSize;
            public const float VirtuaoSlotHeight = 5;
            public const float VisibleSlotHeight = 3;
        }

        static private GraphCanvas _canvas = null;

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
