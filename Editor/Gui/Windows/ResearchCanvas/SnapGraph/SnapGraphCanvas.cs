using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
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

public class SnapGraphCanvas : ScalableCanvas
{
    public SnapGraphCanvas()
    {
        _itemMovement = new SnapItemMovement(this, _snapGraphLayout);
    }

    public void Draw()
    {
        _compositionOp = GraphWindow.GetMainComposition();
        if (_compositionOp == null)
            return;

        _snapGraphLayout.ComputeLayout(_compositionOp);

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
        var showDebug = ImGui.GetIO().KeyCtrl;

        // Draw Nodes
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
            ImGui.PushID(item.Id.GetHashCode());
            ImGui.InvisibleButton(string.Empty, pMax - pMin);
            _itemMovement.Handle(item, this);
            ImGui.PopID();

            drawList.AddRectFilled(pMin, pMax, ColorVariations.OperatorBackground.Apply(typeColor).Fade(0.7f), 3);

            var isSelected = NodeSelection.IsNodeSelected(item.SymbolChildUi);
            var outlineColor = isSelected
                                   ? UiColors.ForegroundFull
                                   : UiColors.BackgroundFull.Fade(0.3f);
            drawList.AddRect(pMin, pMax, outlineColor, 3);
            drawList.AddText(Fonts.FontBold, Fonts.FontBold.FontSize * canvasScale * 0.7f, pMin + new Vector2(4, 3) * canvasScale, labelColor,
                             item.SymbolChild.ReadableName);

            for (var inputIndex = 1; inputIndex < item.InputLines.Length; inputIndex++)
            {
                var input = item.InputLines[inputIndex];
                drawList.AddText(Fonts.FontSmall, 10 * canvasScale,
                                 pMin + new Vector2(4, 3) * canvasScale + new Vector2(0, gridSizeOnScreen.Y * (inputIndex)),
                                 labelColor,
                                 input.Input.Input.Name);
            }

            // Draw sockets
            foreach (var i in item.GetInputAnchors())
            {
                if (!TypeUiRegistry.Entries.TryGetValue(i.ConnectionType, out var type2UiProperties))
                    continue;

                drawList.AddCircle(TransformPosition(i.PositionOnCanvas), 3, type2UiProperties.Color, 4);
                if (showDebug)
                {
                    ImGui.SetCursorScreenPos(TransformPosition(i.PositionOnCanvas));
                    ImGui.Button("##" + i.GetHashCode());
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("hash:" + i.ConnectionHash);
                }
            }

            foreach (var i in item.GetOutputAnchors())
            {
                if (!TypeUiRegistry.Entries.TryGetValue(i.ConnectionType, out var type2UiProperties))
                    continue;

                if (showDebug)
                {
                    ImGui.SetCursorScreenPos(TransformPosition(i.PositionOnCanvas));
                    ImGui.Button("##" + i.GetHashCode());
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("hash:" + i.ConnectionHash);

                }
                drawList.AddCircle(TransformPosition(i.PositionOnCanvas), 3, type2UiProperties.Color, 4);
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

            if (!TypeUiRegistry.Entries.TryGetValue(connection.TargetInput.ValueType, out var typeUiProperties))
                continue;

            var typeColor = typeUiProperties.Color;
            var sourcePosOnScreen = TransformPosition(connection.SourcePos);
            var targetPosOnScreen = TransformPosition(connection.TargetPos);

            var d = Vector2.Distance(sourcePosOnScreen, targetPosOnScreen) / 2;
            switch (connection.Style)
            {
                case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, slotSize * 1.6f, typeColor, 3);
                    break;
                case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                    drawList.AddTriangleFilled(
                                               sourcePosOnScreen + new Vector2(-1, -1) * canvasScale * 4,
                                               sourcePosOnScreen + new Vector2(1, -1) * canvasScale * 4,
                                               sourcePosOnScreen + new Vector2(0, 1) * canvasScale * 4,
                                               typeColor);
                    break;
                case SnapGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, slotSize * 1.6f, typeColor, 3);
                    break;
                case SnapGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical:
                    drawList.AddCircleFilled(sourcePosOnScreen, slotSize * 1.6f, Color.Red, 3);
                    break;
                case SnapGraphConnection.ConnectionStyles.BottomToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    break;
                case SnapGraphConnection.ConnectionStyles.BottomToLeft:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    break;
                case SnapGraphConnection.ConnectionStyles.RightToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    break;
                case SnapGraphConnection.ConnectionStyles.RightToLeft:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
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

    private readonly SnapItemMovement _itemMovement;
    private readonly SnapGraphLayout _snapGraphLayout = new();
    private bool _forceUpdating;
    private Instance _compositionOp;
}