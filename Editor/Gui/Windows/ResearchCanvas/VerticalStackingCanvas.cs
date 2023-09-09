using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

public class VerticalStackingCanvas
{
    private bool _initialized;

    public void Draw(bool hideHeader = false)
    {
        if (ImGui.Button("Reinit") || !_initialized)
        {
            //InitializeFromSymbol();
            InitializeFromDefinition();
        }

        var drawList = ImGui.GetWindowDrawList();

        // move test block
        var posOnCanvas = Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        //_movingTestBlock.PosOnCanvas = posOnCanvas;

        // snap test block


        Canvas.UpdateCanvas();
        HandleFenceSelection();

        var anchorScale = Canvas.TransformDirection(BlockSize);
        var slotSize = 3 * Canvas.Scale.X;

        foreach (var b in Blocks)
        {
            if (!TypeUiRegistry.Entries.TryGetValue(b.PrimaryType, out var typeUiProperties))
                continue;

            var c = typeUiProperties.Color;
            var cLabel = ColorVariations.OperatorLabel.Apply(c);

            var pMin = Canvas.TransformPosition(b.PosOnCanvas);
            var pMax = Canvas.TransformPosition(b.PosOnCanvas + BlockSize);

            ImGui.SetCursorScreenPos(pMin);
            ImGui.InvisibleButton(b.Id.ToString(), anchorScale);
            
            var isDraggedAndSnapped = DragHandling.HandleItemDragging(b, this, out var dragPos);
            if (isDraggedAndSnapped)
            {
            }
            
            var fade = isDraggedAndSnapped ? 0.6f : 1;
            
            drawList.AddRectFilled(pMin, pMax, c.Fade(0.7f * fade), 3);
            var outlineColor = BlockSelection.IsNodeSelected(b)
                                   ? UiColors.ForegroundFull
                                   : UiColors.BackgroundFull.Fade(0.6f);
            drawList.AddRect(pMin, pMax, outlineColor, 4);
            drawList.AddText(Fonts.FontNormal,16 * Canvas.Scale.X,pMin + new Vector2(6, 3), cLabel, b.Name);

            foreach (var slot in b.GetSlots())
            {
                var isSnappedAndConnected = slot.Connections.Count > 0 && slot.Connections[0].IsSnapped;
                if (isSnappedAndConnected)
                {
                    drawList.AddCircleFilled(pMin + anchorScale * slot.AnchorPos, slotSize, c, 3);
                }
                else
                {
                    drawList.AddCircle(pMin + anchorScale * slot.AnchorPos, slotSize, c, 3, 1);
                }
            }

            if (isDraggedAndSnapped)
            {
                var dragPosOnScreen = Canvas.TransformPosition(dragPos);
                drawList.AddRect(dragPosOnScreen, dragPosOnScreen + anchorScale, UiColors.ForegroundFull.Fade(0.5f),4);
            }
        }
        
        // Draw Connections
        foreach (var c in Connections)
        {
            if (c.IsSnapped)
                continue;

            var pSource = Canvas.TransformPosition(c.Source.PosOnCanvas);
            var pTarget = Canvas.TransformPosition(c.Target.PosOnCanvas);
            drawList.AddBezierCubic(pSource, 
                                    pSource + new Vector2(0, 100),
                                    pTarget - new Vector2(0, 100),
                                    pTarget,
                                    UiColors.ForegroundFull.Fade(0.6f),
                                    2);
        }
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

    //public delegate bool SnapHandler(ISelectableCanvasObject canvasObject, out Vector2 delta2); 

    private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
    {
        var boundsInCanvas = Canvas.InverseTransformRect(boundsInScreen);
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


    private void InitializeFromSymbol()
    {
        Blocks.Clear();
        Connections.Clear();
        _groups.Clear();
        _slots.Clear();

        var symbol = SymbolUiRegistry.Entries.Values.FirstOrDefault(c => c.Symbol.Name == "CCCampTable");
        if (symbol == null)
            return;

        foreach (var child in symbol.ChildUis)
        {
            Blocks.Add(new Block
                           {
                               PosOnCanvas = child.PosOnCanvas,
                               UnitHeight = 1,
                               Name = child.SymbolChild.ReadableName,
                           });
        }

        _initialized = true;
    }


    private void InitializeFromDefinition()
    {
        Log.Debug("Initialize!");
        Blocks.Clear();
        Connections.Clear();
        _groups.Clear();
        _slots.Clear();

        var a = new Block(0, 1, "a");
        var b = new Block(0, 2, "b");
        var c = new Block(0, 3, "c");

        var d = new Block(2, 6, "d");

        Blocks.Add(a);
        Blocks.Add(b);
        Blocks.Add(c);
        Blocks.Add(d);

        // Connections.Add(new Connection(a.Outputs[0], b.Inputs[0]));
        // Connections.Add(new Connection(b.Outputs[0], c.Inputs[0]));
        _initialized = true;
    }

    //private Block _movingTestBlock;
    public static readonly Vector2 BlockSize = new(100, 20);
    public readonly ScalableCanvas Canvas = new();
    
    public readonly List<Block> Blocks = new();
    public readonly List<Connection> Connections = new();
    private readonly List<Group> _groups = new();
    private readonly List<Slot> _slots = new();
}
