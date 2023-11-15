using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.ResearchCanvas.Model;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows.ResearchCanvas;



/**
 * Steps for implementation:
 * The general concepts seems to be working. The biggest questions are:
 * - Connection to secondary-parameters:
 *  - Temporarily expanding operators:
 *    - Adjusting layout of snapped ops
 *    - highlighting matching inputs
 *    - highlighting potentially matching inputs while collapes
 *    - damping positions and sizes
 *  - selecting and dragging multiple ops
 *    - only enable snapping with single snapped group
 *    - Select complete group with some kind shortcut (e.g. Shift + Double click?)
 *  - popup operator from snapped groups
 *  - rearrange sorting order without snapped group
 *
 *
 * Mode:
 * - Is dragging multiple ops
 *   - DraggedOps
 *   - IsSingle snapped group
 *   - dragged ops main outputs
 *
 * Apply block-layout change:
 *  - change height: Insert
 *  - Flood fill snapped ops to bottom right quadrant
 *
 *  Toggle expand parameters of op (just to check layout change)
 * 
 */

public class VerticalStackingUi
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

        // // move test block
        // var posOnCanvas = Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        // //_movingTestBlock.PosOnCanvas = posOnCanvas;
        //
        // // snap test block


        Canvas.UpdateCanvas();
        HandleFenceSelection();

        var anchorScale = Canvas.TransformDirection(BlockSize);
        var canvasScale = Canvas.Scale.X;
        var slotSize = 3 * canvasScale;

        foreach (var b in Blocks)
        {
            if (!TypeUiRegistry.Entries.TryGetValue(b.PrimaryType, out var typeUiProperties))
                continue;

            var c = typeUiProperties.Color;
            var cLabel = ColorVariations.OperatorLabel.Apply(c);

            var pMin = Canvas.TransformPosition(b.PosOnCanvas);
            var pMax = Canvas.TransformPosition(b.PosOnCanvas + BlockSize) ;

            ImGui.SetCursorScreenPos(pMin);
            ImGui.InvisibleButton(b.Id.ToString(), anchorScale);
            
            var isDraggedAndSnapped = DragHandling.HandleItemDragging(b, this, out var dragPos);
            if (isDraggedAndSnapped)
            {
            }

            var slots = b.GetSlots().ToList();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                foreach (var s in slots)
                {
                    ImGui.Text("connected:" +s.IsConnected);
                }
                ImGui.EndTooltip();
            }
            
            var fade = isDraggedAndSnapped ? 0.6f : 1;
            
            drawList.AddRectFilled(pMin, pMax,  ColorVariations.OperatorBackground.Apply(c).Fade(0.7f * fade), 3 );
            var outlineColor = BlockSelection.IsNodeSelected(b)
                                   ? UiColors.ForegroundFull
                                   : UiColors.BackgroundFull.Fade(0.3f);
            drawList.AddRect(pMin, pMax, outlineColor, 3 );
            drawList.AddText(Fonts.FontNormal,13 * canvasScale,pMin + new Vector2(4, 3) * canvasScale, cLabel, b.Name);

            // Draw Slots
            foreach (var slot in slots)
            {
                if (slot.IsInput)
                {
                    if (slot.Connections.Count > 0)
                    {
                        slot.Connections[0].GetEndPositions(out _, out var targetPos);
                        drawList.AddCircleFilled( Canvas.TransformPosition( targetPos), slotSize, c, 3);
                    }
                    else
                    {
                        drawList.AddCircle(Canvas.TransformPosition(slot.HorizontalPosOnCanvas), slotSize, c, 3);
                        drawList.AddCircle(Canvas.TransformPosition(slot.VerticalPosOnCanvas), slotSize, c, 3);
                    }
                }
                else
                {
                    // Outputs
                    var isFirstSnappedAndConnected = slot.Connections.Count > 0 && slot.Connections[0].IsSnapped;
                    if (isFirstSnappedAndConnected)
                    {
                        drawList.AddCircleFilled(Canvas.TransformPosition(slot.VerticalPosOnCanvas), slotSize, c, 3);
                    }
                    else
                    {
                        drawList.AddCircle(Canvas.TransformPosition(slot.VerticalPosOnCanvas), slotSize, c, 3, 1);
                    }
                    drawList.AddCircle(Canvas.TransformPosition(slot.HorizontalPosOnCanvas), slotSize, c, 3);
                    //drawList.AddCircle(Canvas.TransformPosition(slot.HorizontalPosOnCanvas), slotSize, UiColors.StatusWarning, 3);
                }

                //var horizontalConnections = slot.GetConnections(Connection.Orientations.Horizontal);
                // var isSnappedAndConnected = slot.Connections.Count > 0 && slot.Connections[0].IsSnapped;
                // if (isSnappedAndConnected)
                // {
                //     drawList.AddCircleFilled(pMin + anchorScale * slot.AnchorPos, slotSize, c, 3);
                // }
                // else
                // {
                //     drawList.AddCircle(pMin + anchorScale * slot.AnchorPos, slotSize, c, 3, 1);
                // }
            }

            if (isDraggedAndSnapped)
            {
                var dragPosOnScreen = Canvas.TransformPosition(dragPos);
                drawList.AddRect(dragPosOnScreen, dragPosOnScreen + anchorScale, UiColors.ForegroundFull.Fade(0.5f),4);
            }
        }
        
        // Draw Connection lines
        foreach (var c in Connections)
        {
            if (c.IsSnapped)
                continue;

            c.GetEndPositions(out var sourcePos, out var targetPos);
            var pSource = Canvas.TransformPosition(sourcePos);
            var pTarget = Canvas.TransformPosition(targetPos);

            var d = Vector2.Distance(pSource, pTarget) / 2;
            if (c.GetOrientation() == Connection.Orientations.Vertical)
            {
                drawList.AddBezierCubic(pSource, 
                                        pSource + new Vector2(0, d),
                                        pTarget - new Vector2(0, d),
                                        pTarget,
                                        UiColors.ForegroundFull.Fade(0.6f),
                                        2);
            }
            else
            {
                drawList.AddBezierCubic(pSource, 
                                        pSource + new Vector2(d, 0),
                                        pTarget - new Vector2(d, 0),
                                        pTarget,
                                        UiColors.ForegroundFull.Fade(0.6f),
                                        2);
            }
        }
    }

    
    private void HandleFenceSelection()
    {
        return;
        // _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
        // switch (_fenceState)
        // {
        //     case SelectionFence.States.PressedButNotMoved:
        //         if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
        //             _selection.Clear();
        //         break;
        //
        //     case SelectionFence.States.Updated:
        //         HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen);
        //         break;
        //
        //     case SelectionFence.States.CompletedAsClick:
        //         _selection.Clear();
        //         break;
        // }
    }

    public void RemoveConnection(Connection c)
    {
        Connections.Remove(c);
        c.Source.Connections.Remove(c);
        c.Target.Connections.Remove(c);
    }

    //public delegate bool SnapHandler(ISelectableCanvasObject canvasObject, out Vector2 delta2); 

    private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
    {
        var boundsInCanvas = Canvas.InverseTransformRect(boundsInScreen);
        // var elementsToSelect = (from child in PoolForBlendOperations.Variations
        //                         let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
        //                         where rect.Overlaps(boundsInCanvas)
        //                         select child).ToList();

        //_selection.Clear();
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
    //private readonly CanvasElementSelection _selection = new();


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
            Blocks.Add(new Block_Attempt1
                           {
                               PosOnCanvas = child.PosOnCanvas,
                               //UnitHeight = 1,
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

        Blocks.Add(new Block_Attempt1(0, 2, "CubeMesh", typeof(MeshBuffers)));
        Blocks.Add(new Block_Attempt1(0, 3, "TransformMesh", typeof(MeshBuffers)));
        Blocks.Add(new Block_Attempt1(0, 5, "Blur", typeof(Texture2D)));
        Blocks.Add(new Block_Attempt1(0, 7, "RenderTarget", typeof(Texture2D)));
        Blocks.Add(new Block_Attempt1(2, 8, "Group", typeof(Command)));
        Blocks.Add(new Block_Attempt1(5, 8, "Value", typeof(float)));
        Blocks.Add(new Block_Attempt1(5, 8, "Add", typeof(float)));
        Blocks.Add(new Block_Attempt1(5, 8, "Value2", typeof(float)));
        Blocks.Add(new Block_Attempt1(5, 8, "AnimValue", typeof(float)));
        
        Blocks.Add(new Block_Attempt1(3, 5, "DrawMesh", typeof(Command)));

        //Connections.Add(new Connection(a.Outputs[0], b.Inputs[0]));
        // Connections.Add(new Connection(b.Outputs[0], c.Inputs[0]));
        _initialized = true;
    }

    //private Block _movingTestBlock;
    public static readonly Vector2 BlockSize = new(100, 20);
    public readonly ScalableCanvas Canvas = new();
    
    public readonly List<Block_Attempt1> Blocks = new();
    public readonly List<Connection> Connections = new();
    private readonly List<SnapGroup> _groups = new();
    private readonly List<Slot> _slots = new();
}
