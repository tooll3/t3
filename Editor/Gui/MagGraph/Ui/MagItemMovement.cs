#nullable enable

using System.Diagnostics.CodeAnalysis;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

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
    public void CompleteFrame()
    {
        if (ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            Reset();
        }
    }
    
    private Vector2 _lastPosForDebug = Vector2.Zero;
    private CircularBuffer<Vector2> _lastPositions = new(100);

    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public void HandleForItem(MagGraphItem item, MagGraphCanvas canvas, Instance composition)
    {
        // Some debugging for drag behaviour...
        // if (item.SymbolUi != null && item.Instance != null && item.SymbolUi.Symbol.Name == "Time")
        // {
        //     //var parentUi = item.SymbolUi.Parent;
        //     if(SymbolUiRegistry.TryGetSymbolUi(item.Instance.Parent.Symbol.Id, out var parentSymbolUi))
        //     {
        //         var symbolChildUi = parentSymbolUi.ChildUis[item.Instance.SymbolChildId];
        //         if (item.PosOnCanvas != _lastPosForDebug)
        //         {
        //             _lastPositions.Enqueue(item.PosOnCanvas);
        //             //Log.Debug($"{_lastAppliedOffset}  {item.PosOnCanvas}  {symbolChildUi.PosOnCanvas}");
        //             _lastPosForDebug = item.PosOnCanvas;
        //         }
        //
        //         var drawList = ImGui.GetForegroundDrawList();
        //         foreach (var p in _lastPositions.ToArray())
        //         {
        //             drawList.AddCircle(canvas.TransformPosition(p), 5, Color.Red);
        //         }
        //     }  
        // }
        
        var isActiveNode = item.Id == _draggedNodeId;
        var clickedDown = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        if (clickedDown)
        {
            _longTapItem = item;
            _draggedNodeId = item.Id;
            if (item.IsSelected(_nodeSelection))
            {
                _draggedItems.Clear();
                foreach (var s in _nodeSelection.Selection)
                {
                    if (_layout.Items.TryGetValue(s.Id, out var i))
                    {
                        _draggedItems.Add(i);
                    }
                }
            }
            else
            {
                _draggedItems.Clear();
                CollectSnappedDragItems(item);
            }

            _mousePressedTime = ImGui.GetTime();

            StartDragging(_draggedItems);

            var snapGraphItems = _draggedItems.Select(i => i as ISelectableCanvasObject).ToList();
            _modifyCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems, _nodeSelection);
            //ShakeDetector.ResetShaking();
        }
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left) && _modifyCommand != null)
        {
            // TODO: Implement shake disconnect later
            HandleSnappedDragging(canvas, composition);
        }
        else if (isActiveNode && ImGui.IsMouseReleased(0) && _modifyCommand != null)
        {
            if (_draggedNodeId != item.Id)
                return;

            var singleDraggedNode = (_draggedItems.Count == 1) ? _draggedItems.First() : null;
            _draggedNodeId = Guid.Empty;
            _draggedItems.Clear();
            _lastAppliedOffset = Vector2.Zero;

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
            _draggedItems.Clear();
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
        DraggedPrimaryInputType = null;
        DraggedPrimaryOutputType = null;
        
        _lastAppliedOffset = Vector2.Zero;
        _isDragging = false;
        UpdateBorderConnections(draggedNodes);

        // Set primary types to indicate targets for dragging
        if (_draggedItems.Count != 1)
            return;
        
        var item = _draggedItems.First();
        DraggedPrimaryOutputType = item.PrimaryType;
            
        if(item.InputLines.Length == 0)
            return;
            
        DraggedPrimaryInputType = item.InputLines[0].Type;
    }

    private void HandleSnappedDragging(ICanvas canvas, Instance composition)
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
                    _draggedItems.Clear();
                    _draggedItems.Add(_longTapItem);
                    StartDragging(_draggedItems);
                    var snapGraphItems = _draggedItems.Select(i => i as ISelectableCanvasObject).ToList();
                    _modifyCommand = new ModifyCanvasElementsCommand(composition.Symbol.Id, snapGraphItems, _nodeSelection);
                }
            }
        
            _isDragging = false;
            return;
        }

        if (!_isDragging)
        {
            _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
            _isDragging = true;
        }

        var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        var requestedDeltaOnCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;

        var dragExtend = MagGraphItem.GetItemSetBounds(_draggedItems);
        dragExtend.Expand(SnapThreshold * canvas.Scale.X);

        if (_canvas.ShowDebug)
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
            if (_draggedItems.Contains(otherItem) || !dragExtend.Overlaps(otherItem.Area))
                continue;

            overlappingItems.Add(otherItem);
        }

        // Move back to non-snapped position
        foreach (var n in _draggedItems)
        {
            n.PosOnCanvas -= _lastAppliedOffset; // Move to position
            n.PosOnCanvas += requestedDeltaOnCanvas; // Move to request position
        }

        _lastAppliedOffset = requestedDeltaOnCanvas;

        var snapping = new Snapping { BestDistance = float.PositiveInfinity };

        foreach (var otherItem in overlappingItems)
        {
            foreach (var draggedItem in _draggedItems)
            {
                snapping.TestItemsForSnap(otherItem, draggedItem, false, _canvas);
                snapping.TestItemsForSnap(draggedItem, otherItem, true, _canvas);
            }
        }

        // Highlight best distance
        if (_canvas.ShowDebug)
        {
            if (snapping.BestDistance < 500)
            {
                var p1 = _canvas.TransformPosition(snapping.OutAnchorPos);
                var p2 = _canvas.TransformPosition(snapping.InputAnchorPos);
                dl.AddLine(p1, p2, UiColors.ForegroundFull.Fade(0.1f), 6);
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

            foreach (var n in _draggedItems)
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

    private void UpdateBorderConnections(HashSet<MagGraphItem> draggedItems)
    {
        _borderConnections.Clear();
        foreach (var c in _layout.MagConnections)
        {
            var targetDragged = draggedItems.Contains(c.TargetItem);
            var sourceDragged = draggedItems.Contains(c.SourceItem);
            if (targetDragged != sourceDragged)
            {
                _borderConnections.Add(c);
            }
        }
        //Log.Debug($" Found {_borderConnections.Count} bridge connections in {draggedItems.Count}" );
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

        public void TestItemsForSnap(MagGraphItem a, MagGraphItem b, bool revert, MagGraphCanvas canvas)
        {
            MultiInputIndex = 0;
            for (var bInputLineIndex = 0; bInputLineIndex < b.InputLines.Length; bInputLineIndex++)
            {
                ref var bInputLine = ref b.InputLines[bInputLineIndex];
                var inConnection = bInputLine.ConnectionIn;

                for (var aOutLineIndex = 0; aOutLineIndex < a.OutputLines.Length; aOutLineIndex++)
                {
                    ref var ol = ref a.OutputLines[aOutLineIndex];
                    if (bInputLine.Type != ol.Output.ValueType)
                        continue;

                    // A -> B vertical
                    if (aOutLineIndex == 0 && bInputLineIndex == 0)
                    {
                        var outPos = new Vector2(a.Area.Min.X + MagGraphItem.WidthHalf, a.Area.Max.Y);
                        var inPos = new Vector2(b.Area.Min.X + MagGraphItem.WidthHalf, b.Area.Min.Y);

                        // If input is connected the only valid output is the one with the connection line
                        if (inConnection != null)
                        {
                            foreach (var c in ol.ConnectionsOut)
                            {
                                if (c != inConnection)
                                    continue;

                                ShowDebugLine(outPos, inPos, a.PrimaryType);

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
                            ShowDebugLine(outPos, inPos, a.PrimaryType);

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
                    if (ol.Output.ValueType == bInputLine.Input.ValueType)
                    {
                        var outPos = new Vector2(a.Area.Max.X, a.Area.Min.Y + (0.5f + ol.VisibleIndex) * MagGraphItem.LineHeight);
                        var inPos = new Vector2(b.Area.Min.X, b.Area.Min.Y + (0.5f + bInputLine.VisibleIndex) * MagGraphItem.LineHeight);

                        if (inConnection != null)
                        {
                            foreach (var c in ol.ConnectionsOut)
                            {
                                if (c != inConnection)
                                    continue;

                                ShowDebugLine(outPos, inPos, a.PrimaryType);

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
                        else
                        {
                            ShowDebugLine(outPos, inPos, a.PrimaryType);

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
            }

            Direction = MagGraphItem.Directions.Horizontal;
            return;

            void ShowDebugLine(Vector2 outPos, Vector2 inPos, Type connectionType)
            {
                if (!canvas.ShowDebug)
                    return;

                var drawList = ImGui.GetForegroundDrawList();
                var uiPrimaryColor = TypeUiRegistry.GetPropertiesForType(connectionType).Color;
                drawList.AddLine(canvas.TransformPosition(outPos),
                                 canvas.TransformPosition(inPos),
                                 uiPrimaryColor.Fade(0.4f));

                drawList.AddCircleFilled(canvas.TransformPosition(inPos), 6, uiPrimaryColor.Fade(0.4f));
            }
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
    
    private static void CollectSnappedDragItems(MagGraphItem rootItem)
    {
        var collection = _draggedItems;
        Collect(rootItem);
        return;

        void Collect(MagGraphItem item)
        {
            if (!collection.Add(item))
                return;
            
            for (var index = 0; index < item.InputLines.Length; index++)
            {
                var c = item.InputLines[index].ConnectionIn;
                if (c == null)
                    continue;

                if (c.IsSnapped)
                    Collect(c.SourceItem);
            }

            for (var index = 0; index < item.OutputLines.Length; index++)
            {
                var connections = item.OutputLines[index].ConnectionsOut;
                foreach (var c in connections)
                {
                    if (c.IsSnapped)
                        Collect(c.TargetItem);
                }
            }
        }
    }
    
    public bool IsItemDragged(MagGraphItem item) => _draggedItems.Contains(item);
    

    /// <summary>
    /// Reset to avoid accidental dragging of previous elements 
    /// </summary>
    private void Reset()
    {
        _modifyCommand = null;
        _lastAppliedOffset = Vector2.Zero;
        _draggedItems.Clear();
    }

    public double LastSnapTime = double.NegativeInfinity;
    public Vector2 LastSnapPositionOnCanvas;

    private Vector2 _lastAppliedOffset;

    private const float SnapThreshold = 30;
    private readonly List<MagGraphConnection> _borderConnections = [];

    private MagGraphItem? _longTapItem;
    private double _mousePressedTime;

    private static bool _isDragging;
    private Vector2 _dragStartPosInOpOnCanvas;
    private static ModifyCanvasElementsCommand? _modifyCommand;
    private static Guid _draggedNodeId = Guid.Empty;
    private static readonly HashSet<MagGraphItem> _draggedItems = [];

    public Type? DraggedPrimaryOutputType;
    public Type? DraggedPrimaryInputType;
    private readonly MagGraphCanvas _canvas;
    private readonly MagGraphLayout _layout;
    private readonly NodeSelection _nodeSelection;
}