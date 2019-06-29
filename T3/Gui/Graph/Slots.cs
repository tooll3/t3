using ImGuiNET;
using imHelpers;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.TypeColors;

namespace T3.Gui.Graph
{
    public static class Slots
    {
        public static void DrawOutputSlot(SymbolChildUi ui, int outputIndex)
        {
            var outputDef = ui.SymbolChild.Symbol.OutputDefinitions[outputIndex];

            //var virtualRectInCanvas = GetOutputSlotSizeInCanvas(ui, outputIndex);

            //var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);
            var usableArea = GetUsableOutputSlotSize(ui, outputIndex);

            var dl = ImGui.GetWindowDrawList();

            ImGui.SetCursorScreenPos(usableArea.Min);
            ImGui.PushID(ui.SymbolChild.Id.GetHashCode());

            ImGui.InvisibleButton("output", usableArea.GetSize());
            THelpers.DebugItemRect();
            var valueType = outputDef.ValueType;
            var colorForType = TypeUiRegistry.Entries[valueType].Color;
            //= ColorForInputType(valueType);
            //var color = ColorForType(outputDef);

            //Note: isItemHovered will not work
            var hovered = BuildingConnections.TempConnection != null ? usableArea.Contains(ImGui.GetMousePos())
                : ImGui.IsItemHovered();


            if (BuildingConnections.IsOutputSlotCurrentConnectionSource(ui, outputIndex))
            {
                dl.AddRectFilled(usableArea.Min, usableArea.Max,
                    ColorVariations.Highlight.GetVariation(colorForType));
                //GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, ColorForType(outputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    BuildingConnections.Update();
                }
            }
            else if (hovered)
            {
                if (BuildingConnections.IsMatchingOutputType(outputDef.ValueType))
                {
                    dl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.GetVariation(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, ui, outputIndex);
                    }
                }
                else
                {
                    dl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.GetVariation(colorForType));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($".{outputDef.Name} ->");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        BuildingConnections.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, ui, outputIndex);
                    }
                    //ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    //ImGui.PushStyleColor(ImGuiCol.PopupBg, new Color(0.2f).Rgba);
                    //ImGui.BeginTooltip();
                    //ImGui.Text($"-> .{outputDef.Name}");
                    //ImGui.EndTooltip();
                    //ImGui.PopStyleColor();
                    //ImGui.PopStyleVar();
                }
            }
            else
            {
                //GraphCanvas.Current.DrawRectFilled(
                //    ImRect.RectWithSize(
                //        new Vector2(ui.PosOnCanvas.X + virtualRectInCanvas.GetWidth() * outputIndex + 1 + 3, ui.PosOnCanvas.Y - 1),
                //        new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                //    , BuildingConnections.IsMatchingOutputType(outputDef.ValueType) ? Color.White : color);

                var style = ColorVariations.Operator;
                if (BuildingConnections.TempConnection != null)
                {
                    style = BuildingConnections.IsMatchingOutputType(valueType)
                        ? ColorVariations.Highlight
                        : ColorVariations.Muted;
                }

                var pos = usableArea.Min + Vector2.UnitY * (usableArea.GetHeight() - GraphOperator._outputSlotMargin - GraphOperator._outputSlotHeight);
                var size = new Vector2(usableArea.GetWidth(), GraphOperator._outputSlotHeight);
                dl.AddRectFilled(
                    pos,
                    pos + size,
                    style.GetVariation(colorForType)
                    );
            }
            ImGui.PopID();
        }


        public static ImRect GetOutputSlotSizeInCanvas(SymbolChildUi sourceUi, int outputIndex)
        {
            var outputCount = sourceUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var outputWidth = sourceUi.Size.X / outputCount;   // size count must be non-zero in this method

            return ImRect.RectWithSize(
                new Vector2(sourceUi.PosOnCanvas.X + outputWidth * outputIndex + 1, sourceUi.PosOnCanvas.Y - 3),
                new Vector2(outputWidth - 2, 6));
        }


        public static ImRect GetUsableOutputSlotSize(SymbolChildUi targetUi, int outputIndex)
        {
            var opRect = GraphOperator._screenRect;
            var outputCount = targetUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var outputWidth = outputCount == 0
                ? opRect.GetWidth()
                : (opRect.GetWidth() + GraphOperator._slotGaps) / outputCount - GraphOperator._slotGaps;

            return ImRect.RectWithSize(
                new Vector2(
                    opRect.Min.X + (outputWidth + GraphOperator._slotGaps) * outputIndex,
                    opRect.Min.Y - GraphOperator._usableSlotHeight),
                new Vector2(
                    outputWidth,
                    GraphOperator._usableSlotHeight
                ));
        }

        #region inputs
        public static void DrawInputSlot(SymbolChildUi targetUi, int inputIndex)
        {
            var inputDef = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex];
            var usableArea = GetUsableInputSlotSize(targetUi, inputIndex);

            ImGui.PushID(targetUi.SymbolChild.Id.GetHashCode() + inputIndex);
            ImGui.SetCursorScreenPos(usableArea.Min);
            ImGui.InvisibleButton("input", usableArea.GetSize());
            THelpers.DebugItemRect("input-slot");

            var valueType = inputDef.DefaultValue.ValueType;
            var colorForType = ColorForInputType(inputDef);

            var dl = ImGui.GetWindowDrawList();

            // Note: isItemHovered does not work when being dragged from another item
            var hovered = BuildingConnections.TempConnection != null ? usableArea.Contains(ImGui.GetMousePos())
                : ImGui.IsItemHovered();

            if (BuildingConnections.IsInputSlotCurrentConnectionTarget(targetUi, inputIndex))
            {
                dl.AddRectFilled(usableArea.Min, usableArea.Max,
                    ColorVariations.Highlight.GetVariation(colorForType));

                if (ImGui.IsMouseDragging(0))
                {
                    BuildingConnections.Update();
                }
            }
            else if (hovered)
            {
                if (BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    dl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.GetVariation(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
                else
                {
                    dl.AddRectFilled(
                        usableArea.Min,
                        usableArea.Max,
                        ColorVariations.OperatorHover.GetVariation(colorForType)
                        );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        BuildingConnections.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (BuildingConnections.TempConnection != null)
                {
                    style = BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType)
                        ? ColorVariations.Highlight
                        : ColorVariations.Muted;
                }

                var pos = usableArea.Min + Vector2.UnitY * GraphOperator._inputSlotMargin;
                var size = new Vector2(usableArea.GetWidth(), GraphOperator._inputSlotHeight);
                dl.AddRectFilled(
                    pos,
                    pos + size,
                    style.GetVariation(colorForType)
                    );
            }

            ImGui.PopID();
        }


        private static Color ColorForInputType(Symbol.InputDefinition inputDef)
        {
            return TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        private static Color ColorForType(Symbol.OutputDefinition outputDef)
        {
            return TypeUiRegistry.Entries[outputDef.ValueType].Color;
        }



        public static ImRect GetInputSlotSizeInCanvas(SymbolChildUi targetUi, int inputIndex)
        {
            var inputCount = targetUi.SymbolChild.Symbol.InputDefinitions.Count;
            var inputWidth = inputCount == 0 ? targetUi.Size.X
                : targetUi.Size.X / inputCount;

            return ImRect.RectWithSize(
                new Vector2(targetUi.PosOnCanvas.X + inputWidth * inputIndex + 1, targetUi.PosOnCanvas.Y + targetUi.Size.Y - 3),
                new Vector2(inputWidth - 2, 6));
        }


        public static ImRect GetUsableInputSlotSize(SymbolChildUi targetUi, int inputIndex)
        {
            var opRect = GraphOperator._screenRect;
            var inputCount = targetUi.SymbolChild.Symbol.InputDefinitions.Count;
            var inputWidth = inputCount == 0
                ? opRect.GetWidth()
                : (opRect.GetWidth() + GraphOperator._slotGaps) / inputCount - GraphOperator._slotGaps;

            return ImRect.RectWithSize(
                new Vector2(
                    opRect.Min.X + (inputWidth + GraphOperator._slotGaps) * inputIndex,
                    opRect.Max.Y),
                new Vector2(
                    inputWidth,
                    GraphOperator._usableSlotHeight
                ));
        }
        #endregion
    }
}
