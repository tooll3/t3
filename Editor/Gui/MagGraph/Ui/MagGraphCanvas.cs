using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed class MagGraphCanvas : ScalableCanvas
{
    public MagGraphCanvas(MagGraphWindow Window, NodeSelection nodeSelection)
    {
        _window = Window;
        NodeSelection = nodeSelection;
        _itemMovement = new MagItemMovement(this, _magGraphLayout, nodeSelection);
        //SelectableNodeMovement = new SelectableNodeMovement(window, this, NodeSelection);
    }

    public void Draw()
    {
        _compositionOp = _window.CompositionOp;
        if (_compositionOp == null)
            return;

        _magGraphLayout.ComputeLayout(_compositionOp);

        if (ImGui.Button("Center"))
        {
            CenterView();
        }

        UpdateCanvas(out _);
        
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
            && ConnectionMaker.GetTempConnectionsFor(_window).Count == 0)
            HandleFenceSelection(_window.CompositionOp, _selectionFence);

        var drawList = ImGui.GetWindowDrawList();
        foreach (var item in _magGraphLayout.Items.Values)
        {
            DrawNode(item, drawList);
        }

        foreach (var connection in _magGraphLayout.SnapConnections)
        {
            DrawConnection(connection, drawList);
        }

        // Draw Snap indicator
        {
            var timeSinceSnap = ImGui.GetTime() - _itemMovement.LastSnapTime;
            var progress = MathUtils.RemapAndClamp((float)timeSinceSnap, 0, 0.4f, 1, 0);
            if (progress < 1)
            {
                drawList.AddCircle(TransformPosition(_itemMovement.LastSnapPositionOnCanvas),
                                   progress * 50,
                                   UiColors.ForegroundFull.Fade(progress * 0.2f));
            }
        }
    }

    private void DrawNode(MagGraphItem item, ImDrawListPtr drawList)
    {
        if (!TypeUiRegistry.TryGetPropertiesForType(item.PrimaryType, out var typeUiProperties))
            return;

        // var itemSizeOnScreen = TransformDirection(item.Size);
        var typeColor = typeUiProperties.Color;
        var labelColor = ColorVariations.OperatorLabel.Apply(typeColor);

        var pMin = TransformPosition(item.PosOnCanvas);
        var pMax = TransformPosition(item.PosOnCanvas + item.Size);

        {
            var isSnappedVertically = false;
            var isSnappedHorizontally = false;
            for (var index = 0; index < 1 && index < item.OutputLines.Length; index++)
            {
                ref var ol = ref item.OutputLines[index];
                foreach (var c in ol.ConnectionsOut)
                {
                    if (c.IsSnapped && c.SourceItem == item)
                    {
                        switch (c.Style)
                        {
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                                isSnappedVertically = true;
                                break;
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                                isSnappedHorizontally = true;
                                break;
                        }
                    }
                }
            }

            if (!isSnappedVertically)
                pMax.Y -= 3;

            if (!isSnappedHorizontally)
                pMax.X -= 3;
        }

        ImGui.SetCursorScreenPos(pMin);
        ImGui.PushID(item.Id.GetHashCode());
        ImGui.InvisibleButton(string.Empty, pMax - pMin);
        _itemMovement.Handle(item, this);
        ImGui.PopID();

        drawList.AddRectFilled(pMin, pMax, ColorVariations.OperatorBackground.Apply(typeColor).Fade(0.7f), 0);

        var isSelected = NodeSelection.IsNodeSelected(item);
        var outlineColor = isSelected
                               ? UiColors.ForegroundFull
                               : UiColors.BackgroundFull.Fade(0.3f);
        drawList.AddRect(pMin, pMax, outlineColor, 0);

        ImGui.PushFont(Fonts.FontBold);
        var labelSize = ImGui.CalcTextSize(item.ReadableName);
        ImGui.PopFont();
        var downScale = MathF.Min(1, MagGraphItem.Width * 0.9f / labelSize.X );

        drawList.AddText(Fonts.FontBold,
                         Fonts.FontBold.FontSize * downScale * CanvasScale ,
                         pMin + new Vector2(8, 9) * CanvasScale,
                         labelColor,
                         item.ReadableName);

        // Draw input labels
        int inputIndex;
        for (inputIndex = 1; inputIndex < item.InputLines.Length; inputIndex++)
        {
            var inputLine = item.InputLines[inputIndex];
            drawList.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize * CanvasScale,
                             pMin + new Vector2(8, 9) * CanvasScale + new Vector2(0, GridSizeOnScreen.Y * (inputIndex)),
                             labelColor.Fade(0.7f),
                             inputLine.InputUi?.InputDefinition.Name ?? "?"
                             );
        }

        // Draw output labels
        for (var outputIndex = 1; outputIndex < item.OutputLines.Length; outputIndex++)
        {
            var outputLine = item.OutputLines[outputIndex];

            ImGui.PushFont(Fonts.FontSmall);
            var outputDefinitionName = outputLine.OutputUi.OutputDefinition.Name;
            var outputLabelSize = ImGui.CalcTextSize(outputDefinitionName);
            ImGui.PopFont();

            drawList.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize * CanvasScale,
                             pMin
                             + new Vector2(-8, 9) * CanvasScale
                             + new Vector2(0, GridSizeOnScreen.Y * (outputIndex + inputIndex - 1))
                             + new Vector2(MagGraphItem.Width * CanvasScale - outputLabelSize.X  * CanvasScale, 0),
                             labelColor.Fade(0.7f),
                             outputDefinitionName);
        }

        // Draw sockets
        foreach (var i in item.GetInputAnchors())
        {
            if (!TypeUiRegistry.TryGetPropertiesForType(i.ConnectionType, out var type2UiProperties))
                continue;
            
            var p = TransformPosition(i.PositionOnCanvas);
            if (i.Direction == MagGraphItem.Directions.Vertical)
            {
                drawList.AddTriangleFilled(p + new Vector2(-1.5f, 0) * CanvasScale * 1.5f,
                                           p + new Vector2(1.5f, 0) * CanvasScale * 1.5f,
                                           p + new Vector2(0, 2) * CanvasScale * 1.5f,
                                           ColorVariations.OperatorOutline.Apply(type2UiProperties.Color));
            }
            else
            {
                drawList.AddTriangleFilled(p + new Vector2(1, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f,
                                           p + new Vector2(1, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f,
                                           p + new Vector2(1, 0) + new Vector2(2, 0) * CanvasScale * 1.5f,
                                           ColorVariations.OperatorOutline.Apply(type2UiProperties.Color));
            }

            if (ShowDebug)
            {
                ImGui.SetCursorScreenPos(TransformPosition(i.PositionOnCanvas));
                ImGui.Button("##" + i.GetHashCode());
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("hash:" + i.ConnectionHash);
            }
        }

        foreach (var oa in item.GetOutputAnchors())
        {
            if (!TypeUiRegistry.TryGetPropertiesForType(oa.ConnectionType, out var type2UiProperties))
                continue;
            
            var p = TransformPosition(oa.PositionOnCanvas);
            var color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(0.7f);
            if (oa.Direction == MagGraphItem.Directions.Vertical)
            {
                drawList.AddTriangleFilled(p + new Vector2(0, -1) + new Vector2(-1.5f, 0) * CanvasScale * 1.5f,
                                           p + new Vector2(0, -1) + new Vector2(1.5f, 0) * CanvasScale * 1.5f,
                                           p + new Vector2(0, -1) + new Vector2(0, 2) * CanvasScale * 1.5f,
                                           color);
            }
            else
            {
                drawList.AddTriangleFilled(p + new Vector2(0, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f,
                                           p + new Vector2(0, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f,
                                           p + new Vector2(0, 0) + new Vector2(2, 0) * CanvasScale * 1.5f,
                                           color);
            }

            if (ShowDebug)
            {
                ImGui.SetCursorScreenPos(TransformPosition(oa.PositionOnCanvas));
                ImGui.Button("##" + oa.GetHashCode());
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("hash:" + oa.ConnectionHash);
            }
            //drawList.AddCircle(TransformPosition(i.PositionOnCanvas), 3, type2UiProperties.Color, 4);
        }
    }

    private void DrawConnection(MagGraphConnection connection, ImDrawListPtr drawList)
    {
        if (connection.Style == MagGraphConnection.ConnectionStyles.Unknown)
            return;

        var type = connection.TargetItem.InputLines[connection.InputLineIndex].Type;

        if (!TypeUiRegistry.TryGetPropertiesForType(type, out var typeUiProperties))
            return;
        
        var anchorSize = 3 * CanvasScale;
        var typeColor = typeUiProperties.Color;
        var sourcePosOnScreen = TransformPosition(connection.SourcePos);
        var targetPosOnScreen = TransformPosition(connection.TargetPos);

        if (connection.IsSnapped)
        {
            switch (connection.Style)
            {
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, typeColor, 3);
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                    drawList.AddTriangleFilled(
                                               sourcePosOnScreen + new Vector2(-1, -1) * CanvasScale * 4,
                                               sourcePosOnScreen + new Vector2(1, -1) * CanvasScale * 4,
                                               sourcePosOnScreen + new Vector2(0, 1) * CanvasScale * 4,
                                               typeColor);
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, typeColor, 3);
                    break;
                case MagGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, Color.Red, 3);
                    break;
            }
        }
        else
        {
            var d = Vector2.Distance(sourcePosOnScreen, targetPosOnScreen) / 2;

            switch (connection.Style)
            {
                case MagGraphConnection.ConnectionStyles.BottomToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    break;
                case MagGraphConnection.ConnectionStyles.BottomToLeft:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(0, d),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    break;
                case MagGraphConnection.ConnectionStyles.RightToTop:
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(0, d),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    break;
                case MagGraphConnection.ConnectionStyles.RightToLeft:
                    var hoverPositionOnLine = Vector2.Zero;
                    var isHovering = ArcConnection.Draw( Scale,
                                                         new ImRect(sourcePosOnScreen, sourcePosOnScreen + new Vector2(10, 10)),
                                                        sourcePosOnScreen,
                                                        ImRect.RectWithSize(
                                                                            TransformPosition(connection.TargetItem.PosOnCanvas),
                                                                            TransformDirection(connection.TargetItem.Size)),
                                                        targetPosOnScreen,
                                                        typeColor,
                                                        2,
                                                        ref hoverPositionOnLine);

                    // const float minDistanceToTargetSocket = 10;
                    // if (isHovering && Vector2.Distance(hoverPositionOnLine, TargetPosition) > minDistanceToTargetSocket
                    //                && Vector2.Distance(hoverPositionOnLine, SourcePosition) > minDistanceToTargetSocket)
                    // {
                    //     ConnectionSplitHelper.RegisterAsPotentialSplit(Connection, ColorForType, hoverPositionOnLine);
                    // }                        

                    // drawList.AddBezierCubic(sourcePosOnScreen,
                    //                         sourcePosOnScreen + new Vector2(d, 0),
                    //                         targetPosOnScreen - new Vector2(d, 0),
                    //                         targetPosOnScreen,
                    //                         typeColor.Fade(0.6f),
                    //                         2);
                    break;
                case MagGraphConnection.ConnectionStyles.Unknown:
                    break;
            }
        }
    }

    // private void DrawSlot(ImDrawListPtr drawList, Vector2 sourcePosOnScreen, float scale, SnapGraphItem.Directions horizontal, Color typeColor)
    // {
    //     drawList.AddTriangleFilled(
    //                                sourcePosOnScreen + new Vector2(-1, -1) * scale,
    //                                sourcePosOnScreen + new Vector2(1, -1) * scale,
    //                                sourcePosOnScreen + new Vector2(0, 1) * scale,
    //                                typeColor);
    // }

    private void HandleFenceSelection(Instance compositionOp, SelectionFence selectionFence)
    {
        const bool allowRectOutOfBounds = true;
        switch (selectionFence.UpdateAndDraw(out var selectMode, allowRectOutOfBounds))
        {
            case SelectionFence.States.PressedButNotMoved:
                if (selectMode == SelectionFence.SelectModes.Replace)
                    NodeSelection.Clear();
                break;

            case SelectionFence.States.Updated:
                var bounds = allowRectOutOfBounds ? selectionFence.BoundsUnclamped : selectionFence.BoundsInScreen;
                HandleSelectionFenceUpdate(bounds, compositionOp, selectMode);
                break;

            case SelectionFence.States.CompletedAsClick:
                // A hack to prevent clearing selection when opening parameter popup
                if (ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
                    break;

                NodeSelection.Clear();
                NodeSelection.SetSelectionToComposition(compositionOp);
                break;
        }
    }

    private SelectionFence.States _fenceState = SelectionFence.States.Inactive;

    // TODO: Support non graph items like annotations.
    private void HandleSelectionFenceUpdate(ImRect bounds, Instance compositionOp, SelectionFence.SelectModes selectMode)
    {
        var boundsInCanvas = InverseTransformRect(bounds);
        var itemsInFence = (from child in _magGraphLayout.Items.Values
                            let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                            where rect.Overlaps(boundsInCanvas)
                            select child).ToList();

        if (selectMode == SelectionFence.SelectModes.Replace)
        {
            NodeSelection.Clear();
        }

        foreach (var item in itemsInFence)
        {
            if (selectMode == SelectionFence.SelectModes.Remove)
            {
                NodeSelection.DeselectNode(item, item.Instance);
            }
            else
            {
                if (item.Category == MagGraphItem.Categories.Operator)
                {
                    NodeSelection.AddSelection(item, item.Instance);
                }
                else
                {
                    NodeSelection.AddSelection(item);
                }
            }
        }
    }

    private void CenterView()
    {
        var visibleArea = new ImRect();
        foreach (var item in _magGraphLayout.Items.Values)
        {
            visibleArea.Add(item.PosOnCanvas);
        }

        FitAreaOnCanvas(visibleArea);
    }

    private readonly MagItemMovement _itemMovement;

    private readonly MagGraphLayout _magGraphLayout = new();

    private readonly MagGraphWindow _window;
    internal readonly NodeSelection NodeSelection;
    private Instance _compositionOp;
    private readonly SelectionFence _selectionFence = new();
    private Vector2 GridSizeOnScreen => TransformDirection(MagGraphItem.GridSize);
    private float CanvasScale => Scale.X;
    private static bool ShowDebug => ImGui.GetIO().KeyCtrl;
}