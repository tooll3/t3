#nullable enable

using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable UseWithExpressionToCopyStruct

namespace T3.Editor.Gui.MagGraph.Ui;


/// <summary>
/// 
/// </summary>
/// <remarks>
/// Things would be slightly more efficient if this would would use SnapGraphItems. However this would
/// prevent us from reusing fence selection. Enforcing this to be used for dragging inputs and outputs
/// makes this class unnecessarily complex.
/// </remarks>
internal sealed class MagItemMovement
{
    public MagItemMovement(MagGraphCanvas magGraphCanvas, MagGraphLayout layout, NodeSelection nodeSelection)
    {
        _canvas = magGraphCanvas;
        _layout = layout;
        _nodeSelection = nodeSelection;
    }



    /// <summary>
    /// For certain edge cases like the release handling of nodes cannot be detected.
    /// This is a work around to clear the state on mouse release
    /// </summary>
    // Todo: Call this from the main loop?
    public static void CompleteFrame()
    {
        if (ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            Reset();
        }
    }

    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public void HandleForItem(MagGraphItem item, MagGraphCanvas canvas, Instance composition)
    {
        var justClicked = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        //var composition = item.Instance?.Parent;
        //Debug.Assert(composition != null, "Composition not found");

        var isActiveNode = item.Id == _draggedNodeId;
        if (justClicked)
        {
            _longTapItem = item;
            _draggedNodeId = item.Id;
            if (IsItemSelected(item))
            {
                _draggedNodes.Clear();
                foreach (var s in _nodeSelection.Selection)
                {
                    if (_layout.Items.TryGetValue(s.Id, out var i))
                    {
                        _draggedNodes.Add(i);
                    }
                }
            }
            else
            {
                _draggedNodes.Clear();
                CollectSnappedItems(item);
            }

            _mousePressedTime = ImGui.GetTime();

            StartDragging(_draggedNodes);

            var snapGraphItems = _draggedNodes.Select(i => i as ISelectableCanvasObject).ToList();
            _modifyCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems, _nodeSelection);
            //ShakeDetector.ResetShaking();
        }
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _modifyCommand != null)
        {
            // TODO: Implement shake disconnect later
            HandleNodeDragging(canvas, composition);
        }
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            if (_draggedNodeId != item.Id)
                return;

            var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes.First() : null;
            _draggedNodeId = Guid.Empty;
            _draggedNodes.Clear();

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                _modifyCommand.StoreCurrentValues();

                if (singleDraggedNode != null
                    && ConnectionSplitHelper.BestMatchLastFrame != null
                    && singleDraggedNode is MagGraphItem graphItem)
                {
                    //var instanceForSymbolChildUi = composition.Children.SingleOrDefault(child => child.SymbolChildId == graphItem.SymbolChildUi.Id);
                    // TODO: Implement later
                    // ConnectionMaker.SplitConnectionWithDraggedNode(graphItem.SymbolChildUi,
                    //                                                ConnectionSplitHelper.BestMatchLastFrame.Connection,
                    //                                                graphItem.Instance,
                    //                                                _modifyCommand);
                    _modifyCommand = null;
                }
                else
                {
                    UndoRedoStack.Add(_modifyCommand);
                }

                // Reorder input nodes if dragged
                var selectedInputs = _nodeSelection.GetSelectedNodes<IInputUi>().ToList();
                if (selectedInputs.Count > 0)
                {
                    //var compositionUi = SymbolUiRegistry.Entries[composition.Symbol.Id];
                    if (!SymbolUiRegistry.TryGetSymbolUi(composition.Symbol.Id, out var compositionUi))
                        return;

                    composition.Symbol.InputDefinitions.Sort((a, b) =>
                                                             {
                                                                 var childA = compositionUi.InputUis[a.Id];
                                                                 var childB = compositionUi.InputUis[b.Id];
                                                                 return (int)(childA.PosOnCanvas.Y * 10000 + childA.PosOnCanvas.X) -
                                                                        (int)(childB.PosOnCanvas.Y * 10000 + childB.PosOnCanvas.X);
                                                             });
                    composition.Symbol.SortInputSlotsByDefinitionOrder();
                    InputsAndOutputs.AdjustInputOrderOfSymbol(composition.Symbol);
                }
            }
            else
            {
                if (!_nodeSelection.IsNodeSelected(item))
                {
                    var replaceSelection = !ImGui.GetIO().KeyShift;
                    if (replaceSelection)
                    {
                        item.Select(_nodeSelection);
                    }
                    else
                    {
                        item.AddToSelection(_nodeSelection);
                    }
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        _nodeSelection.DeselectNode(item, item.Instance);
                    }
                }
            }

            _modifyCommand = null;
        }
        else if (ImGui.IsMouseReleased(0) && _modifyCommand == null)
        {
            // This happens after shake
            _draggedNodes.Clear();
        }

        var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
            && !wasDraggingRight
            && ImGui.IsItemHovered()
            && !_nodeSelection.IsNodeSelected(item))
        {
            item.Select(_nodeSelection);
        }
    }

    private void StartDragging(HashSet<MagGraphItem> draggedNodes)
    {
        //_currentAppliedSnapOffset = Vector2.Zero;
        _lastAppliedOffset = Vector2.Zero;
        UpdateDragConnectionOnStart(draggedNodes);
    }

    private void HandleNodeDragging(ICanvas canvas, Instance composition)
    {
        var dl = ImGui.GetWindowDrawList();
        if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            {
                var timeSincePress = ImGui.GetTime() - _mousePressedTime;
                const float longTapDuration = 0.3f;
                var longTapProgress = (float)(timeSincePress / longTapDuration);
                if (longTapProgress < 1)
                {
                    dl.AddCircle(ImGui.GetMousePos(), 100 * (1 - longTapProgress), Color.White.Fade(MathF.Pow(longTapProgress, 3)));
                }
                else if (_longTapItem != null)
                {
                    _longTapItem.Select(_nodeSelection);
                    _draggedNodes.Clear();
                    _draggedNodes.Add(_longTapItem);
                    var snapGraphItems = _draggedNodes.Select(i => i as ISelectableCanvasObject).ToList();
                    _modifyCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems, _nodeSelection);
                }
            }

            _isDragging = false;
            return;
        }

        if (!_isDragging)
        {
            //Log.Debug("Dragging has begun");
            _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            _isDragging = true;
        }

        var showDebug = ImGui.GetIO().KeyCtrl;
        var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        var requestedDeltaOnCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

        var dragExtend = MagGraphItem.GetItemSetBounds(_draggedNodes);
        dragExtend.Expand(SnapThreshold * canvas.Scale.X);

        if (showDebug)
        {
            dl.AddCircle(_canvas.TransformPosition(_dragStartPosInOpOnCanvas), 10, Color.Blue);

            dl.AddLine(_canvas.TransformPosition(_dragStartPosInOpOnCanvas),
                       _canvas.TransformPosition(_dragStartPosInOpOnCanvas + requestedDeltaOnCanvas), Color.Blue);

            dl.AddRect(_canvas.TransformPosition(dragExtend.Min),
                       _canvas.TransformPosition(dragExtend.Max),
                       Color.Green.Fade(0.1f));
        }

        var overlappingItems = new List<MagGraphItem>();
        foreach (var otherItem in _layout.Items.Values)
        {
            if (_draggedNodes.Contains(otherItem) || !dragExtend.Overlaps(otherItem.Area))
                continue;

            overlappingItems.Add(otherItem);
        }
        
        // Move back to non-snapped position
        foreach (var n in _draggedNodes)
        {
            n.PosOnCanvas -= _lastAppliedOffset; // Move to position
            n.PosOnCanvas += requestedDeltaOnCanvas; // Move to request position
        }

        _lastAppliedOffset = requestedDeltaOnCanvas;

        var snapping = new Snapping { BestDistance = float.PositiveInfinity };

        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in _draggedNodes)
            {
                snapping.TestForSnap(otherItem, draggedItem, false);
                snapping.TestForSnap(draggedItem, otherItem, true);
            }
        }

        // Snapped
        if (snapping.BestDistance < MagGraphItem.LineHeight * 0.5f)
        {
            var bestSnapDelta = !snapping.Reverse
                                    ? snapping.OutAnchorPos - snapping.InputAnchorPos
                                    : snapping.InputAnchorPos - snapping.OutAnchorPos;

            dl.AddLine(_canvas.TransformPosition(mousePosOnCanvas),
                       canvas.TransformPosition(mousePosOnCanvas) + _canvas.TransformDirection(bestSnapDelta),
                       Color.White);

            if (Vector2.Distance(snapping.InputAnchorPos, LastSnapPositionOnCanvas) > 2)
            {
                LastSnapTime = ImGui.GetTime();
                LastSnapPositionOnCanvas = snapping.InputAnchorPos;
            }

            foreach (var n in _draggedNodes)
            {
                n.PosOnCanvas += bestSnapDelta;
            }

            _lastAppliedOffset += bestSnapDelta;
        }
        else
        {
            LastSnapPositionOnCanvas = Vector2.Zero;
            LastSnapTime = double.NegativeInfinity;
        }
    }

    private void UpdateDragConnectionOnStart(HashSet<MagGraphItem> draggedItems)
    {
        _bridgeConnectionsOnStart.Clear();
        foreach (var c in _layout.SnapConnections)
        {
            var targetDragged = draggedItems.Contains(c.TargetItem);
            var sourceDragged = draggedItems.Contains(c.TargetItem);
            if (targetDragged != sourceDragged)
            {
                _bridgeConnectionsOnStart.Add(c);
            }
        }
    }

    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private struct Snapping
    {
        public float BestDistance;
        public MagGraphItem BestA;
        public MagGraphItem BestB;
        public MagGraphItem.Directions Direction;
        public int InputLineIndex;
        public int MultiInputIndex;
        public int OutLineIndex;
        public Vector2 OutAnchorPos;
        public Vector2 InputAnchorPos;
        public bool Reverse;

        public void TestForSnap(MagGraphItem a, MagGraphItem b, bool revert)
        {
            MultiInputIndex = 0;
            for (var bInputLineIndex = 0; bInputLineIndex < b.InputLines.Length; bInputLineIndex++)
            {
                ref var bInputLine = ref b.InputLines[bInputLineIndex];
                var inConnection = bInputLine.ConnectionIn;

                for (var aOutLineIndex = 0; aOutLineIndex < a.OutputLines.Length; aOutLineIndex++)
                {
                    ref var ol = ref a.OutputLines[aOutLineIndex];

                    // A -> B vertical
                    if (aOutLineIndex == 0 && bInputLineIndex == 0)
                    {
                        // If input is connected the only valid output is the one with the connection line

                        var outPos = new Vector2(a.Area.Min.X + MagGraphItem.WidthHalf, a.Area.Max.Y);
                        var inPos = new Vector2(b.Area.Min.X + MagGraphItem.WidthHalf, b.Area.Min.Y);

                        if (inConnection != null)
                        {
                            foreach (var c in ol.ConnectionsOut)
                            {
                                if (c != inConnection)
                                    continue;

                                if (TestAndKeepPositionsForSnapping(outPos, inPos))
                                {
                                    Direction = MagGraphItem.Directions.Vertical;
                                    OutLineIndex = aOutLineIndex;
                                    InputLineIndex = bInputLineIndex;
                                    MultiInputIndex = 0;
                                    BestA = a;
                                    BestB = b;
                                    Reverse = revert;
                                }
                            }
                        }
                        else
                        {
                            if (TestAndKeepPositionsForSnapping(outPos, inPos))
                            {
                                Direction = MagGraphItem.Directions.Vertical;
                                OutLineIndex = aOutLineIndex;
                                InputLineIndex = bInputLineIndex;
                                MultiInputIndex = 0;
                                BestA = a;
                                BestB = b;
                                Reverse = revert;
                            }
                        }
                    }

                    // // A -> B horizontally
                    {
                        var outPos = new Vector2(a.Area.Max.X, a.Area.Min.Y + (0.5f + ol.VisibleIndex) * MagGraphItem.LineHeight);
                        var inPos = new Vector2(b.Area.Min.X, b.Area.Min.Y + (0.5f + bInputLine.VisibleIndex) * MagGraphItem.LineHeight);

                        if (TestAndKeepPositionsForSnapping(outPos, inPos))
                        {
                            Direction = MagGraphItem.Directions.Horizontal;
                            OutLineIndex = aOutLineIndex;
                            InputLineIndex = bInputLineIndex;
                            MultiInputIndex = bInputLine.MultiInputIndex;
                            BestA = a;
                            BestB = b;
                            Reverse = revert;
                        }
                    }
                }
            }

            Direction = MagGraphItem.Directions.Horizontal;
        }

        private bool TestAndKeepPositionsForSnapping(Vector2 outPos, Vector2 inputPos)
        {
            var d = Vector2.Distance(outPos, inputPos);
            if (d >= BestDistance)
                return false;

            BestDistance = d;
            OutAnchorPos = outPos;
            InputAnchorPos = inputPos;
            return true;
        }
    }

    private static void CollectSnappedItems(MagGraphItem item)
    {
        if (!_draggedNodes.Add(item))
            return;

        for (var index = 0; index < item.InputLines.Length; index++)
        {
            var c = item.InputLines[index].ConnectionIn;
            if (c == null)
                continue;

            if (c.IsSnapped)
                CollectSnappedItems(c.SourceItem);
        }

        for (var index = 0; index < item.OutputLines.Length; index++)
        {
            var connections = item.OutputLines[index].ConnectionsOut;
            foreach (var c in connections)
            {
                if (c.IsSnapped)
                    CollectSnappedItems(c.TargetItem);
            }
        }
    }

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    private static void Reset()
    {
        _modifyCommand = null;
        _draggedNodes.Clear();
    }
    
    private bool IsItemSelected(ISelectableCanvasObject item) => _nodeSelection.IsNodeSelected(item);

    public double LastSnapTime = double.NegativeInfinity;
    public Vector2 LastSnapPositionOnCanvas;

    private Vector2 _lastAppliedOffset;

    private const float SnapThreshold = 10;
    private readonly List<MagGraphConnection> _bridgeConnectionsOnStart = [];

    private MagGraphItem? _longTapItem;
    private double _mousePressedTime;

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;
    private static ModifyCanvasElementsCommand? _modifyCommand;
    private static Guid _draggedNodeId = Guid.Empty;
    private static readonly HashSet<MagGraphItem> _draggedNodes = [];

    private readonly MagGraphCanvas _canvas;
    private readonly MagGraphLayout _layout;
    private readonly NodeSelection _nodeSelection;
}