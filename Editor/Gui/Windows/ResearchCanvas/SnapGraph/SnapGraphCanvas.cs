using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Windows.ResearchCanvas.SnapGraph;

public class SnapGraphCanvas:ScalableCanvas
{
    public void Draw(bool hideHeader = false)
    {
        if (_forceUpdating)
        {
            _compositionOp = GraphWindow.GetMainComposition();
            if (_compositionOp == null)
                return;

            _snapGraphLayout.CollectSnappingGroupsFromSymbolUi(_compositionOp);
        }

        if (ImGui.Button("Center"))
        {
            CenterView();
        }

        FormInputs.AddCheckBox("Update", ref _forceUpdating);

        var drawList = ImGui.GetWindowDrawList();

        UpdateCanvas();

        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
            && ConnectionMaker.TempConnections.Count == 0)
            HandleFenceSelection();

        var canvasScale = Scale.X;
        var slotSize = 3 * canvasScale;
        var gridSizeOnScreen = TransformDirection(SnapGraphItem.GridSize);

        foreach (var item in _snapGraphLayout.Items.Values)
        {
            if (!TypeUiRegistry.Entries.TryGetValue(item.PrimaryType, out var typeUiProperties))
                continue;

            // var itemSizeOnScreen = TransformDirection(item.Size);
            var typeColor = typeUiProperties.Color;
            var labelColor = ColorVariations.OperatorLabel.Apply(typeColor);

            var pMin = TransformPosition(item.PosOnCanvas);
            var pMax = TransformPosition(item.PosOnCanvas + item.Size);

            ImGui.SetCursorScreenPos(pMin);
            ImGui.InvisibleButton(item.Id.ToString(), pMax-pMin);
            SnapItemMovement.Handle(item, this);

            drawList.AddRectFilled(pMin, pMax, ColorVariations.OperatorBackground.Apply(typeColor).Fade(0.7f), 3);
            
            var isSelected = NodeSelection.IsNodeSelected(item.SymbolChildUi);
            var outlineColor = isSelected
                                   ? UiColors.ForegroundFull
                                   : UiColors.BackgroundFull.Fade(0.3f);
            drawList.AddRect(pMin, pMax, outlineColor, 3);
            drawList.AddText(Fonts.FontNormal, 13 * canvasScale, pMin + new Vector2(4, 3) * canvasScale, labelColor, item.SymbolChild.ReadableName);

            for (var inputIndex = 1; inputIndex < item.VisibleInputSockets.Count; inputIndex++)
            {
                var input = item.VisibleInputSockets[inputIndex];
                drawList.AddText(Fonts.FontSmall, 11 * canvasScale,
                                 pMin + new Vector2(4, 3) * canvasScale + new Vector2(0, gridSizeOnScreen.Y * (inputIndex)),
                                 labelColor,
                                 input.Input.Input.Name);
            }

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

        // Draw connections
        foreach (var connection in _snapGraphLayout.SnapConnections)
        {
            if (connection.Style == SnapGraphConnection.ConnectionStyles.Unknown)
                continue;

            if (!TypeUiRegistry.Entries.TryGetValue(connection.TargetItem.PrimaryType, out var typeUiProperties))
                continue;

            var c = typeUiProperties.Color;
            var sourcePosOnScreen = TransformPosition(connection.SourcePos);
            var targetPosOnScreen = TransformPosition(connection.TargetPos);

            var d = Vector2.Distance(sourcePosOnScreen, targetPosOnScreen) / 2;
            switch (connection.Style)
            {
                case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, slotSize, c, 3);
                    break;
                case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                    drawList.AddTriangleFilled(
                                               sourcePosOnScreen + new Vector2(-1, -1) * canvasScale * 4,
                                               sourcePosOnScreen + new Vector2(1, -1) * canvasScale * 4,
                                               sourcePosOnScreen + new Vector2(0, 1) * canvasScale * 4,
                                               c);
                    break;
                case SnapGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal:
                    break;
                case SnapGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical:
                    break;
                case SnapGraphConnection.ConnectionStyles.BottomToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            UiColors.ForegroundFull.Fade(0.6f),
                                            2);
                    break;
                case SnapGraphConnection.ConnectionStyles.BottomToLeft:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            UiColors.ForegroundFull.Fade(0.6f),
                                            2);
                    break;
                case SnapGraphConnection.ConnectionStyles.RightToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            UiColors.ForegroundFull.Fade(0.6f),
                                            2);
                    break;
                case SnapGraphConnection.ConnectionStyles.RightToLeft:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            UiColors.ForegroundFull.Fade(0.6f),
                                            2);
                    break;
                case SnapGraphConnection.ConnectionStyles.Unknown:
                    break;
            }
        }
    }

    private void HandleFenceSelection()
    {
        _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
        switch (_fenceState)
        {
            case SelectionFence.States.PressedButNotMoved:
                if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
                    NodeSelection.Clear();
                break;

            case SelectionFence.States.Updated:
                HandleSelectionFenceUpdate(SelectionFence.BoundsInScreen);
                break;

            case SelectionFence.States.CompletedAsClick:
                // A hack to prevent clearing selection when opening parameter popup
                if (ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
                    break;

                NodeSelection.Clear();
                NodeSelection.SetSelectionToParent(_compositionOp);
                break;
        }
    }

    private SelectionFence.States _fenceState = SelectionFence.States.Inactive;

    // TODO: Support non graph items like annotations.
    private void HandleSelectionFenceUpdate(ImRect boundsInScreen)
    {
        var boundsInCanvas = InverseTransformRect(boundsInScreen);
        var itemsInFence = (from child in _snapGraphLayout.Items.Values
                             let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                             where rect.Overlaps(boundsInCanvas)
                             select child).ToList();

        if (SelectionFence.SelectMode == SelectionFence.SelectModes.Replace)
        {
            NodeSelection.Clear();
        }

        foreach (var item in itemsInFence)
        {
            if (SelectionFence.SelectMode == SelectionFence.SelectModes.Remove)
            {
                NodeSelection.DeselectNode(item, item.Instance);
            }
            else
            {
                NodeSelection.AddSymbolChildToSelection(item.SymbolChildUi, item.Instance);
            }
        }
    }

    private void CenterView()
    {
        var visibleArea = new ImRect();
        foreach (var item in _snapGraphLayout.Items.Values)
        {
            visibleArea.Add(item.PosOnCanvas);
        }

        FitAreaOnCanvas(visibleArea);
    }

    private readonly SnapGraphLayout _snapGraphLayout = new();
    private bool _forceUpdating;
    private Instance _compositionOp;

}