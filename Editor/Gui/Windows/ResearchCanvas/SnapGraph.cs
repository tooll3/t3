using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.ResearchCanvas.Model;

namespace T3.Editor.Gui.Windows.ResearchCanvas;

public class SnapGraph
{
    public void Draw(bool hideHeader = false)
    {
        if(_keepUpdating)
            InitializeFromDefinition();
        if (ImGui.Button("Center"))
        {
            CenterView();
        }

        FormInputs.AddCheckBox("Update", ref _keepUpdating);

        var drawList = ImGui.GetWindowDrawList();

        // // move test block
        // var posOnCanvas = Canvas.InverseTransformPositionFloat(ImGui.GetMousePos());
        // //_movingTestBlock.PosOnCanvas = posOnCanvas;
        //
        // // snap test block
        
        _canvas.UpdateCanvas();
        
        
        // HandleFenceSelection();

        var anchorScale = _canvas.TransformDirection(SnapGraphItem.GridSize);
        var canvasScale = _canvas.Scale.X;
        var slotSize = 3 * canvasScale;
        
        foreach (var b in _snapGraphLayout.Items.Values)
        {
            if (!TypeUiRegistry.Entries.TryGetValue(b.PrimaryType, out var typeUiProperties))
                continue;
        
            var c = typeUiProperties.Color;
            var cLabel = ColorVariations.OperatorLabel.Apply(c);
        
            var pMin = _canvas.TransformPosition(b.PosOnCanvas);
            var pMax = _canvas.TransformPosition(b.PosOnCanvas + SnapGraphItem.GridSize) ;
        
            ImGui.SetCursorScreenPos(pMin);
            ImGui.InvisibleButton(b.Id.ToString(), anchorScale);
            
            // var isDraggedAndSnapped = DragHandling.HandleItemDragging(b, this, out var dragPos);
            // if (isDraggedAndSnapped)
            // {
            // }
        
            // var slots = b.GetSlots().ToList();
            // if (ImGui.IsItemHovered())
            // {
            //     ImGui.BeginTooltip();
            //     foreach (var s in slots)
            //     {
            //         ImGui.Text("connected:" +s.IsConnected);
            //     }
            //     ImGui.EndTooltip();
            // }
            
            // var fade = isDraggedAndSnapped ? 0.6f : 1;
            
            drawList.AddRectFilled(pMin, pMax,  ColorVariations.OperatorBackground.Apply(c).Fade(0.7f ), 3 );
            var outlineColor = BlockSelection.IsNodeSelected(b)
                                   ? UiColors.ForegroundFull
                                   : UiColors.BackgroundFull.Fade(0.3f);
            drawList.AddRect(pMin, pMax, outlineColor, 3 );
            drawList.AddText(Fonts.FontNormal,13 * canvasScale,pMin + new Vector2(4, 3) * canvasScale, cLabel, b.SymbolChild.ReadableName);
            //
            // // Draw Slots
            // foreach (var slot in slots)
            // {
            //     if (slot.IsInput)
            //     {
            //         if (slot.Connections.Count > 0)
            //         {
            //             slot.Connections[0].GetEndPositions(out _, out var targetPos);
            //             drawList.AddCircleFilled( _canvas.TransformPosition( targetPos), slotSize, c, 3);
            //         }
            //         else
            //         {
            //             drawList.AddCircle(_canvas.TransformPosition(slot.HorizontalPosOnCanvas), slotSize, c, 3);
            //             drawList.AddCircle(_canvas.TransformPosition(slot.VerticalPosOnCanvas), slotSize, c, 3);
            //         }
            //     }
            //     else
            //     {
            //         // Outputs
            //         var isFirstSnappedAndConnected = slot.Connections.Count > 0 && slot.Connections[0].IsSnapped;
            //         if (isFirstSnappedAndConnected)
            //         {
            //             drawList.AddCircleFilled(_canvas.TransformPosition(slot.VerticalPosOnCanvas), slotSize, c, 3);
            //         }
            //         else
            //         {
            //             drawList.AddCircle(_canvas.TransformPosition(slot.VerticalPosOnCanvas), slotSize, c, 3, 1);
            //         }
            //         drawList.AddCircle(_canvas.TransformPosition(slot.HorizontalPosOnCanvas), slotSize, c, 3);
            //         //drawList.AddCircle(Canvas.TransformPosition(slot.HorizontalPosOnCanvas), slotSize, UiColors.StatusWarning, 3);
            //     }
            //
            //     //var horizontalConnections = slot.GetConnections(Connection.Orientations.Horizontal);
            //     // var isSnappedAndConnected = slot.Connections.Count > 0 && slot.Connections[0].IsSnapped;
            //     // if (isSnappedAndConnected)
            //     // {
            //     //     drawList.AddCircleFilled(pMin + anchorScale * slot.AnchorPos, slotSize, c, 3);
            //     // }
            //     // else
            //     // {
            //     //     drawList.AddCircle(pMin + anchorScale * slot.AnchorPos, slotSize, c, 3, 1);
            //     // }
            // }
            //
            // if (isDraggedAndSnapped)
            // {
            //     var dragPosOnScreen = _canvas.TransformPosition(dragPos);
            //     drawList.AddRect(dragPosOnScreen, dragPosOnScreen + anchorScale, UiColors.ForegroundFull.Fade(0.5f),4);
            // }
        }
        //
        // // Draw Connection lines
        // foreach (var c in Connections)
        // {
        //     if (c.IsSnapped)
        //         continue;
        //
        //     c.GetEndPositions(out var sourcePos, out var targetPos);
        //     var pSource = _canvas.TransformPosition(sourcePos);
        //     var pTarget = _canvas.TransformPosition(targetPos);
        //
        //     var d = Vector2.Distance(pSource, pTarget) / 2;
        //     if (c.GetOrientation() == Connection.Orientations.Vertical)
        //     {
        //         drawList.AddBezierCubic(pSource, 
        //                                 pSource + new Vector2(0, d),
        //                                 pTarget - new Vector2(0, d),
        //                                 pTarget,
        //                                 UiColors.ForegroundFull.Fade(0.6f),
        //                                 2);
        //     }
        //     else
        //     {
        //         drawList.AddBezierCubic(pSource, 
        //                                 pSource + new Vector2(d, 0),
        //                                 pTarget - new Vector2(d, 0),
        //                                 pTarget,
        //                                 UiColors.ForegroundFull.Fade(0.6f),
        //                                 2);
        //     }
        // }
    }

    private bool _keepUpdating;
    
    private void InitializeFromDefinition()
    {
        // Log.Debug("Initialize!");
        var instance = NodeSelection.GetFirstSelectedInstance();
        instance = GraphWindow.GetMainComposition();
        if (instance != null)
        {
            _snapGraphLayout.CollectSnappingGroupsFromSymbolUi(instance);
        }
        _initialized = true;
    }

    private void CenterView()
    {
        ImRect visibleArea = new ImRect();
        foreach (var item in _snapGraphLayout.Items.Values)
        {
            visibleArea.Add(item.PosOnCanvas);
        }
        _canvas.FitAreaOnCanvas(visibleArea);
    }

    private readonly SnapGraphLayout _snapGraphLayout = new();
    private readonly ScalableCanvas _canvas = new();
    private bool _initialized;
}