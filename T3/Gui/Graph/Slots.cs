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
    public static class Slots
    {

        //private static void DrawAll(SymbolChildUi symbolChildUi)
        //{
        //    for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.OutputDefinitions.Count; slot_idx++)
        //    {
        //        DrawOutputSlot(symbolChildUi, slot_idx);
        //    }

        //    for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.InputDefinitions.Count; slot_idx++)
        //    {
        //        DrawInputSlot(symbolChildUi, slot_idx);
        //    }
        //}


        public static void DoStuff()
        {
            var symbol = GraphCanvas.Current.CompositionOp.Symbol;
            var allConnections = symbol.Connections;
            var uiChildrenById = GraphCanvas.Current.UiChildrenById;
            var wdl = ImGui.GetWindowDrawList();

            // 1. Initialize connection lines
            //var linesForConnections = new Dictionary<Symbol.Connection, ConnectionLineUi>(allConnections.Count);
            var lines = new List<ConnectionLineUi>(allConnections.Count);
            var linesOut = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();
            var linesInto = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();

            // 2. Prepare lines internal connections
            foreach (var c in allConnections)
            {
                var newLine = new ConnectionLineUi() { Connection = c };
                //linesForConnections.Add(c, newLine);
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
                    var colorForType = ColorForType(output);
                    var isHovered = false; // TODO Implement
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

                    outputIndex++;
                }

                // Inputs...
                var visibleInputs = childUi.SymbolChild.Symbol.InputDefinitions; // TODO: Implement relevancy filter
                for (var inputIndex = 0; inputIndex < visibleInputs.Count; inputIndex++)
                {
                    var input = visibleInputs[inputIndex];

                    var usableArea = GetUsableInputSlotSize(childUi, inputIndex);
                    var isHovered = false;                  // ToDo Implement
                    var isPotentialConnectionTarget = false; // ToDo Implement

                    var connectedLines = linesInto.ContainsKey(childUi)
                        ? linesInto[childUi].FindAll(l => l.Connection.TargetSlotId == input.Id)
                        : _noLines;

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
                            var socketCount = Math.Min(connectedLines.Count, 1);
                            var index = 0;
                            foreach (var line in connectedLines)
                            {
                                line.TargetPosition =
                                    new Vector2(
                                        usableArea.Min.X + usableArea.GetWidth() / socketCount * index,
                                        usableArea.Min.Y);
                                line.IsSelected |= childUi.IsSelected;
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
                }

            }

            // Draw Symbol Outputs
            // TODO ...

            // Draw Symbol Inputs
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

        // Reuse empty list instead of null check
        private static readonly List<ConnectionLineUi> _noLines = new List<ConnectionLineUi>();

        #region outputs 
        public static void DrawOutputSlot(SymbolChildUi ui, int outputIndex)
        {
            var outputDef = ui.SymbolChild.Symbol.OutputDefinitions[outputIndex];
            var usableArea = GetUsableOutputSlotSize(ui, outputIndex);

            var dl = ImGui.GetWindowDrawList();

            ImGui.SetCursorScreenPos(usableArea.Min);
            ImGui.PushID(ui.SymbolChild.Id.GetHashCode());

            ImGui.InvisibleButton("output", usableArea.GetSize());
            THelpers.DebugItemRect();
            var valueType = outputDef.ValueType;
            var colorForType = TypeUiRegistry.Entries[valueType].Color;

            //Note: isItemHovered does not work when dragging is active
            var hovered = BuildingConnections.TempConnection != null
                ? usableArea.Contains(ImGui.GetMousePos())
                : ImGui.IsItemHovered();


            if (BuildingConnections.IsOutputSlotCurrentConnectionSource(ui, outputIndex))
            {
                dl.AddRectFilled(usableArea.Min, usableArea.Max,
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
                    dl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        BuildingConnections.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, ui, outputIndex);
                    }
                }
                else
                {
                    dl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($".{outputDef.Name} ->");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        BuildingConnections.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, ui, outputIndex);
                    }
                }
            }
            else
            {
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
                    style.Apply(colorForType)
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
        #endregion

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
            var hovered = BuildingConnections.TempConnection != null
                ? usableArea.Contains(ImGui.GetMousePos())
                : ImGui.IsItemHovered();

            // Render Label
            var inputLabelOpacity = Im.Clamp((GraphCanvas.Current.Scale.X - 1f) / 3f, 0, 1);
            if (inputLabelOpacity > 0)
            {
                ImGui.PushFont(ImGuiDx11Impl.FontSmall);
                var labelColor = ColorVariations.OperatorLabel.Apply(colorForType);
                labelColor.Rgba.W = inputLabelOpacity;
                var label = inputDef.Name;
                if (inputDef.IsMultiInput)
                {
                    label += " [...]";
                }
                var textSize = ImGui.CalcTextSize(inputDef.Name);
                if (textSize.X > usableArea.GetWidth())
                {
                    ImGui.PushClipRect(usableArea.Min - new Vector2(0, 20), usableArea.Max, true);
                    dl.AddText(usableArea.Min + new Vector2(0, -15), labelColor, label);
                    ImGui.PopClipRect();
                }
                else
                {
                    dl.AddText(usableArea.Min + new Vector2((usableArea.GetWidth() - textSize.X) / 2, -15), labelColor, label);
                }
                ImGui.PopFont();
            }

            if (BuildingConnections.IsInputSlotCurrentConnectionTarget(targetUi, inputIndex))
            {
                dl.AddRectFilled(usableArea.Min, usableArea.Max,
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
                    dl.AddRectFilled(usableArea.Min, usableArea.Max,
                        ColorVariations.OperatorHover.Apply(colorForType));

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
                dl.AddRectFilled(
                    pos,
                    pos + size,
                    style.Apply(colorForType)
                    );

                if (inputDef.IsMultiInput)
                {
                    dl.AddRectFilled(
                        pos + new Vector2(0, GraphOperator._inputSlotHeight),
                        pos + new Vector2(GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                        style.Apply(colorForType)
                        );

                    dl.AddRectFilled(
                        pos + new Vector2(size.X - GraphOperator._inputSlotHeight, GraphOperator._inputSlotHeight),
                        pos + new Vector2(size.X, GraphOperator._inputSlotHeight + GraphOperator._multiInputSize),
                        style.Apply(colorForType)
                        );

                }
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
        #endregion

        private static Dictionary<Guid, Symbol.Connection> _connectionsToTargets = new Dictionary<Guid, Symbol.Connection>();
        private static Dictionary<Guid, Symbol.Connection> _connectionsFromSources = new Dictionary<Guid, Symbol.Connection>();
        private static Dictionary<Guid, SymbolChildUi> _childUiById = new Dictionary<Guid, SymbolChildUi>();
        private static List<ConnectionLineUi> _connectionLines = new List<ConnectionLineUi>(1000);
    }

    public class Socket
    {
        public ImRect screenRect;

    }

    public class ConnectionLineUi
    {
        //public Socket TargetSocket;
        //public Socket SourceSocket;
        //public bool IsUnderConstruction;    // not used yet
        //public int UnderConstructionMultiinputIndex = 0;    // not used yet
        public Vector2 TargetPosition;
        public Vector2 SourcePosition;
        public Color ColorForType;
        //public float Width;
        public bool IsSelected;
        public bool IsMultiinput;
        public Symbol.Connection Connection;
    }
}
