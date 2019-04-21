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
                        var instance = _canvas.CompositionOp.Children.Find(c => c.Symbol == childUi.SymbolChild.Symbol);
                        _canvas.CompositionOp = instance;
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
                DrawOutputSlot(symbolChildUi, slot_idx);
            }

            for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.InputDefinitions.Count; slot_idx++)
            {
                DrawInputSlot(symbolChildUi, slot_idx);
            }
        }


        private static void DrawOutputSlot(SymbolChildUi ui, int outputIndex)
        {
            var outputDef = ui.SymbolChild.Symbol.OutputDefinitions[outputIndex];

            var virtualRectInCanvas = GetOutputSlotSizeInCanvas(ui, outputIndex);

            var rInScreen = _canvas.ScreenRectFromCanvas(virtualRectInCanvas);

            ImGui.SetCursorScreenPos(rInScreen.Min);
            ImGui.InvisibleButton("output", rInScreen.GetSize());
            THelpers.DebugItemRect();
            var color = ColorForType(outputDef);

            if (DraftConnection.IsCurrentSourceOutput(ui, outputIndex))
            {
                _canvas.DrawRectFilled(virtualRectInCanvas, ColorForType(outputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    DraftConnection.Update();
                }
            }
            else if (ImGui.IsItemHovered())
            {
                _canvas.DrawRectFilled(virtualRectInCanvas, Color.White);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Color(0.2f).Rgba);
                ImGui.BeginTooltip();
                ImGui.Text($"-> .{outputDef.Name}");
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();

                if (ImGui.IsItemClicked(0))
                {
                    DraftConnection.StartFromOutput(_canvas.CompositionOp.Symbol, ui, outputIndex);
                }
            }
            else
            {
                _canvas.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(ui.Position.X + virtualRectInCanvas.GetWidth() * outputIndex + 1 + 3, ui.Position.Y - 1),
                        new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                    , DraftConnection.IsMatchingOutput(outputDef) ? Color.White : color);
            }
        }

        public static ImRect GetOutputSlotSizeInCanvas(SymbolChildUi sourceUi, int outputIndex)
        {
            var outputCount = sourceUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var inputWidth = sourceUi.Size.X / outputCount;   // size count must be non-zero in this method

            return ImRect.RectWithSize(
                new Vector2(sourceUi.Position.X + inputWidth * outputIndex + 1, sourceUi.Position.Y - 3),
                new Vector2(inputWidth - 2, 6));
        }



        private static void DrawInputSlot(SymbolChildUi targetUi, int inputIndex)
        {
            var inputDef = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex];
            var virtualRectInCanvas = GetInputSlotSizeInCanvas(targetUi, inputIndex);
            var rInScreen = _canvas.ScreenRectFromCanvas(virtualRectInCanvas);
            ImGui.SetCursorScreenPos(rInScreen.Min);
            ImGui.InvisibleButton("input", rInScreen.GetSize());
            THelpers.DebugItemRect();

            var valueType = inputDef.DefaultValue.ValueType;
            var color = ColorForType(inputDef);

            var hovered = rInScreen.Contains(ImGui.GetMousePos()); // TODO: check why ImGui.IsItemHovered() is not working


            if (DraftConnection.IsCurrentTargetInput(targetUi, inputIndex))
            {
                _canvas.DrawRectFilled(virtualRectInCanvas, ColorForType(inputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    DraftConnection.Update();
                }
            }
            else if (hovered)
            {
                //Log.Debug("Is Mouse hovered " + targetUi.ReadableName);
                if (DraftConnection.IsMatchingInput(inputDef))
                {
                    _canvas.DrawRectFilled(virtualRectInCanvas, color);

                    if (ImGui.IsMouseReleased(0))
                    {
                        DraftConnection.CompleteAtInput(_canvas.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
                else
                {
                    _canvas.DrawRectFilled(virtualRectInCanvas, color);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked())
                    {
                        DraftConnection.StartFromInput(_canvas.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
            }
            else
            {
                _canvas.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(targetUi.Position.X + virtualRectInCanvas.GetWidth() * inputIndex + 1 + 3,
                                    targetUi.Position.Y + targetUi.Size.Y - SlotConfig.VisibleSlotHeight),
                        new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6,
                                    SlotConfig.VisibleSlotHeight))
                    , color: DraftConnection.IsMatchingInput(inputDef) ? Color.White : color);
            }
        }


        private static Color ColorForType(Symbol.InputDefinition inputDef)
        {
            return InputUiRegistry.EntriesByType[inputDef.DefaultValue.ValueType].Color;
        }

        private static Color ColorForType(Symbol.OutputDefinition outputDef)
        {
            return InputUiRegistry.EntriesByType[outputDef.ValueType].Color;
        }

        public static ImRect GetInputSlotSizeInCanvas(SymbolChildUi targetUi, int inputIndex)
        {
            var inputCount = targetUi.SymbolChild.Symbol.InputDefinitions.Count;
            var inputWidth = inputCount == 0 ? targetUi.Size.X
                : targetUi.Size.X / inputCount;

            return ImRect.RectWithSize(
                new Vector2(targetUi.Position.X + inputWidth * inputIndex + 1, targetUi.Position.Y + targetUi.Size.Y - 3),
                new Vector2(inputWidth - 2, 6));
        }



        static class SlotConfig
        {
            public const float Height = GraphCanvas.GridSize;
            public const float VirtuaoSlotHeight = 5;
            public const float VisibleSlotHeight = 3;
        }

        static private GraphCanvas _canvas = null;



        // if (_contextMenuOpened)
        // {
        //     ImGui.OpenPopup("context_menu");
        //     if (_hoveredListNodeIndex != -1)
        //         _selectedNodeID = _hoveredListNodeIndex;

        //     if (_hoveredSceneNodeIndex != -1)
        //         _selectedNodeID = _hoveredSceneNodeIndex;
        // }


        // Scrolling
        // if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemActive() && ImGui.IsMouseDragging(2, 0.0f))
        //     _scroll = _scroll + ImGui.GetIO().MouseDelta;

        // ImGui.PopItemWidth();
    }
}
