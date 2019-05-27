using ImGuiNET;
using imHelpers;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    public static class Slots
    {
        public static void DrawOutputSlot(SymbolChildUi ui, int outputIndex)
        {
            var outputDef = ui.SymbolChild.Symbol.OutputDefinitions[outputIndex];

            var virtualRectInCanvas = GetOutputSlotSizeInCanvas(ui, outputIndex);

            var rInScreen = Canvas.TransformRect(virtualRectInCanvas);

            ImGui.SetCursorScreenPos(rInScreen.Min);
            ImGui.InvisibleButton("output", rInScreen.GetSize());
            THelpers.DebugItemRect();
            var color = ColorForType(outputDef);

            if (DraftConnection.IsOutputSlotCurrentConnectionSource(ui, outputIndex))
            {
                Canvas.DrawRectFilled(virtualRectInCanvas, ColorForType(outputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    DraftConnection.Update();
                }
            }
            else if (ImGui.IsItemHovered())
            {
                Canvas.DrawRectFilled(virtualRectInCanvas, Color.White);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Color(0.2f).Rgba);
                ImGui.BeginTooltip();
                ImGui.Text($"-> .{outputDef.Name}");
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
                ImGui.PopStyleVar();

                if (ImGui.IsItemClicked(0))
                {
                    DraftConnection.StartFromOutputSlot(Canvas.Current.CompositionOp.Symbol, ui, outputIndex);
                }
            }
            else
            {
                Canvas.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(ui.Position.X + virtualRectInCanvas.GetWidth() * outputIndex + 1 + 3, ui.Position.Y - 1),
                        new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                    , DraftConnection.IsMatchingOutputType(outputDef.ValueType) ? Color.White : color);
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


        public static void DrawInputSlot(SymbolChildUi targetUi, int inputIndex)
        {
            var inputDef = targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex];
            var virtualRectInCanvas = GetInputSlotSizeInCanvas(targetUi, inputIndex);
            var rInScreen = Canvas.TransformRect(virtualRectInCanvas);
            ImGui.SetCursorScreenPos(rInScreen.Min);
            ImGui.InvisibleButton("input", rInScreen.GetSize());
            THelpers.DebugItemRect();

            var valueType = inputDef.DefaultValue.ValueType;
            var color = ColorForType(inputDef);

            var hovered = rInScreen.Contains(ImGui.GetMousePos()); // TODO: check why ImGui.IsItemHovered() is not working

            if (DraftConnection.IsInputSlotCurrentConnectionTarget(targetUi, inputIndex))
            {
                Canvas.DrawRectFilled(virtualRectInCanvas, ColorForType(inputDef));

                if (ImGui.IsMouseDragging(0))
                {
                    DraftConnection.Update();
                }
            }
            else if (hovered)
            {
                if (DraftConnection.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    Canvas.DrawRectFilled(virtualRectInCanvas, color);

                    if (ImGui.IsMouseReleased(0))
                    {
                        DraftConnection.CompleteAtInputSlot(Canvas.Current.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
                else
                {
                    Canvas.DrawRectFilled(virtualRectInCanvas, color);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked())
                    {
                        DraftConnection.StartFromInputSlot(Canvas.Current.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
            }
            else
            {
                Canvas.DrawRectFilled(
                    ImRect.RectWithSize(
                        new Vector2(targetUi.Position.X + virtualRectInCanvas.GetWidth() * inputIndex + 1 + 3,
                                    targetUi.Position.Y + targetUi.Size.Y - T3Style.VisibleSlotHeight),
                        new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6,
                                    T3Style.VisibleSlotHeight))
                    , color: DraftConnection.IsMatchingInputType(inputDef.DefaultValue.ValueType) ? Color.White : color);
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

    }
}
