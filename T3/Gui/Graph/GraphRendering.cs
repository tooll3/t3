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
        public static void DoStuff()
        {
            var symbol = GraphCanvas.Current.CompositionOp.Symbol;
            var allConnections = symbol.Connections;
            var uiChildrenById = GraphCanvas.Current.UiChildrenById;
            wdl = ImGui.GetWindowDrawList();

            // 1. Initialize connection lines
            var lines = new List<ConnectionLineUi>(allConnections.Count);
            var linesOut = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();
            var linesInto = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();

            // 2. Prepare lines internal connections
            foreach (var c in allConnections)
            {
                var newLine = new ConnectionLineUi() { Connection = c };
                lines.Add(newLine);

                var isConnectionToSymbolOutput = c.TargetParentOrChildId == Guid.Empty;
                if (isConnectionToSymbolOutput)
                {
                    // TODO
                }
                else
                {
                    var childUi = uiChildrenById[c.TargetParentOrChildId];
                    if (!linesInto.ContainsKey(childUi))
                        linesInto.Add(childUi, new List<ConnectionLineUi>());

                    linesInto[childUi].Add(newLine);
                }

                var isConnectionFromSymbolInput = c.SourceParentOrChildId == Guid.Empty;
                if (isConnectionFromSymbolInput)
                {
                    // TODO
                }
                else
                {
                    var source = uiChildrenById[c.SourceParentOrChildId];
                    if (!linesOut.ContainsKey(source))
                        linesOut.Add(source, new List<ConnectionLineUi>());

                    linesOut[source].Add(newLine);
                }

            }

            // Prepare connections under construction
            // TODO ...

            // Prepare connections to nodes under construction
            // TODO ...

            // 3. Draw Nodes and their sockets and set positions for connection lines
            foreach (var childUi in GraphCanvas.Current.UiChildrenById.Values)
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

                    //var colorForType = ColorForTypeOut(output);
                    //var isHovered = false; // TODO Implement
                    var isPotentialConnectionTarget = false; // ToDo Implement

                    var connectedLines = linesOut.ContainsKey(childUi)
                        ? linesOut[childUi].FindAll(l => l.Connection.SourceSlotId == output.Id)
                        : _noLines;

                    foreach (var line in connectedLines)
                    {
                        line.SourcePosition = usableArea.GetCenter();
                        line.ColorForType = colorForType;
                        line.IsSelected |= childUi.IsSelected;
                    }

                    DrawOutput(childUi, outputIndex, output, usableArea, colorForType, hovered);

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


                    var isPotentialConnectionTarget = BuildingConnections.IsInputSlotCurrentConnectionTarget(childUi, inputIndex);
                    var colorForType = ColorForInputType(input);

                    var connectedLines = linesInto.ContainsKey(childUi)
                        ? linesInto[childUi].FindAll(l => l.Connection.TargetSlotId == input.Id)
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
                            wdl.AddText(usableArea.Min + new Vector2(0, -15), labelColor, label);
                            ImGui.PopClipRect();
                        }
                        else
                        {
                            wdl.AddText(usableArea.Min + new Vector2((usableArea.GetWidth() - textSize.X) / 2, -15), labelColor, label);
                        }
                        ImGui.PopFont();
                    }

                    if (input.IsMultiInput)
                    {
                        if (isPotentialConnectionTarget)
                        {
                            // Reveal gaps for insertion / reordering
                            var socketCount = connectedLines.Count * 2 + 1;
                            var socketWidth = usableArea.GetWidth() / socketCount;
                            var targetPos = new Vector2(
                                        usableArea.Min.X + socketWidth * 0.5f,
                                        usableArea.Min.Y);

                            for (var index = 0; index < socketCount; index++)
                            {
                                var isGap = (index & 1) == 0;
                                if (isGap)
                                {
                                    // intentionally left blank :-)
                                }
                                else
                                {
                                    var line = connectedLines[index << 1];
                                    line.TargetPosition = targetPos;
                                    line.IsSelected |= childUi.IsSelected;

                                    // TODO: Draw input

                                }
                                targetPos.X += socketWidth;
                            }
                        }
                        else
                        {
                            // Sockets are defined through inputs
                            var socketCount = Math.Max(connectedLines.Count, 1);
                            var socketWidth = usableArea.GetWidth() / socketCount;
                            var targetPos = new Vector2(
                                        usableArea.Min.X + socketWidth * 0.5f,
                                        usableArea.Min.Y);

                            var index = 0;
                            foreach (var line in connectedLines)
                            {
                                //line.TargetPosition =
                                //    new Vector2(
                                //        usableArea.Min.X + usableArea.GetWidth() / socketCount * index,
                                //        usableArea.Min.Y);
                                line.TargetPosition = targetPos;
                                line.IsSelected |= childUi.IsSelected;

                                targetPos.X += socketWidth;
                                index++;
                            }
                        }
                    }
                    else
                    {
                        foreach (var line in connectedLines)
                        {
                            line.TargetPosition = usableArea.GetCenter();
                            line.IsSelected |= childUi.IsSelected;
                        }

                        // Todo: Draw Input...
                    }
                    DrawInputSlot(childUi, inputIndex, input, usableArea, colorForType, hovered);

                    ImGui.PopID();
                }
            }

            // Draw Output Nodes
            // TODO ...

            // Draw Inputs Nodes
            // TODO ....

            // 6. Draw ConnectionLines
            foreach (var line in lines)
            {
                var color = line.IsSelected
                    ? ColorVariations.Highlight.Apply(line.ColorForType)
                    : ColorVariations.ConnectionLines.Apply(line.ColorForType);

                wdl.AddBezierCurve(
                    line.SourcePosition,
                    line.SourcePosition + new Vector2(0, -50),
                    line.TargetPosition + new Vector2(0, 50),
                    line.TargetPosition,
                    color, 3f,
                    num_segments: 20);


                wdl.AddTriangleFilled(
                    line.TargetPosition + new Vector2(0, -3),
                    line.TargetPosition + new Vector2(4, 2),
                    line.TargetPosition + new Vector2(-4, 2),
                    color);
            }
        }




        private static void DrawOutput(SymbolChildUi childUi, int outputIndex, Symbol.OutputDefinition outputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (BuildingConnections.IsOutputSlotCurrentConnectionSource(childUi, outputIndex))
            {
                wdl.AddRectFilled(usableArea.Min, usableArea.Max,
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
                    wdl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputIndex);
                    }
                }
                else
                {
                    wdl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($".{outputDef.Name} ->");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        BuildingConnections.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputIndex);
                    }
                }
            }
            else
            {
                var style = ColorVariations.Operator;
                if (BuildingConnections.TempConnection != null)
                {
                    style = BuildingConnections.IsMatchingOutputType(outputDef.ValueType)
                        ? ColorVariations.Highlight
                        : ColorVariations.Muted;
                }

                var pos = usableArea.Min + Vector2.UnitY * (usableArea.GetHeight() - GraphOperator._outputSlotMargin - GraphOperator._outputSlotHeight);
                var size = new Vector2(usableArea.GetWidth(), GraphOperator._outputSlotHeight);
                wdl.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );
            }
        }


        //public static ImRect GetOutputSlotSizeInCanvas(SymbolChildUi sourceUi, int outputIndex)
        //{
        //    var outputCount = sourceUi.SymbolChild.Symbol.OutputDefinitions.Count;
        //    var outputWidth = sourceUi.Size.X / outputCount;   // size count must be non-zero in this method

        //    return ImRect.RectWithSize(
        //        new Vector2(sourceUi.PosOnCanvas.X + outputWidth * outputIndex + 1, sourceUi.PosOnCanvas.Y - 3),
        //        new Vector2(outputWidth - 2, 6));
        //}


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


        private static void DrawInputSlot(SymbolChildUi targetUi, int inputIndex, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool hovered)
        {

            if (BuildingConnections.IsInputSlotCurrentConnectionTarget(targetUi, inputIndex))
            {
                wdl.AddRectFilled(usableArea.Min, usableArea.Max,
                    ColorVariations.Highlight.Apply(colorForType));

                if (ImGui.IsMouseDragging(0))
                {
                    BuildingConnections.Update();
                }
            }
            else if (hovered)
            {
                if (BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    wdl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputIndex);
                    }
                }
                else
                {
                    wdl.AddRectFilled(
                        usableArea.Min,
                        usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType)
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
                wdl.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );

                if (inputDef.IsMultiInput)
                {
                    wdl.AddRectFilled(
                        pos + new Vector2(0, GraphOperator._inputSlotHeight),
                        pos + new Vector2(GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                        style.Apply(colorForType)
                        );

                    wdl.AddRectFilled(
                        pos + new Vector2(size.X - GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight),
                        pos + new Vector2(size.X, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                        style.Apply(colorForType)
                        );
                }
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
        private static ImDrawListPtr wdl;

        private static Dictionary<Guid, Symbol.Connection> _connectionsToTargets = new Dictionary<Guid, Symbol.Connection>();
        private static Dictionary<Guid, Symbol.Connection> _connectionsFromSources = new Dictionary<Guid, Symbol.Connection>();
        private static Dictionary<Guid, SymbolChildUi> _childUiById = new Dictionary<Guid, SymbolChildUi>();
        private static List<ConnectionLineUi> _connectionLines = new List<ConnectionLineUi>(1000);
    }

    //public class Socket
    //{
    //    public ImRect screenRect;

    //}

    public class ConnectionLineUi
    {
        //public Socket TargetSocket;
        //public Socket SourceSocket;
        //public bool IsUnderConstruction;    // not used yet
        //public int UnderConstructionMultiinputIndex = 0;    // not used yet
        public Vector2 TargetPosition;
        public Vector2 SourcePosition;
        public Color ColorForType;
        public bool IsSelected;
        public bool IsMultiinput;
        public Symbol.Connection Connection;
    }
}
