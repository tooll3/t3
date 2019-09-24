using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.TypeColors;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders a graphic representation of a <see cref="SymbolChild"/> within the current <see cref="GraphWindow"/>
    /// </summary>
    static class GraphNode
    {
        public static void Draw(SymbolChildUi childUi)
        {
            _drawList = Graph._drawList;


            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(childUi.PosOnCanvas, childUi.PosOnCanvas + childUi.Size));
                _lastScreenRect.Floor();

                // Interaction
                ImGui.SetCursorScreenPos(_lastScreenRect.Min);
                ImGui.InvisibleButton("node", _lastScreenRect.GetSize());

                THelpers.DebugItemRect();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    T3UI.AddHoveredId(childUi.SymbolChild.Id);
                }

                SelectableMovement.Handle(childUi);

                if (ImGui.IsItemActive() && ImGui.IsMouseDoubleClicked(0))
                {
                    var instance = GraphCanvas.Current.CompositionOp.Children.Find(c => c.Symbol == childUi.SymbolChild.Symbol);
                    GraphCanvas.Current.CompositionOp = instance;
                }

                bool hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    //NodeDetailsPanel.Draw(childUi);
                }

                // Rendering
                var typeColor = childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0
                                    ? TypeUiRegistry.GetPropertiesForType(childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType).Color
                                    : Color.Gray;

                var dl = GraphCanvas.Current.DrawList;
                dl.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.Operator.Apply(typeColor));

                dl.AddRectFilled(new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                                 new Vector2(_lastScreenRect.Max.X, _lastScreenRect.Max.Y + _inputSlotHeight + _inputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));

                dl.AddText(_lastScreenRect.Min + _labelPos,
                           ColorVariations.OperatorLabel.Apply(typeColor),
                           string.Format($"{childUi.SymbolChild.ReadableName}"));

                if (childUi.IsSelected)
                {
                    dl.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }
            }
            ImGui.PopID();

            // Outputs...
            var outputIndex = 0;
            foreach (var output in childUi.SymbolChild.Symbol.OutputDefinitions)
            {
                var usableArea = GetUsableOutputSlotSize(childUi, outputIndex);
                ImGui.SetCursorScreenPos(usableArea.Min);
                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + output.Id.GetHashCode());

                ImGui.InvisibleButton("output", usableArea.GetSize());
                THelpers.DebugItemRect();
                var valueType = output.ValueType;
                var colorForType = TypeUiRegistry.Entries[valueType].Color;

                //Note: isItemHovered does not work when dragging is active
                var hovered = ConnectionMaker.TempConnection != null
                    ? usableArea.Contains(ImGui.GetMousePos())
                    : ImGui.IsItemHovered();

                foreach (var line in Graph.Connections.GetLinesFromNodeOutput(childUi, output.Id))
                {
                    line.SourcePosition = usableArea.GetCenter();
                    line.ColorForType = colorForType;
                    line.IsSelected |= childUi.IsSelected;
                }

                DrawOutput(childUi, output, usableArea, colorForType, hovered);

                outputIndex++;
            }

            // Input Sockets...

            // prototype implemention of finding visible relevant inputs
            var connectionsToNode = Graph.Connections.GetLinesIntoNode(childUi);
            SymbolUi childSymbolUi = SymbolUiRegistry.Entries[childUi.SymbolChild.Symbol.Id];
            var visibleInputUis = (from inputUi in childSymbolUi.InputUis.Values
                                   where inputUi.Relevancy != Relevancy.Optional ||
                                         connectionsToNode.Any(c => c.Connection.TargetSlotId == inputUi.Id)
                                   select inputUi).ToArray();

            for (var inputIndex = 0; inputIndex < visibleInputUis.Length; inputIndex++)
            {
                Symbol.InputDefinition input = visibleInputUis[inputIndex].InputDefinition;

                var usableArea = GetUsableInputSlotSize(childUi, inputIndex, visibleInputUis.Length);

                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + input.GetHashCode());
                ImGui.SetCursorScreenPos(usableArea.Min);
                ImGui.InvisibleButton("input", usableArea.GetSize());
                THelpers.DebugItemRect("input-slot");

                // Note: isItemHovered does not work when being dragged from another item
                var hovered = ConnectionMaker.TempConnection != null
                    ? usableArea.Contains(ImGui.GetMousePos())
                    : ImGui.IsItemHovered();

                var isPotentialConnectionTarget = ConnectionMaker.IsMatchingInputType(input.DefaultValue.ValueType);
                var colorForType = ColorForInputType(input);

                var connectedLines = Graph.Connections.GetLinesToNodeInputSlot(childUi, input.Id);

                // Render Label
                var inputLabelOpacity = Im.Clamp((GraphCanvas.Current.Scale.X - 1f) / 3f, 0, 1);
                if (inputLabelOpacity > 0)
                {
                    ImGui.PushFont(ImGuiDx11Impl.FontSmall);
                    var labelColor = ColorVariations.OperatorLabel.Apply(colorForType);
                    labelColor.Rgba.W = inputLabelOpacity;
                    var label = input.Name;
                    if (input.IsMultiInput)
                    {
                        label += " [...]";
                    }
                    var textSize = ImGui.CalcTextSize(input.Name);
                    if (textSize.X > usableArea.GetWidth())
                    {
                        ImGui.PushClipRect(usableArea.Min - new Vector2(0, 20), usableArea.Max, true);
                        _drawList.AddText(usableArea.Min + new Vector2(0, -15), labelColor, label);
                        ImGui.PopClipRect();
                    }
                    else
                    {
                        _drawList.AddText(usableArea.Min + new Vector2((usableArea.GetWidth() - textSize.X) / 2, -15), labelColor, label);
                    }
                    ImGui.PopFont();
                }

                if (input.IsMultiInput)
                {
                    var showGaps = isPotentialConnectionTarget;

                    var socketCount = showGaps
                        ? connectedLines.Count * 2 + 1
                        : connectedLines.Count;

                    var socketWidth = usableArea.GetWidth() / socketCount;
                    var targetPos = new Vector2(
                                usableArea.Min.X + socketWidth * 0.5f,
                                usableArea.Min.Y);

                    var topLeft = new Vector2(usableArea.Min.X, usableArea.Min.Y);
                    var socketSize = new Vector2(socketWidth - 2, usableArea.GetHeight());

                    for (var index = 0; index < socketCount; index++)
                    {

                        var usableSocketArea = new ImRect(
                            topLeft,
                            topLeft + socketSize);

                        var isSocketHovered = usableSocketArea.Contains(ImGui.GetMousePos());

                        bool isGap = false;
                        if (showGaps)
                        {
                            isGap = (index & 1) == 0;
                        }

                        if (!isGap)
                        {
                            var line = showGaps
                                ? connectedLines[index >> 1]
                                : connectedLines[index];

                            line.TargetPosition = targetPos;
                            line.IsSelected |= childUi.IsSelected;
                        }
                        DrawMultiInputSocket(childUi, input, usableSocketArea, colorForType, isSocketHovered, index, isGap);

                        targetPos.X += socketWidth;
                        topLeft.X += socketWidth;
                    }
                }
                else
                {
                    foreach (var line in connectedLines)
                    {
                        line.TargetPosition = usableArea.GetCenter();
                        line.IsSelected |= childUi.IsSelected;
                    }
                    DrawInputSlot(childUi, input, usableArea, colorForType, hovered);
                }

                ImGui.PopID();
            }

        }



        private static void DrawOutput(SymbolChildUi childUi, Symbol.OutputDefinition outputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (ConnectionMaker.IsOutputSlotCurrentConnectionSource(childUi, outputDef))
            {
                _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                    ColorVariations.Highlight.Apply(colorForType));

                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (hovered)
            {
                if (ConnectionMaker.IsMatchingOutputType(outputDef.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($".{outputDef.Name} ->");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (ConnectionMaker.TempConnection != null)
                {
                    if (ConnectionMaker.IsMatchingOutputType(outputDef.ValueType))
                    {
                        var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.5f);
                        colorForType.Rgba.W *= blink;
                        style = ColorVariations.Highlight;
                    }
                    else
                    {
                        style = ColorVariations.Muted;
                    }
                }

                var pos = usableArea.Min + Vector2.UnitY * (usableArea.GetHeight() - GraphNode._outputSlotMargin - GraphNode._outputSlotHeight);
                var size = new Vector2(usableArea.GetWidth(), GraphNode._outputSlotHeight);
                _drawList.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );
            }
        }


        private static ImRect GetUsableOutputSlotSize(SymbolChildUi targetUi, int outputIndex)
        {
            var opRect = GraphNode._lastScreenRect;
            var outputCount = targetUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var outputWidth = outputCount == 0
                ? opRect.GetWidth()
                : (opRect.GetWidth() + GraphNode._slotGaps) / outputCount - GraphNode._slotGaps;

            return ImRect.RectWithSize(
                new Vector2(
                    opRect.Min.X + (outputWidth + GraphNode._slotGaps) * outputIndex,
                    opRect.Min.Y - GraphNode._usableSlotHeight),
                new Vector2(
                    outputWidth,
                    GraphNode._usableSlotHeight
                ));
        }


        private static void DrawInputSlot(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (hovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(
                        usableArea.Min,
                        usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType)
                        );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (ConnectionMaker.TempConnection != null)
                {
                    if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                    {
                        var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.5f);
                        colorForType.Rgba.W *= blink;
                        style = ColorVariations.Highlight;
                    }
                    else
                    {
                        style = ColorVariations.Muted;
                    }
                }

                var pos = usableArea.Min + Vector2.UnitY * GraphNode._inputSlotMargin;
                var size = new Vector2(usableArea.GetWidth(), GraphNode._inputSlotHeight);
                _drawList.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );

                if (inputDef.IsMultiInput)
                {
                    _drawList.AddRectFilled(
                        pos + new Vector2(0, GraphNode._inputSlotHeight),
                        pos + new Vector2(GraphNode._inputSlotHeight, GraphNode._inputSlotHeight + GraphNode._multiInputSize),
                        style.Apply(colorForType)
                        );

                    _drawList.AddRectFilled(
                        pos + new Vector2(size.X - GraphNode._inputSlotHeight, GraphNode._inputSlotHeight),
                        pos + new Vector2(size.X, GraphNode._inputSlotHeight + GraphNode._multiInputSize),
                        style.Apply(colorForType)
                        );
                }
            }
        }


        private static void DrawMultiInputSocket(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool isInputHovered, int multiInputIndex, bool isGap)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef, multiInputIndex))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (isInputHovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(
                        usableArea.Min,
                        usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType)
                        );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                        Log.Debug("started connection at MultiInputIndex:" + multiInputIndex);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (ConnectionMaker.TempConnection != null)
                {
                    if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                    {
                        var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.5f);
                        colorForType.Rgba.W *= blink;
                        style = ColorVariations.Highlight;
                    }
                    else
                    {
                        style = ColorVariations.Muted;
                    }
                }

                var pos = usableArea.Min + Vector2.UnitY * GraphNode._inputSlotMargin;
                var size = new Vector2(usableArea.GetWidth(), GraphNode._inputSlotHeight);
                _drawList.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );

                //drawList.AddRectFilled(
                //    pos + new Vector2(0, GraphOperator._inputSlotHeight),
                //    pos + new Vector2(GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                //    style.Apply(colorForType)
                //    );

                //drawList.AddRectFilled(
                //    pos + new Vector2(size.X - GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight),
                //    pos + new Vector2(size.X, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                //    style.Apply(colorForType)
                //    );
            }
        }

        private static ImRect GetUsableInputSlotSize(SymbolChildUi targetUi, int inputIndex, int visibleSlotCount)
        {
            var opRect = GraphNode._lastScreenRect;
            var inputWidth = visibleSlotCount == 0
                ? opRect.GetWidth()
                : (opRect.GetWidth() + GraphNode._slotGaps) / visibleSlotCount - GraphNode._slotGaps;

            return ImRect.RectWithSize(
                new Vector2(
                    opRect.Min.X + (inputWidth + GraphNode._slotGaps) * inputIndex,
                    opRect.Max.Y),
                new Vector2(
                    inputWidth,
                    GraphNode._usableSlotHeight
                ));
        }

        private static Color ColorForInputType(Symbol.InputDefinition inputDef)
        {
            return TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        #region style variables
        public static Vector2 _labelPos = new Vector2(4, 4);
        public static float _usableSlotHeight = 12;
        public static float _inputSlotMargin = 1;
        public static float _inputSlotHeight = 2;
        public static float _slotGaps = 2;
        public static float _outputSlotMargin = 1;
        public static float _outputSlotHeight = 2;
        public static float _multiInputSize = 5;
        #endregion

        public static ImRect _lastScreenRect;
        public static ImDrawListPtr _drawList;
    }
}