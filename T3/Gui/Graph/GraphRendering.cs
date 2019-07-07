using ImGuiNET;
using imHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.TypeColors;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Rendering the graph is complicated because:
    /// - Connection has no real model to store computations
    /// - Connection are defined by Guid references to Symbol-Definitions
    /// - Computation of connection end point position is involves...
    ///    - many states of the graph nodes
    ///    - connections under construction
    ///    - potentially hidden connections
    ///    - layout of connections into multiinput slots
    ///    
    /// 
    ///    
    ///</summary>
    public static class GraphRendering
    {
        public static void DrawGraph()
        {
            drawList = ImGui.GetWindowDrawList();    // just cachine

            var symbol = GraphCanvas.Current.CompositionOp.Symbol;
            //var allConnections = symbol.Connections;
            var allConnections = new List<Symbol.Connection>(symbol.Connections);
            if (BuildingConnections.TempConnection != null)
                allConnections.Add(BuildingConnections.TempConnection);

            var childUisById = GraphCanvas.Current.ChildUisById;
            var inputUisById = InputUiRegistry.Entries[symbol.Id];
            var outputUisById = OutputUiRegistry.Entries[symbol.Id];

            // 1. Initialize connection lines
            var lines = new List<ConnectionLineUi>(allConnections.Count);
            var linesFromNodes = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();
            var linesIntoNodes = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();
            var linesToOutputNodes = new Dictionary<IOutputUi, List<ConnectionLineUi>>();
            var linesFromInputNodes = new Dictionary<IInputUi, List<ConnectionLineUi>>();

            // 2. Prepare lines internal connections
            foreach (var c in allConnections)
            {
                var newLine = new ConnectionLineUi() { Connection = c };
                lines.Add(newLine);


                if (c == BuildingConnections.TempConnection)
                {
                    if (c.TargetParentOrChildId == BuildingConnections.NotConnected)
                    {
                        newLine.TargetPosition = ImGui.GetMousePos();
                    }
                    else if (c.SourceParentOrChildId == BuildingConnections.NotConnected)
                    {
                        newLine.TargetPosition = Vector2.Zero;
                        newLine.SourcePosition = ImGui.GetMousePos();
                        newLine.ColorForType = Color.White;

                    }
                    else
                    {
                        Log.Warning("invalid temporary connection?");
                    }
                }


                var isConnectionToSymbolOutput = c.TargetParentOrChildId == Guid.Empty;
                if (isConnectionToSymbolOutput)
                {
                    var outputNode = outputUisById[c.TargetSlotId];

                    if (!linesToOutputNodes.ContainsKey(outputNode))
                        linesToOutputNodes.Add(outputNode, new List<ConnectionLineUi>());

                    linesToOutputNodes[outputNode].Add(newLine);
                }
                else
                {
                    if (c.TargetParentOrChildId != BuildingConnections.NotConnected)
                    {
                        var targetNode = childUisById[c.TargetParentOrChildId];
                        if (!linesIntoNodes.ContainsKey(targetNode))
                            linesIntoNodes.Add(targetNode, new List<ConnectionLineUi>());

                        linesIntoNodes[targetNode].Add(newLine);
                    }
                }

                var isConnectionFromSymbolInput = c.SourceParentOrChildId == Guid.Empty;
                if (isConnectionFromSymbolInput)
                {
                    //var outputNode = outputUisById[c.TargetSlotId];
                    var inputNode = inputUisById[c.SourceSlotId];

                    if (!linesFromInputNodes.ContainsKey(inputNode))
                        linesFromInputNodes.Add(inputNode, new List<ConnectionLineUi>());

                    linesFromInputNodes[inputNode].Add(newLine);
                    newLine.ColorForType = TypeUiRegistry.Entries[inputNode.Type].Color;
                }
                else
                {
                    if (c.SourceParentOrChildId != BuildingConnections.NotConnected)
                    {
                        var sourceNode = childUisById[c.SourceParentOrChildId];
                        if (!linesFromNodes.ContainsKey(sourceNode))
                            linesFromNodes.Add(sourceNode, new List<ConnectionLineUi>());

                        linesFromNodes[sourceNode].Add(newLine);
                    }
                }
            }

            // Prepare connections to nodes under construction
            // TODO ...

            // 3. Draw Nodes and their sockets and set positions for connection lines
            foreach (var childUi in childUisById.Values)
            {
                GraphOperator.Draw(childUi);

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
                    var hovered = BuildingConnections.TempConnection != null
                        ? usableArea.Contains(ImGui.GetMousePos())
                        : ImGui.IsItemHovered();

                    var isPotentialConnectionTarget = false; // ToDo Implement

                    var connectedLines = linesFromNodes.ContainsKey(childUi)
                        ? linesFromNodes[childUi].FindAll(l => l.Connection.SourceSlotId == output.Id)
                        : _noLines;

                    foreach (var line in connectedLines)
                    {
                        line.SourcePosition = usableArea.GetCenter();
                        line.ColorForType = colorForType;
                        line.IsSelected |= childUi.IsSelected;
                    }

                    DrawOutput(childUi, output, usableArea, colorForType, hovered);

                    outputIndex++;
                }

                // Inputs...
                var visibleInputs = childUi.SymbolChild.Symbol.InputDefinitions; // TODO: Implement relevancy filter
                for (var inputIndex = 0; inputIndex < visibleInputs.Count; inputIndex++)
                {
                    var input = visibleInputs[inputIndex];

                    var usableArea = GetUsableInputSlotSize(childUi, inputIndex);

                    ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + input.GetHashCode());
                    ImGui.SetCursorScreenPos(usableArea.Min);
                    ImGui.InvisibleButton("input", usableArea.GetSize());
                    THelpers.DebugItemRect("input-slot");

                    // Note: isItemHovered does not work when being dragged from another item
                    var hovered = BuildingConnections.TempConnection != null
                        ? usableArea.Contains(ImGui.GetMousePos())
                        : ImGui.IsItemHovered();


                    //var isPotentialConnectionTarget = BuildingConnections.IsInputSlotCurrentConnectionTarget(childUi, inputIndex);
                    var isPotentialConnectionTarget = BuildingConnections.IsMatchingInputType(input.DefaultValue.ValueType);
                    var colorForType = ColorForInputType(input);

                    var connectedLines = linesIntoNodes.ContainsKey(childUi)
                        ? linesIntoNodes[childUi].FindAll(l => l.Connection.TargetSlotId == input.Id)
                        : _noLines;


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
                            drawList.AddText(usableArea.Min + new Vector2(0, -15), labelColor, label);
                            ImGui.PopClipRect();
                        }
                        else
                        {
                            drawList.AddText(usableArea.Min + new Vector2((usableArea.GetWidth() - textSize.X) / 2, -15), labelColor, label);
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
                        //}
                        //else
                        //{
                        //    // Sockets are defined through inputs
                        //    var socketCount = Math.Max(connectedLines.Count, 1);
                        //    var socketWidth = usableArea.GetWidth() / socketCount;
                        //    var targetPos = new Vector2(
                        //                usableArea.Min.X + socketWidth * 0.5f,
                        //                usableArea.Min.Y);

                        //    var index = 0;
                        //    foreach (var line in connectedLines)
                        //    {

                        //        line.TargetPosition = targetPos;
                        //        line.IsSelected |= childUi.IsSelected;

                        //        targetPos.X += socketWidth;
                        //        index++;
                        //    }
                        //    DrawInputSlot(childUi, input, usableArea, colorForType, hovered);
                        //}
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

            // Draw Output Nodes
            foreach (var pair in outputUisById)
            {
                var outputId = pair.Key;
                var outputNode = pair.Value;
                var connectedLines = linesToOutputNodes.ContainsKey(outputNode)
                    ? linesToOutputNodes[outputNode].FindAll(l => l.Connection.TargetSlotId == outputId)
                    : _noLines;

                var def = symbol.OutputDefinitions.Find(od => od.Id == outputId);
                OutputNodes.Draw(def, outputNode);

                var outputUisForSymbol = OutputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                var targetPos = new Vector2(
                    OutputNodes._lastScreenRect.GetCenter().X,
                    OutputNodes._lastScreenRect.Max.Y);

                foreach (var line in connectedLines)
                {
                    line.TargetPosition = targetPos;
                }
            }

            // Draw Inputs Nodes
            foreach (var pair in inputUisById)
            {
                var inputId = pair.Key;
                var inputNode = pair.Value;
                var connectedLines = linesFromInputNodes.ContainsKey(inputNode)
                    ? linesFromInputNodes[inputNode].FindAll(l => l.Connection.SourceSlotId == inputId)
                    : _noLines;

                var def = symbol.InputDefinitions.Find(idef => idef.Id == inputId);
                InputNodes.Draw(def, inputNode);

                var outputUisForSymbol = OutputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                var sourcePos = new Vector2(
                    InputNodes._lastScreenRect.GetCenter().X,
                    InputNodes._lastScreenRect.Min.Y);

                foreach (var line in connectedLines)
                {
                    line.SourcePosition = sourcePos;
                }
            }

            // 6. Draw ConnectionLines
            foreach (var line in lines)
            {
                var color = line.IsSelected
                    ? ColorVariations.Highlight.Apply(line.ColorForType)
                    : ColorVariations.ConnectionLines.Apply(line.ColorForType);

                drawList.AddBezierCurve(
                    line.SourcePosition,
                    line.SourcePosition + new Vector2(0, -50),
                    line.TargetPosition + new Vector2(0, 50),
                    line.TargetPosition,
                    color, 3f,
                    num_segments: 20);


                drawList.AddTriangleFilled(
                    line.TargetPosition + new Vector2(0, -3),
                    line.TargetPosition + new Vector2(4, 2),
                    line.TargetPosition + new Vector2(-4, 2),
                    color);
            }
        }


        private static void DrawOutput(SymbolChildUi childUi, Symbol.OutputDefinition outputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (BuildingConnections.IsOutputSlotCurrentConnectionSource(childUi, outputDef))
            {
                drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                    ColorVariations.Highlight.Apply(colorForType));

                if (ImGui.IsMouseDragging(0))
                {
                    BuildingConnections.Update();
                }
            }
            else if (hovered)
            {
                if (BuildingConnections.IsMatchingOutputType(outputDef.ValueType))
                {
                    drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
                else
                {
                    drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($".{outputDef.Name} ->");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        BuildingConnections.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (BuildingConnections.TempConnection != null)
                {
                    if (BuildingConnections.IsMatchingOutputType(outputDef.ValueType))
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

                var pos = usableArea.Min + Vector2.UnitY * (usableArea.GetHeight() - GraphOperator._outputSlotMargin - GraphOperator._outputSlotHeight);
                var size = new Vector2(usableArea.GetWidth(), GraphOperator._outputSlotHeight);
                drawList.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );
            }
        }


        public static ImRect GetUsableOutputSlotSize(SymbolChildUi targetUi, int outputIndex)
        {
            var opRect = GraphOperator._lastScreenRect;
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



        private static void DrawInputSlot(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (BuildingConnections.IsInputSlotCurrentConnectionTarget(targetUi, inputDef))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    BuildingConnections.Update();
                }
            }
            else if (hovered)
            {
                if (BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    //drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                    //    ColorVariations.Highlight.Apply(colorForType));

                    drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
                }
                else
                {
                    drawList.AddRectFilled(
                        usableArea.Min,
                        usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType)
                        );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        BuildingConnections.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (BuildingConnections.TempConnection != null)
                {
                    if (BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType))
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

                var pos = usableArea.Min + Vector2.UnitY * GraphOperator._inputSlotMargin;
                var size = new Vector2(usableArea.GetWidth(), GraphOperator._inputSlotHeight);
                drawList.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );

                if (inputDef.IsMultiInput)
                {
                    drawList.AddRectFilled(
                        pos + new Vector2(0, GraphOperator._inputSlotHeight),
                        pos + new Vector2(GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                        style.Apply(colorForType)
                        );

                    drawList.AddRectFilled(
                        pos + new Vector2(size.X - GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight),
                        pos + new Vector2(size.X, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                        style.Apply(colorForType)
                        );
                }
            }
        }

        private static void DrawMultiInputSocket(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool isInputHovered, int multiInputIndex, bool isGap)
        {
            if (BuildingConnections.IsInputSlotCurrentConnectionTarget(targetUi, inputDef, multiInputIndex))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    BuildingConnections.Update();
                }
            }
            else if (isInputHovered)
            {
                if (BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    //drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                    //    ColorVariations.Highlight.Apply(colorForType));

                    drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                    }
                }
                else
                {
                    drawList.AddRectFilled(
                        usableArea.Min,
                        usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType)
                        );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        BuildingConnections.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (BuildingConnections.TempConnection != null)
                {
                    if (BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType))
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

                var pos = usableArea.Min + Vector2.UnitY * GraphOperator._inputSlotMargin;
                var size = new Vector2(usableArea.GetWidth(), GraphOperator._inputSlotHeight);
                drawList.AddRectFilled(
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



        private static Color ColorForInputType(Symbol.InputDefinition inputDef)
        {
            return TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        private static Color ColorForTypeOut(Symbol.OutputDefinition outputDef)
        {
            return TypeUiRegistry.Entries[outputDef.ValueType].Color;
        }

        public static ImRect GetUsableInputSlotSize(SymbolChildUi targetUi, int inputIndex)
        {
            var opRect = GraphOperator._lastScreenRect;
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

        // Reuse empty list instead of null check
        private static readonly List<ConnectionLineUi> _noLines = new List<ConnectionLineUi>();
        private static ImDrawListPtr drawList;

        private static Dictionary<Guid, Symbol.Connection> _connectionsToTargets = new Dictionary<Guid, Symbol.Connection>();
        private static Dictionary<Guid, Symbol.Connection> _connectionsFromSources = new Dictionary<Guid, Symbol.Connection>();
        private static Dictionary<Guid, SymbolChildUi> _childUiById = new Dictionary<Guid, SymbolChildUi>();
        private static List<ConnectionLineUi> _connectionLines = new List<ConnectionLineUi>(1000);
    }

    public class ConnectionLineUi
    {
        public Vector2 TargetPosition;
        public Vector2 SourcePosition;
        public Color ColorForType;
        public bool IsSelected;
        public bool IsMultiinput;
        public Symbol.Connection Connection;
    }
}
