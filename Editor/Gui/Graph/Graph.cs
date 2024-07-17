using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Truncon.Collections;
// ReSharper disable LoopCanBeConvertedToQuery

namespace T3.Editor.Gui.Graph
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
        public static void DrawGraph(ImDrawListPtr drawList, bool preventInteraction , float graphOpacity= 1)
        {
            
            var needsReinit = false;
            GraphOpacity = graphOpacity; //MathF.Sin((float)ImGui.GetTime() * 2) * 0.5f + 0.5f;
            DrawList = drawList;
            var graphSymbol = GraphCanvas.Current.CompositionOp.Symbol;
            var children = GraphCanvas.Current.CompositionOp.Children;

            _symbolUi = SymbolUiRegistry.Entries[graphSymbol.Id];
            _childUis = _symbolUi.ChildUis;
            _inputUisById = _symbolUi.InputUis;
            _outputUisById = _symbolUi.OutputUis;

            if (ConnectionMaker.TempConnections.Count > 0 || AllConnections.Count != ConnectionMaker.TempConnections.Count + graphSymbol.Connections.Count)
            {
                _lastCheckSum = 0;
                needsReinit = true;
            }

            // Checksum
            if (!needsReinit)
            {
                var checkSum = _updateRequestCount;
                
                for (var index = 0; index < graphSymbol.Connections.Count; index++)
                {
                    var c = graphSymbol.Connections[index];
                    checkSum += c.GetHashCode() * (index+1);
                }

                foreach (var c in ConnectionMaker.TempConnections)
                {
                    checkSum += c.GetHashCode();
                }

                if (checkSum != _lastCheckSum)
                {
                    needsReinit = true;
                    _lastCheckSum = checkSum;
                }
            }

            //needsReinit = true;
            if (needsReinit)
            {
                AllConnections.Clear();
                AllConnections.AddRange(graphSymbol.Connections);
                AllConnections.AddRange(ConnectionMaker.TempConnections);

                // 1. Initializes lists of ConnectionLineUis
                Connections.Init();

                // 2. Collect which nodes are connected to which lines
                foreach (var c in AllConnections)
                {
                    Connections.CreateAndSortLineUi(c);
                }
            }
            else
            {
                if (Connections != null && Connections.Lines != null)
                {
                    foreach (var c in Connections.Lines)
                    {
                        c.IsSelected = false;
                    }
                }
            }

            drawList.ChannelsSplit(2);
            DrawList.ChannelsSetCurrent((int)Channels.Operators);

            // 3. Draw Nodes and their sockets and set positions for connection lines
            for (var childIndex = 0; childIndex < children.Count; childIndex++)
            {
                var instance = children[childIndex];
                if (graphSymbol != GraphCanvas.Current.CompositionOp.Symbol)
                    break;

                foreach (var childUi in _childUis) // Don't use linq to avoid allocations
                {
                    if (childUi.Id != instance.SymbolChildId)
                        continue;

                    GraphNode.Draw(childUi, instance, preventInteraction);
                    break;
                }
            }

            // 4. Draw Inputs Nodes
            if (Connections != null)
            {
                foreach (var (nodeId, node) in _inputUisById)
                {
                    var index = graphSymbol.InputDefinitions.FindIndex(def => def.Id == nodeId);
                    var inputDef = graphSymbol.InputDefinitions[index];
                    var isSelectedOrHovered = InputNode.Draw(inputDef, node, index);

                    var sourcePos = new Vector2(
                                                InputNode._lastScreenRect.Max.X + GraphNode.UsableSlotThickness,
                                                InputNode._lastScreenRect.GetCenter().Y
                                               );
                    foreach (var line in Connections.GetLinesFromInputNodes(node, nodeId))
                    {
                        line.SourcePosition = sourcePos;
                        line.IsSelected |= isSelectedOrHovered;
                    }
                }
            }

            // 5. Draw Output Nodes
            foreach (var (outputId, outputNode) in _outputUisById)
            {
                var outputDef = graphSymbol.OutputDefinitions.Find(od => od.Id == outputId);
                OutputNode.Draw(outputDef, outputNode);

                var targetPos = new Vector2(OutputNode.LastScreenRect.Min.X + GraphNode.InputSlotThickness,
                                            OutputNode.LastScreenRect.GetCenter().Y);

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

            // 7. Draw Annotations
            drawList.ChannelsSetCurrent((int)Channels.Annotations);
            foreach (var annotation in _symbolUi.Annotations.Values)
            {
                //var posOnScreen = GraphCanvas.Current.TransformPosition(annotation.Position);
                //drawList.AddRectFilled(  posOnScreen, posOnScreen + new Vector2(300,300), Color.Green);
                AnnotationElement.Draw(annotation);
            }

            drawList.ChannelsMerge();
        }

        internal class ConnectionSorter
        {
            public readonly List<ConnectionLineUi> Lines = new();

            public void Init()
            {
                Lines.Clear();
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
                    if (!_outputUisById.TryGetValue(c.TargetSlotId, out var outputNode))
                        return;
                    
                    if (!_linesToOutputNodes.ContainsKey(outputNode))
                        _linesToOutputNodes.Add(outputNode, new List<ConnectionLineUi>());

                    _linesToOutputNodes[outputNode].Add(newLine);
                }
                else if (c.TargetParentOrChildId != ConnectionMaker.NotConnectedId
                         && c.TargetParentOrChildId != ConnectionMaker.UseDraftChildId)
                {
                    var targetNode = _childUis.SingleOrDefault(childUi => childUi.Id == c.TargetParentOrChildId);
                    if (targetNode == null)
                        return;
                    
                    if (!_linesIntoNodes.ContainsKey(targetNode))
                        _linesIntoNodes.Add(targetNode, new List<ConnectionLineUi>());

                    _linesIntoNodes[targetNode].Add(newLine);
                }

                if (c.IsConnectedToSymbolInput)
                {
                    if (!_inputUisById.TryGetValue(c.SourceSlotId, out var inputNode))
                        return;
                    
                    if (!_linesFromInputNodes.ContainsKey(inputNode))
                        _linesFromInputNodes.Add(inputNode, new List<ConnectionLineUi>());

                    _linesFromInputNodes[inputNode].Add(newLine);

                    var color = UiColors.Gray;
                    if (TypeUiRegistry.Entries.TryGetValue(inputNode.Type, out var typeUiProperties))
                        color = typeUiProperties.Color;
                    
                    newLine.ColorForType = color;
                }
                else if (c.SourceParentOrChildId != ConnectionMaker.NotConnectedId
                         && c.SourceParentOrChildId != ConnectionMaker.UseDraftChildId)
                {
                    var sourceNode = _childUis.SingleOrDefault(childUi => childUi.Id == c.SourceParentOrChildId);
                    if (sourceNode == null)
                        return;

                    if (!_linesFromNodes.ContainsKey(sourceNode))
                        _linesFromNodes.Add(sourceNode, new List<ConnectionLineUi>());

                    _linesFromNodes[sourceNode].Add(newLine);
                }

                InitTempConnection(newLine);
            }

            private static void InitTempConnection(ConnectionLineUi newLine)
            {
                if (!(newLine.Connection is ConnectionMaker.TempConnection c))
                    return;

                newLine.ColorForType = TypeUiRegistry.Entries[c.ConnectionType].Color;

                // if (!ConnectionMaker.TempConnections.Contains(c))
                //     return;

                // if (!Equals(newLine.Connection, ConnectionMaker.TempConnections))
                //     return;

                if (c.TargetParentOrChildId == ConnectionMaker.NotConnectedId)
                {
                    if (ConnectionSnapEndHelper.BestMatchLastFrame != null)
                    {
                        newLine.TargetPosition = new Vector2(ConnectionSnapEndHelper.BestMatchLastFrame.Area.Min.X,
                                                             ConnectionSnapEndHelper.BestMatchLastFrame.Area.GetCenter().Y);
                    }
                    else
                    {
                        newLine.TargetPosition = ImGui.GetMousePos();
                    }

                    newLine.ColorForType = Color.White;
                }
                else if (c.TargetParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    newLine.TargetPosition = GraphCanvas.Current.TransformPosition(GraphCanvas.Current.SymbolBrowser.PosOnCanvas);
                    //newLine.ColorForType = Color.White;
                }
                else if (c.SourceParentOrChildId == ConnectionMaker.NotConnectedId)
                {
                    newLine.SourcePosition = ImGui.GetMousePos();
                    //newLine.ColorForType = Color.White;
                }
                else if (c.SourceParentOrChildId == ConnectionMaker.UseDraftChildId)
                {
                    newLine.SourcePosition = GraphCanvas.Current.SymbolBrowser.OutputPositionOnScreen;
                }
                else
                {
                    Log.Warning("invalid temporary connection?");
                }
            }

            private static readonly List<ConnectionLineUi> _resultConnection = new(20);

            public List<ConnectionLineUi> GetLinesFromNodeOutput(SymbolChildUi childUi, Guid outputId)
            {
                _resultConnection.Clear();

                if (!_linesFromNodes.TryGetValue(childUi, out var lines))
                    return NoLines;

                foreach (var l in lines)
                {
                    if (l.Connection.SourceSlotId != outputId)
                        continue;

                    _resultConnection.Add(l);
                }

                return _resultConnection;
            }

            public List<ConnectionLineUi> GetLinesToNodeInputSlot(SymbolChildUi childUi, Guid inputId)
            {
                _resultConnection.Clear();
                if (!_linesIntoNodes.TryGetValue(childUi, out var lines))
                    return NoLines;

                foreach (var l in lines)
                {
                    if (l.Connection.TargetSlotId != inputId)
                        continue;
                    _resultConnection.Add(l);
                }

                return _resultConnection;
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

            private Dictionary<SymbolChildUi, List<ConnectionLineUi>> _linesFromNodes = new(50);
            private Dictionary<SymbolChildUi, List<ConnectionLineUi>> _linesIntoNodes = new(50);
            private Dictionary<IOutputUi, List<ConnectionLineUi>> _linesToOutputNodes = new(50);
            private Dictionary<IInputUi, List<ConnectionLineUi>> _linesFromInputNodes = new(50);

            // Reuse empty list instead of null check
            private static readonly List<ConnectionLineUi> NoLines = new();
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
            }

            internal void Draw()
            {
                var color = IsSelected
                                ? ColorVariations.Highlight.Apply(ColorForType)
                                : ColorVariations.ConnectionLines.Apply(ColorForType);

                if (IsAboutToBeReplaced)
                    color = Color.Mix(color, UiColors.StatusAttention, (float)Math.Sin(ImGui.GetTime() * 15) / 2 + 0.5f);

                if (!IsSelected)
                    color = color.Fade(0.6f);

                var usageFactor = Math.Max(0, 1 - FramesSinceLastUsage / 50f);
                var thickness = ((1 - 1 / (UpdateCount + 1f)) * 3 + 1) * 0.5f * (usageFactor * 2 + 1);

                if (UserSettings.Config.UseArcConnections)
                {
                    var hoverPositionOnLine = Vector2.Zero;
                    var isHovering = ArcConnection.Draw(new ImRect(SourcePosition, SourcePosition + new Vector2(10, 10)),
                                                        SourcePosition,
                                                        TargetNodeArea,
                                                        TargetPosition,
                                                        color,
                                                        thickness,
                                                        ref hoverPositionOnLine);

                    const float minDistanceToTargetSocket = 10;
                    if (isHovering && Vector2.Distance(hoverPositionOnLine, TargetPosition) > minDistanceToTargetSocket
                                   && Vector2.Distance(hoverPositionOnLine, SourcePosition) > minDistanceToTargetSocket)
                    {
                        ConnectionSplitHelper.RegisterAsPotentialSplit(Connection, ColorForType, hoverPositionOnLine);
                    }
                }
                else
                {
                    var tangentLength = MathUtils.RemapAndClamp(Vector2.Distance(SourcePosition, TargetPosition),
                                                                30, 300,
                                                                5, 200);

                    DrawList.AddBezierCubic(
                                            SourcePosition,
                                            SourcePosition + new Vector2(tangentLength, 0),
                                            TargetPosition + new Vector2(-tangentLength, 0),
                                            TargetPosition,
                                            color,
                                            thickness,
                                            num_segments: 20);
                }
            }
        }
        
        public static void RequestUpdate()
        {
            _updateRequestCount++;
        }
        
        private enum Channels
        {
            Annotations = 0,
            Operators = 1,
        }

        public static float GraphOpacity = 0.2f;
        public static int _updateRequestCount=0;
        
        private static int _lastCheckSum;
        internal static readonly ConnectionSorter Connections = new();
        public static ImDrawListPtr DrawList;
        private static List<SymbolChildUi> _childUis;
        private static SymbolUi _symbolUi;
        private static OrderedDictionary<Guid, IOutputUi> _outputUisById;
        private static OrderedDictionary<Guid, IInputUi> _inputUisById;

        // Try to avoid allocations
        private static readonly List<Symbol.Connection> AllConnections = new(100);
    }
}