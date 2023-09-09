using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

public class ResearchWindow : Window
{
    public ResearchWindow()
    {
        Config.Title = "Research";
        MenuTitle = "Research";
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    }

    protected override void DrawContent()
    {
        DrawWindowContent();
    }

    private bool _initialized;
    private Vector2 _dampedMovePos;

    private void DrawWindowContent(bool hideHeader = false)
    {
        if (ImGui.Button("Reinit") || !_initialized)
        {
            //InitializeFromSymbol();
            InitializeFromDefinition();
        }

        var drawList = ImGui.GetWindowDrawList();

        // move test block
        var posOnCanvas = _canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        //_movingTestBlock.PosOnCanvas = posOnCanvas;

        // snap test block
        var foundSnapPos = false;
        var bestSnapDistance = float.PositiveInfinity;
        var bestSnapPos = Vector2.Zero;
        var snapThreshold = 20;

        foreach (var movingBlockSlot in _movingTestBlock.GetSlots())
        {
            var slotPosA = movingBlockSlot.Block.PosOnCanvas + movingBlockSlot.AnchorPos * BlockSize;

            foreach (var other in _blocks)
            {
                if (other == _movingTestBlock)
                    continue;

                var otherSlots = movingBlockSlot.IsInput ? other.Outputs : other.Inputs;
                foreach (var otherSlot in otherSlots)
                {
                    var otherSlotPos = other.PosOnCanvas + otherSlot.AnchorPos * BlockSize;
                    var delta = slotPosA - otherSlotPos;
                    var distance = delta.Length();
                    if (distance > snapThreshold)
                        continue;

                    if (distance < bestSnapDistance)
                    {
                        bestSnapDistance = distance;
                        bestSnapPos = other.PosOnCanvas - (movingBlockSlot.AnchorPos - otherSlot.AnchorPos) * BlockSize;
                        foundSnapPos = true;
                    }
                }
            }
        }

        if (foundSnapPos)
        {
            _dampedMovePos = Vector2.Lerp(_dampedMovePos, bestSnapPos, 0.5f);
            _movingTestBlock.PosOnCanvas = _dampedMovePos;
        }
        else
        {
            //_dampedMovePos = _movingTestBlock.PosOnCanvas;
            //_movingTestBlock.PosOnCanvas = posOnCanvas;
        }

        _canvas.UpdateCanvas();
        HandleFenceSelection();

        var anchorScale = _canvas.TransformDirection(BlockSize);
        var slotSize = 3 * _canvas.Scale.X;

        foreach (var b in _blocks)
        {
            if (!TypeUiRegistry.Entries.TryGetValue(b.PrimaryType, out var typeUiProperties))
                continue;

            var c = typeUiProperties.Color;
            var cLabel = ColorVariations.OperatorLabel.Apply(c);

            var pMin = _canvas.TransformPosition(b.PosOnCanvas);
            var pMax = _canvas.TransformPosition(b.PosOnCanvas + BlockSize);
            drawList.AddRectFilled(pMin, pMax, c.Fade(0.7f), 3);
            
            // Outline

            var outlineColor = DragHandling.IsNodeSelected(b) ? UiColors.ForegroundFull 
                                   : UiColors.BackgroundFull.Fade(0.6f);
            drawList.AddRect(pMin, pMax, outlineColor, 3);
            drawList.AddText(pMin + new Vector2(2, -2), cLabel, b.Name);
            ImGui.SetCursorScreenPos(pMin);
            ImGui.InvisibleButton(b.Id.ToString(), anchorScale);
            DragHandling.HandleItemDragging(b, _canvas);

            foreach (var slot in b.GetSlots())
            {
                var xxx = slot.Connections.Count > 0 && slot.Connections[0].IsSnapped;
                var thickness = xxx ? 2 : 1;
                drawList.AddCircle(pMin + anchorScale * slot.AnchorPos, slotSize, c, 3, thickness);
            }
        }
    }

    private readonly ScalableCanvas _canvas = new();

    public void Draw(ImDrawListPtr drawList, bool hideHeader = false)
    {
        _canvas.UpdateCanvas();
        HandleFenceSelection();
    }

    private void HandleFenceSelection()
    {
        _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
        switch (_fenceState)
        {
            case SelectionFence.States.PressedButNotMoved:
                if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
                    _selection.Clear();
                break;

            case SelectionFence.States.Updated:
                HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen);
                break;

            case SelectionFence.States.CompletedAsClick:
                _selection.Clear();
                break;
        }
    }

    private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
    {
        var boundsInCanvas = _canvas.InverseTransformRect(boundsInScreen);
        // var elementsToSelect = (from child in PoolForBlendOperations.Variations
        //                         let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
        //                         where rect.Overlaps(boundsInCanvas)
        //                         select child).ToList();

        _selection.Clear();
        // foreach (var element in elementsToSelect)
        // {
        //     Selection.AddSelection(element);
        // }
    }

    /// <summary>
    /// Implement selectionContainer
    /// </summary>
    public IEnumerable<ISelectableCanvasObject> GetSelectables()
    {
        //return PoolForBlendOperations.Variations;
        return new List<ISelectableCanvasObject>();
    }

    private SelectionFence.States _fenceState;
    private readonly CanvasElementSelection _selection = new();

    public override List<Window> GetInstances()
    {
        return new List<Window>();
    }

    private void InitializeFromSymbol()
    {
        _blocks.Clear();
        _connections.Clear();
        _groups.Clear();
        _slots.Clear();

        var symbol = SymbolUiRegistry.Entries.Values.FirstOrDefault(c => c.Symbol.Name == "CCCampTable");
        if (symbol == null)
            return;

        foreach (var child in symbol.ChildUis)
        {
            _blocks.Add(new Block
                            {
                                PosOnCanvas = child.PosOnCanvas,
                                UnitHeight = 1,
                                Name = child.SymbolChild.ReadableName,
                            });
        }

        _initialized = true;
    }

    public static readonly Vector2 BlockSize = new(100, 20);

    private void InitializeFromDefinition()
    {
        _blocks.Clear();
        _connections.Clear();
        _groups.Clear();
        _slots.Clear();

        var a = new Block(0, 1, "a");
        var b = new Block(0, 2, "b");
        var c = new Block(0, 3, "c");

        var d = new Block(2, 6, "d");

        _blocks.Add(a);
        _blocks.Add(b);
        _blocks.Add(c);
        _blocks.Add(d);

        _movingTestBlock = d;

        _connections.Add(new Connection() { Source = a.Outputs[0], Target = b.Inputs[0], IsSnapped = true });
        _connections.Add(new Connection() { Source = b.Outputs[0], Target = c.Inputs[0], IsSnapped = true });

        // Register connections to blocks
        foreach (var connection in _connections)
        {
            connection.Source?.Block.Outputs[0].Connections.Add(connection);
            connection.Target?.Block.Inputs[0].Connections.Add(connection);
        }

        _initialized = true;
    }

    private Block _movingTestBlock;
    private readonly List<Block> _blocks = new();
    private readonly List<Connection> _connections = new();
    private readonly List<Group> _groups = new();
    private readonly List<Slot> _slots = new();
}

public class Block : ISelectableCanvasObject
{
    public Block()
    {
    }

    public Block(int x, int y, string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        PosOnCanvas = new Vector2(x, y) * ResearchWindow.BlockSize;
        Inputs = new List<Slot>()
                     {
                         new() { Block = this, AnchorPos = new Vector2(0.5f, 0.0f), IsInput = true },
                         new() { Block = this, AnchorPos = new Vector2(0.0f, 0.5f), IsInput = true },
                     };

        Outputs = new List<Slot>()
                      {
                          new() { Block = this, AnchorPos = new Vector2(0.5f, 1.0f) },
                          new() { Block = this, AnchorPos = new Vector2(1f, 0.5f) },
                      };
    }

    public IEnumerable<Slot> GetSlots()
    {
        foreach (var input in Inputs)
        {
            yield return input;
        }

        foreach (var output in Outputs)
        {
            yield return output;
        }
    }

    public int UnitHeight;
    public string Name;
    public Type PrimaryType = typeof(float);

    public List<Slot> Inputs = new();
    public List<Slot> Outputs = new();
    public Guid Id { get; }
    public Vector2 PosOnCanvas { get; set; }
    public Vector2 Size { get; set; }
    public bool IsSelected { get; }

    public override string ToString()
    {
        return Name;
    }
}

public class Group
{
    public List<Block> Blocks;
}

public class Slot
{
    public Block Block;
    public Type Type = typeof(float);
    public Vector2 AnchorPos;
    public bool IsInput;
    public List<Connection> Connections = new List<Connection>(); // not sure if merging inout and output connection is a good idea.
    public bool IsShy;

    public enum Visibility
    {
        Visible,
        ShyButConnected,
        ShyButRevealed,
        Shy,
    }
}

public class Connection
{
    public Slot Source;
    public Slot Target;
    public bool IsSnapped;
}

public static class DragHandling
{
    /// <summary>
    /// NOTE: This has to be called for ALL movable elements (ops, inputs, outputs) and directly after ImGui.Item
    /// </summary>
    public static void HandleItemDragging(ISelectableCanvasObject node, ScalableCanvas canvas)
    {
        var justClicked = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

        var isActiveNode = node == _draggedNode;
        
        
        //Log.Debug($"is Active node {node}  {isActiveNode}   DraggedNode:{_draggedNode}");
        if (justClicked)
        {
            //var compositionSymbolId = GraphCanvas.Current.CompositionOp.Symbol.Id;
            _draggedNode = node;
            if (node.IsSelected)
            {
                _draggedNodes = _selectedNodes;
            }
            else
            {
                //var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];

                _draggedNodes.Add(node);
            }

            // _moveCommand = new ModifyCanvasElementsCommand(compositionSymbolId, _draggedNodes);
        }
        else if (isActiveNode && ImGui.IsMouseDown(ImGuiMouseButton.Left)) // && _moveCommand != null)
        {
            // if (!T3Ui.IsCurrentlySaving && ShakeDetector.TestDragForShake(ImGui.GetMousePos()))
            // {
            //     _moveCommand.StoreCurrentValues();
            //     UndoRedoStack.Add(_moveCommand);
            //     DisconnectDraggedNodes();
            // }
            if (!ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                _isDragging = false;
            }
            else
            {
                if (!_isDragging)
                {
                    _dragStartPosInOpOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos()) - node.PosOnCanvas;
                    _isDragging = true;
                }

                var mousePosOnCanvas = canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
                var newDragPosInCanvas = mousePosOnCanvas - _dragStartPosInOpOnCanvas;
                //
                // var bestDistanceInCanvas = float.PositiveInfinity;
                // var targetSnapPositionInCanvas = Vector2.Zero;
                //
                // foreach (var offset in _snapOffsetsInCanvas)
                // {
                //     var heightAffectFactor = 0;
                //     if (Math.Abs(offset.X) < 0.01f)
                //     {
                //         if (offset.Y > 0)
                //         {
                //             heightAffectFactor = -1;
                //         }
                //         else
                //         {
                //             heightAffectFactor = 1;
                //         }
                //     }
                //
                //     foreach (var neighbor in GraphCanvas.Current.SelectableChildren)
                //     {
                //         if (neighbor == node || _draggedNodes.Contains(neighbor))
                //             continue;
                //
                //         var offset2 = new Vector2(offset.X, -neighbor.Size.Y * heightAffectFactor + offset.Y);
                //         var snapToNeighborPos = neighbor.PosOnCanvas + offset2;
                //
                //         var d = Vector2.Distance(snapToNeighborPos, newDragPosInCanvas);
                //         if (!(d < bestDistanceInCanvas))
                //             continue;
                //
                //         targetSnapPositionInCanvas = snapToNeighborPos;
                //         bestDistanceInCanvas = d;
                //     }
                // }
                //
                // var snapDistanceInCanvas = GraphCanvas.Current.InverseTransformDirection(new Vector2(20, 0)).X;
                // var isSnapping = bestDistanceInCanvas < snapDistanceInCanvas;

                //var isSnapping = false;
                // var moveDeltaOnCanvas = isSnapping
                //                             ? targetSnapPositionInCanvas - node.PosOnCanvas
                //                             : newDragPosInCanvas - node.PosOnCanvas;

                var moveDeltaOnCanvas = newDragPosInCanvas - node.PosOnCanvas;
                // Drag selection
                foreach (var e in _draggedNodes)
                {
                    e.PosOnCanvas += moveDeltaOnCanvas;
                }
            }
        }
        else if (isActiveNode && ImGui.IsMouseReleased(0))
        {
            if (_draggedNode != node)
                return;

            //var singleDraggedNode = (_draggedNodes.Count == 1) ? _draggedNodes[0] : null;
            _draggedNode = null;
            _draggedNodes.Clear();

            var wasDragging = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() > UserSettings.Config.ClickThreshold;
            if (wasDragging)
            {
                // connection lines would be split here...
            }
            else
            {
                if (!IsNodeSelected(node))
                {
                    var replaceSelection = !ImGui.GetIO().KeyShift;
                    if (replaceSelection)
                    {
                        SetSelection(node);
                    }
                    else
                    {
                        AddSelection(node);
                    }
                }
                else
                {
                    if (ImGui.GetIO().KeyShift)
                    {
                        DeselectNode(node);
                    }
                }
            }
        }
        else if (ImGui.IsMouseReleased(0)) // && _moveCommand == null)
        {
            // This happens after shake
            _draggedNodes.Clear();
            _draggedNode = null;
        }

        var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
            && !wasDraggingRight
            && ImGui.IsItemHovered()
            && !IsNodeSelected(node))
        {
            SetSelection(node);
        }
    }

    public static void SetSelection(ISelectableCanvasObject selectedObject)
    {
        _selectedNodes.Clear();
        _selectedNodes.Add(selectedObject);
    }

    public static bool IsNodeSelected(ISelectableCanvasObject node)
    {
        return _selectedNodes.Contains(node);
    }

    public static void AddSelection(IEnumerable<ISelectableCanvasObject> additionalObjects)
    {
        _selectedNodes.UnionWith(additionalObjects);
    }

    public static void AddSelection(ISelectableCanvasObject additionalObject)
    {
        _selectedNodes.Add(additionalObject);
    }

    public static void DeselectNode(ISelectableCanvasObject objectToRemove)
    {
        _selectedNodes.Remove(objectToRemove);
    }

    private static bool _isDragging;
    private static Vector2 _dragStartPosInOpOnCanvas;

    public static HashSet<ISelectableCanvasObject> _selectedNodes = new();

    private static ISelectableCanvasObject _draggedNode;
    private static HashSet<ISelectableCanvasObject> _draggedNodes = new();
}