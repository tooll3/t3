using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.Styling;
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
            var isPotentialConnectionTargetNode =   _hoveredId == childUi.Id 
                                                    && ConnectionMaker.TempConnection != null
                                                    && ConnectionMaker.TempConnection.TargetParentOrChildId == ConnectionMaker.NotConnectedId;

            // Find visible input sockets from relevancy or connection
            var connectionsToNode = Graph.Connections.GetLinesIntoNode(childUi);
            SymbolUi childSymbolUi = SymbolUiRegistry.Entries[childUi.SymbolChild.Symbol.Id];
            var visibleInputUis = (from inputUi in childSymbolUi.InputUis.Values
                                   where isPotentialConnectionTargetNode || inputUi.Relevancy != Relevancy.Optional ||
                                         connectionsToNode.Any(c => c.Connection.TargetSlotId == inputUi.Id)
                                   orderby inputUi.Index
                                   select inputUi).ToArray();

            var additionalMultiInputSlots = 0;
            foreach (var input in visibleInputUis)
            {
                if (!input.InputDefinition.IsMultiInput) 
                    continue;
                
                //TODO: this should be refacatored, because it's very slow and is later repeated  
                var connectedLines = Graph.Connections.GetLinesToNodeInputSlot(childUi, input.Id);
                additionalMultiInputSlots += connectedLines.Count;
            }

            _drawList = Graph.DrawList;
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                var heightWithParams = 23 + (visibleInputUis.Length + additionalMultiInputSlots) * 13;
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(
                                                                               childUi.PosOnCanvas,
                                                                               childUi.PosOnCanvas + new Vector2(childUi.Size.X,
                                                                                                                 heightWithParams)));
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
                    GraphCanvas.Current.OpenComposition(instance);
                }

                if (_lastScreenRect.Contains(ImGui.GetMousePos()))
                {
                    _hoveredId = childUi.Id;
                }

                var hovered = ImGui.IsItemHovered();
                var drawList = GraphCanvas.Current.DrawList;

                // Rendering
                var typeColor = childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0
                                    ? TypeUiRegistry.GetPropertiesForType(childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType).Color
                                    : Color.Gray;


                drawList.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.Operator.Apply(typeColor));

                drawList.AddRectFilled(new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                                 new Vector2(_lastScreenRect.Max.X, _lastScreenRect.Max.Y + _inputSlotThickness + _inputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));


                // Visualize update
                {
                    var childInstance = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(c => c.Id == childUi.SymbolChild.Id);
                    var output = childInstance?.Outputs.FirstOrDefault();
                    int framesSinceLastUpdate = output?.DirtyFlag.FramesSinceLastUpdate ?? 0;
                    int updateCountThisFrame = output?.DirtyFlag.NumUpdatesWithinFrame ?? 0;
                    if (updateCountThisFrame > 0) {
                        const double timeScale = 0.125f;
                        var blink = (float)(ImGui.GetTime() * timeScale * updateCountThisFrame) % 1f * _lastScreenRect.GetWidth();
                        drawList.AddRectFilled(new Vector2(_lastScreenRect.Min.X + blink,
                                                           _lastScreenRect.Min.Y),
                                               new Vector2(_lastScreenRect.Min.X + blink + 2,
                                                           _lastScreenRect.Max.Y),
                                               new Color(0.06f)
                                              );
                    }
                }

                drawList.AddText(_lastScreenRect.Min + _labelPos,
                           ColorVariations.OperatorLabel.Apply(typeColor),
                           string.Format(childUi.SymbolChild.ReadableName));

                if (childUi.IsSelected)
                {
                    drawList.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }
            }
            ImGui.PopID();

            // Input Sockets...
            for (var inputIndex = 0; inputIndex < visibleInputUis.Length; inputIndex++)
            {
                Symbol.InputDefinition input = visibleInputUis[inputIndex].InputDefinition;

                var usableSlotArea = GetUsableInputSlotSize(childUi, inputIndex, visibleInputUis.Length);

                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + input.GetHashCode());
                ImGui.SetCursorScreenPos(usableSlotArea.Min);
                ImGui.InvisibleButton("input", usableSlotArea.GetSize());
                THelpers.DebugItemRect("input-slot");

                // Note: isItemHovered does not work when being dragged from another item
                var hovered = ConnectionMaker.TempConnection != null
                                  ? usableSlotArea.Contains(ImGui.GetMousePos())
                                  : ImGui.IsItemHovered();

                var isPotentialConnectionTarget = ConnectionMaker.IsMatchingInputType(input.DefaultValue.ValueType);
                var colorForType = ColorForInputType(input);

                var connectedLines = Graph.Connections.GetLinesToNodeInputSlot(childUi, input.Id);

                // Render input Label
                {
                    var inputLabelOpacity = Im.Remap(GraphCanvas.Current.Scale.X,
                                                     0.75f, 1.5f,
                                                     0f, 1f);
                    if (inputLabelOpacity > 0)
                    {
                        ImGui.PushFont(Fonts.FontSmall);
                        var labelColor = ColorVariations.OperatorLabel.Apply(colorForType);
                        labelColor.Rgba.W = inputLabelOpacity;
                        var label = input.Name;
                        if (input.IsMultiInput)
                        {
                            label += " [...]";
                        }

                        _drawList.AddText(usableSlotArea.GetCenter() + new Vector2(14, -7), labelColor, label);

                        ImGui.PopFont();
                    }
                }

                if (input.IsMultiInput)
                {
                    var showGaps = isPotentialConnectionTarget;

                    var socketCount = showGaps
                                          ? connectedLines.Count * 2 + 1
                                          : connectedLines.Count;

                    var socketHeight = (usableSlotArea.GetHeight() + 1) / socketCount;
                    var targetPos = new Vector2(
                                                usableSlotArea.Max.X - 2,
                                                usableSlotArea.Min.Y + socketHeight * 0.5f);

                    var topLeft = new Vector2(usableSlotArea.Min.X, usableSlotArea.Min.Y);
                    var socketSize = new Vector2(usableSlotArea.GetWidth(), socketHeight - 1);

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
                            line.TargetRect = _lastScreenRect;
                            line.IsSelected |= childUi.IsSelected;
                        }

                        DrawMultiInputSocket(childUi, input, usableSocketArea, colorForType, isSocketHovered, index, isGap);

                        targetPos.Y += socketHeight;
                        topLeft.Y += socketHeight;
                    }
                }
                else
                {
                    foreach (var line in connectedLines)
                    {
                        line.TargetPosition = new Vector2(usableSlotArea.Max.X-1,
                                                          usableSlotArea.GetCenter().Y);
                        line.IsSelected |= childUi.IsSelected;
                        line.TargetRect = _lastScreenRect;
                    }

                    DrawInputSlot(childUi, input, usableSlotArea, colorForType, hovered);
                }

                ImGui.PopID();
            }


            // Outputs sockets...
            var outputIndex = 0;
            foreach (var output in childUi.SymbolChild.Symbol.OutputDefinitions)
            {
                var usableArea = GetUsableOutputSlotArea(childUi, outputIndex);
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
                    line.SourcePosition = new Vector2(usableArea.Min.X +1, usableArea.GetCenter().Y); 
                    line.SourceRect = _lastScreenRect;
                    line.ColorForType = colorForType;
                    line.IsSelected |= childUi.IsSelected;
                }

                DrawOutput(childUi, output, usableArea, colorForType, hovered);

                outputIndex++;
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

                //var pos = usableArea.Min + Vector2.UnitY * (usableArea.GetHeight() - GraphNode._outputSlotMargin - GraphNode._outputSlotThickness);
                var pos = usableArea.Min;
                var size = new Vector2(GraphNode._outputSlotThickness, usableArea.GetHeight());
                _drawList.AddRectFilled(
                                        pos,
                                        pos + size,
                                        style.Apply(colorForType)
                                       );
            }
        }


        private static ImRect GetUsableOutputSlotArea(SymbolChildUi targetUi, int outputIndex)
        {
            var opRect = GraphNode._lastScreenRect;
            var outputCount = targetUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var outputHeight = outputCount == 0
                                   ? opRect.GetHeight()
                                   : (opRect.GetHeight() + GraphNode._slotGaps) / outputCount - GraphNode._slotGaps;

            return ImRect.RectWithSize(
                                       new Vector2(
                                                   opRect.Max.X - 2, // - GraphNode._usableSlotThickness,
                                                   opRect.Min.Y + (outputHeight + GraphNode._slotGaps) * outputIndex
                                                  ),
                                       new Vector2(
                                                   GraphNode._usableSlotThickness,
                                                   outputHeight
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

                //var pos =  usableArea.Min + Vector2.UnitX * ( GraphNode._inputSlotThickness - GraphNode._inputSlotMargin);
                var pos = new Vector2(
                                      usableArea.Max.X - GraphNode._inputSlotThickness - _inputSlotMargin,
                                      usableArea.Min.Y
                                     );
                var size = new Vector2(GraphNode._inputSlotThickness, usableArea.GetHeight());
                _drawList.AddRectFilled(
                                        pos,
                                        pos + size,
                                        style.Apply(colorForType)
                                       );


//                if (inputDef.IsMultiInput)
//                {
//                    _drawList.AddRectFilled(
//                                            pos + new Vector2(0, GraphNode._inputSlotThickness),
//                                            pos + new Vector2(GraphNode._inputSlotThickness, GraphNode._inputSlotThickness + GraphNode._multiInputSize),
//                                            style.Apply(colorForType)
//                                           );
//
//                    _drawList.AddRectFilled(
//                                            pos + new Vector2(size.X - GraphNode._inputSlotThickness, GraphNode._inputSlotThickness),
//                                            pos + new Vector2(size.X, GraphNode._inputSlotThickness + GraphNode._multiInputSize),
//                                            style.Apply(colorForType)
//                                           );
//                }
            }
        }


        private static void DrawMultiInputSocket(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType,
                                                 bool isInputHovered, int multiInputIndex, bool isGap)
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

                //var pos = usableArea.Min + Vector2.UnitY * GraphNode._inputSlotMargin;
                var pos = new Vector2(usableArea.Max.X - GraphNode._inputSlotMargin - GraphNode._inputSlotThickness,
                                      usableArea.Min.Y);
                var size = new Vector2(GraphNode._inputSlotThickness, usableArea.GetHeight());
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

        private static float _nodeTitleHeight = 22;

        private static ImRect GetUsableInputSlotSize(SymbolChildUi targetUi, int inputIndex, int visibleSlotCount)
        {
            //var opRect = GraphNode._lastScreenRect;
            var heightForInputs = _lastScreenRect.GetHeight() - _nodeTitleHeight;

            var areaForParams = new ImRect(new Vector2(
                                                       _lastScreenRect.Min.X,
                                                       _lastScreenRect.Min.Y + _nodeTitleHeight),
                                           _lastScreenRect.Max);
            var inputHeight = visibleSlotCount == 0
                                  ? areaForParams.GetHeight()
                                  : (areaForParams.GetHeight() + GraphNode._slotGaps) / visibleSlotCount - GraphNode._slotGaps;

            return ImRect.RectWithSize(
                                       new Vector2(
                                                   areaForParams.Min.X - _usableSlotThickness,
                                                   areaForParams.Min.Y + (inputHeight + GraphNode._slotGaps) * inputIndex
                                                  ),
                                       new Vector2(
                                                   GraphNode._usableSlotThickness,
                                                   inputHeight
                                                  ));
        }

        private static Color ColorForInputType(Symbol.InputDefinition inputDef)
        {
            return TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        #region style variables

        public static Vector2 _labelPos = new Vector2(4, 4);
        public static float _usableSlotThickness = 12;
        public static float _inputSlotThickness = 2;
        public static float _inputSlotMargin = 1;
        public static float _slotGaps = 2;
        public static float _outputSlotMargin = 1;
        private static float _outputSlotThickness = 2;

        #endregion

        private static Guid _hoveredId;

        private static ImRect _lastScreenRect;
        private static ImDrawListPtr _drawList;
    }
}