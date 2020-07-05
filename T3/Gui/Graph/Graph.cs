using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using Truncon.Collections;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>Rendering a node graph</summary>
    /// <remarks>
    /// Rendering the graph is complicated because:
    /// - Connection has no real model to store computations
    /// - Connections are defined by Guid references to Symbol-Definitions
    /// - Computing connection end point position involves...
    ///    - ...many states of the graph nodes
    ///    - ...connections under construction
    ///    - ...potentially hidden connections
    ///    - ...layout of connections into multi input slots
    ///    
    /// This implementation first collects the information required to drawing the input sockets
    /// and connection links over several passes in which the information about visible connection-lines
    /// is collected into a list of ConnectionLineUi instances. These passes are...
    /// 
    /// 1. Initializes lists of ConnectionLineUis
    /// 2. Fill the lists of which nodes are connected to which lines
    /// 3. Draw nodes and their sockets and set positions for connection lines
    /// 4. Draw inputs
    /// 5. Draw outputs 
    /// 6. Draw connection lines
    ///</remarks>
    public static class Graph
    {
        public static void DrawGraph(ImDrawListPtr drawList)
        {
            DrawList = drawList;

            var graphSymbol = GraphCanvas.Current.CompositionOp.Symbol;
            var allConnections = new List<Symbol.Connection>(graphSymbol.Connections);
            allConnections.AddRange(ConnectionMaker.TempConnections);

            var children = GraphCanvas.Current.CompositionOp.Children;

            _symbolUi = SymbolUiRegistry.Entries[graphSymbol.Id];
            _childUis = _symbolUi.ChildUis;
            _inputUisById = _symbolUi.InputUis;
            _outputUisById = _symbolUi.OutputUis;

            // 1. Initializes lists of ConnectionLineUis
            Connections.Init();

            // 2. Collect which nodes are connected to which lines
            foreach (var c in allConnections)
            {
                Connections.CreateAndSortLineUi(c);
            }

            // 3. Draw Nodes and their sockets and set positions for connection lines
            foreach (var instance in children)
            {
                var childUi = _childUis.Single(ui => ui.Id == instance.SymbolChildId);
                GraphNode.Draw(childUi, instance);
            }

            // 4. Draw Inputs Nodes
            foreach (var inputNode in _inputUisById)
            {
                var index = graphSymbol.InputDefinitions.FindIndex(def => def.Id == inputNode.Key);
                var inputDef = graphSymbol.InputDefinitions[index];
                InputNode.Draw(inputDef, inputNode.Value, index);

                var sourcePos = new Vector2(
                                            InputNode._lastScreenRect.Max.X + GraphNode.UsableSlotThickness,
                                            InputNode._lastScreenRect.GetCenter().Y
                                           );

                foreach (var line in Connections.GetLinesFromInputNodes(inputNode.Value, inputNode.Key))
                {
                    line.SourcePosition = sourcePos;
                }
            }

            // 5. Draw Output Nodes
            foreach (var (outputId, outputNode) in _outputUisById)
            {
                var outputDef = graphSymbol.OutputDefinitions.Find(od => od.Id == outputId);
                OutputNode.Draw(outputDef, outputNode);

                var targetPos = new Vector2(OutputNode._lastScreenRect.Min.X + GraphNode.InputSlotThickness,
                                            OutputNode._lastScreenRect.GetCenter().Y);

                foreach (var line in Connections.GetLinesToOutputNodes(outputNode, outputId))
                {
                    line.TargetPosition = targetPos;
                }
            }

            // 6. Draw ConnectionLines
            foreach (var line in Connections.Lines)
            {
                line.Draw();
            }
        }

        internal class ConnectionSorter
        {
            public List<ConnectionLineUi> Lines;

            public void Init()
            {
                Lines = new List<ConnectionLineUi>();
                _linesFromNodes = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();
                _linesIntoNodes = new Dictionary<SymbolChildUi, List<ConnectionLineUi>>();
                _linesToOutputNodes = new Dictionary<IOutputUi, List<ConnectionLineUi>>();
                _linesFromInputNodes = new Dictionary<IInputUi, List<ConnectionLineUi>>();
            }

            public void CreateAndSortLineUi(Symbol.Connection c)
            {
                var newLine = new ConnectionLineUi(c);
                Lines.Add(newLine);

                if (c.IsConnectedToSymbolOutput)
                {
                    var outputNode = _outputUisById[c.TargetSlotId];

                    if (!_linesToOutputNodes.ContainsKey(outputNode))
                        _linesToOutputNodes.Add(outputNode, new List<ConnectionLineUi>());

                    _linesToOutputNodes[outputNode].Add(newLine);
                }
                else if (c.TargetParentOrChildId != ConnectionMaker.NotConnectedId
                         && c.TargetParentOrChildId != ConnectionMaker.UseDraftChildId)
                {
                    var targetNode = _childUis.Single(childUi => childUi.Id == c.TargetParentOrChildId);
                    if (!_linesIntoNodes.ContainsKey(targetNode))
                        _linesIntoNodes.Add(targetNode, new List<ConnectionLineUi>());

                    _linesIntoNodes[targetNode].Add(newLine);
                }

                if (c.IsConnectedToSymbolInput)
                {
                    var inputNode = _inputUisById[c.SourceSlotId];

                    if (!_linesFromInputNodes.ContainsKey(inputNode))
                        _linesFromInputNodes.Add(inputNode, new List<ConnectionLineUi>());

                    _linesFromInputNodes[inputNode].Add(newLine);
                    newLine.ColorForType = TypeUiRegistry.Entries[inputNode.Type].Color;
                }
                else if (c.SourceParentOrChildId != ConnectionMaker.NotConnectedId
                         && c.SourceParentOrChildId != ConnectionMaker.UseDraftChildId)
                {
                    var sourceNode = _childUis.Single(childUi => childUi.Id == c.SourceParentOrChildId);
                    if (!_linesFromNodes.ContainsKey(sourceNode))
                        _linesFromNodes.Add(sourceNode, new List<ConnectionLineUi>());

                    _linesFromNodes[sourceNode].Add(newLine);
                }

                InitTempConnection(newLine);
            }

            private static void InitTempConnection(ConnectionLineUi newLine)
            {
                var c = newLine.Connection;
                if (!ConnectionMaker.TempConnections.Contains(c))
                    return;
                    
                // if (!Equals(newLine.Connection, ConnectionMaker.TempConnections))
                //     return;

                if (c.TargetParentOrChildId == ConnectionMaker.NotConnectedId)
                {
                    if (ConnectionMaker.ConnectionSnapEndHelper.BestMatchLastFrame != null)
                    {
                        newLine.TargetPosition = new Vector2(  ConnectionMaker.ConnectionSnapEndHelper.BestMatchLastFrame.Area.Min.X,
                                                               ConnectionMaker.ConnectionSnapEndHelper.BestMatchLastFrame.Area.GetCenter().Y);
                    }
                    else
                    {
                        newLine.TargetPosition = ImGui.GetMousePos();
                    }

                    newLine.ColorForType = Color.White;
                }
                else if (c.TargetParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    newLine.TargetPosition = GraphCanvas.Current.TransformPosition(SymbolBrowser.Current.PosOnCanvas);
                    newLine.ColorForType = Color.White;
                }
                else if (c.SourceParentOrChildId == ConnectionMaker.NotConnectedId)
                {
                    newLine.SourcePosition = ImGui.GetMousePos();
                    newLine.ColorForType = Color.White;
                }
                else if (c.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    newLine.SourcePosition = SymbolBrowser.Current.OutputPositionOnScreen;
                }
                else
                {
                    Log.Warning("invalid temporary connection?");
                }
            }

            public IEnumerable<ConnectionLineUi> GetLinesFromNodeOutput(SymbolChildUi childUi, Guid outputId)
            {
                return _linesFromNodes.ContainsKey(childUi)
                           ? _linesFromNodes[childUi].FindAll(l => l.Connection.SourceSlotId == outputId)
                           : NoLines;
            }

            public List<ConnectionLineUi> GetLinesToNodeInputSlot(SymbolChildUi childUi, Guid inputId)
            {
                return _linesIntoNodes.ContainsKey(childUi)
                           ? _linesIntoNodes[childUi].FindAll(l => l.Connection.TargetSlotId == inputId)
                           : NoLines;
            }

            public List<ConnectionLineUi> GetLinesIntoNode(SymbolChildUi childUi)
            {
                return _linesIntoNodes.ContainsKey(childUi) ? _linesIntoNodes[childUi] : NoLines;
            }

            public List<ConnectionLineUi> GetLinesToOutputNodes(IOutputUi outputNode, Guid outputId)
            {
                return _linesToOutputNodes.ContainsKey(outputNode)
                           ? _linesToOutputNodes[outputNode].FindAll(l => l.Connection.TargetSlotId == outputId)
                           : NoLines;
            }

            public List<ConnectionLineUi> GetLinesFromInputNodes(IInputUi inputNode, Guid inputNodeId)
            {
                return _linesFromInputNodes.ContainsKey(inputNode)
                           ? _linesFromInputNodes[inputNode].FindAll(l => l.Connection.SourceSlotId == inputNodeId)
                           : NoLines;
            }

            private Dictionary<SymbolChildUi, List<ConnectionLineUi>> _linesFromNodes;
            private Dictionary<SymbolChildUi, List<ConnectionLineUi>> _linesIntoNodes;
            private Dictionary<IOutputUi, List<ConnectionLineUi>> _linesToOutputNodes;
            private Dictionary<IInputUi, List<ConnectionLineUi>> _linesFromInputNodes;

            // Reuse empty list instead of null check
            private static readonly List<ConnectionLineUi> NoLines = new List<ConnectionLineUi>();
        }

        internal class ConnectionLineUi
        {
            public readonly Symbol.Connection Connection;
            public Vector2 TargetPosition;
            public Vector2 SourcePosition;
            public Color ColorForType;

            public bool IsSelected;
            public int UpdateCount;
            public int FramesSinceLastUsage;
            public ImRect SourceNodeArea;
            public ImRect TargetNodeArea;
            public bool IsAboutToBeReplaced;

            internal ConnectionLineUi(Symbol.Connection connection)
            {
                Connection = connection;
                if (!Equals(connection, ConnectionMaker.TempConnections))
                    return;

                if (connection.TargetParentOrChildId == ConnectionMaker.NotConnectedId)
                {
                    TargetPosition = ImGui.GetMousePos();
                }
                else if (connection.TargetParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                }
                else if (connection.SourceParentOrChildId == ConnectionMaker.NotConnectedId)
                {
                    SourcePosition = ImGui.GetMousePos();
                    ColorForType = Color.White;
                }
                else if (connection.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    ColorForType = Color.White;
                    //Log.Debug("temp connection from draft child");
                }
                else
                {
                    Log.Warning("invalid temporary connection?");
                }
            }

            internal void Draw()
            {
                var color = IsSelected
                                ? ColorVariations.Highlight.Apply(ColorForType)
                                : ColorVariations.ConnectionLines.Apply(ColorForType);

                if (IsAboutToBeReplaced)
                    color = Color.Mix(color, Color.Red, (float)Math.Sin(ImGui.GetTime() * 10) / 2 + 0.5f);

                var usageFactor =  Math.Max(0,1- FramesSinceLastUsage/ 50f);
                var thickness = ((1 - 1 / (UpdateCount + 1f)) * 3 + 1) * 0.5f *  (usageFactor * 2 + 1);

                if (UserSettings.Config.UseArcConnections)
                {
                    Im.DrawArcConnection(new ImRect(SourcePosition, SourcePosition + new Vector2(10, 10)),
                                         SourcePosition,
                                         TargetNodeArea,
                                         TargetPosition,
                                         color,
                                         thickness);
                }
                else
                {
                    var tangentLength = MathUtils.Remap(Vector2.Distance(SourcePosition, TargetPosition),
                                                        30, 300,
                                                        5, 200);
                    DrawList.AddBezierCurve(
                                            SourcePosition,
                                            SourcePosition + new Vector2(tangentLength, 0),
                                            TargetPosition + new Vector2(-tangentLength, 0),
                                            TargetPosition,
                                            color,
                                            thickness,
                                            num_segments: 30);
                }
            }
        }

        internal static readonly ConnectionSorter Connections = new ConnectionSorter();
        public static ImDrawListPtr DrawList;
        private static List<SymbolChildUi> _childUis;
        private static SymbolUi _symbolUi;
        private static OrderedDictionary<Guid, IOutputUi> _outputUisById;
        private static OrderedDictionary<Guid, IInputUi> _inputUisById;
    }
}