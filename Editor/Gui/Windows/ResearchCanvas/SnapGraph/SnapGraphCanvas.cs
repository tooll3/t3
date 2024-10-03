using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
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
        
        UpdateCanvas();

        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
            && ConnectionMaker.TempConnections.Count == 0)
            HandleFenceSelection();

        var drawList = ImGui.GetWindowDrawList();
        foreach (var item in _snapGraphLayout.Items.Values)
        {
            DrawNode(item, drawList);
        }

        foreach (var connection in _snapGraphLayout.SnapConnections)
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

    private void DrawNode(SnapGraphItem item, ImDrawListPtr drawList)
    {
        if (!TypeUiRegistry.Entries.TryGetValue(item.PrimaryType, out var typeUiProperties))
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
                            case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                                isSnappedVertically = true;
                                break;
                            case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
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
        var downScale = MathF.Min(1, SnapGraphItem.Width * 0.9f / labelSize.X );

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
                             + new Vector2(SnapGraphItem.Width * CanvasScale - outputLabelSize.X  * CanvasScale, 0),
                             labelColor.Fade(0.7f),
                             outputDefinitionName);
        }

        // Draw sockets
        foreach (var i in item.GetInputAnchors())
        {
            if (!TypeUiRegistry.Entries.TryGetValue(i.ConnectionType, out var type2UiProperties))
                continue;

            var p = TransformPosition(i.PositionOnCanvas);
            if (i.Direction == SnapGraphItem.Directions.Vertical)
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
            if (!TypeUiRegistry.Entries.TryGetValue(oa.ConnectionType, out var type2UiProperties))
                continue;

            var p = TransformPosition(oa.PositionOnCanvas);
            var color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(0.7f);
            if (oa.Direction == SnapGraphItem.Directions.Vertical)
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

    private void DrawConnection(SnapGraphConnection connection, ImDrawListPtr drawList)
    {
        if (connection.Style == SnapGraphConnection.ConnectionStyles.Unknown)
            return;

        var type = connection.TargetItem.InputLines[connection.InputLineIndex].Type;
        if (!TypeUiRegistry.Entries.TryGetValue(type, out var typeUiProperties))
            return;

        var anchorSize = 3 * CanvasScale;
        var typeColor = typeUiProperties.Color;
        var sourcePosOnScreen = TransformPosition(connection.SourcePos);
        var targetPosOnScreen = TransformPosition(connection.TargetPos);

        if (connection.IsSnapped)
        {
            switch (connection.Style)
            {
                case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, typeColor, 3);
                    break;
                case SnapGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                    drawList.AddTriangleFilled(
                                               sourcePosOnScreen + new Vector2(-1, -1) * CanvasScale * 4,
                                               sourcePosOnScreen + new Vector2(1, -1) * CanvasScale * 4,
                                               sourcePosOnScreen + new Vector2(0, 1) * CanvasScale * 4,
                                               typeColor);
                    break;
                case SnapGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, typeColor, 3);
                    break;
                case SnapGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical:
                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, Color.Red, 3);
                    break;
            }
        }
        else
        {
            var d = Vector2.Distance(sourcePosOnScreen, targetPosOnScreen) / 2;

            switch (connection.Style)
            {
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
                    var hoverPositionOnLine = Vector2.Zero;
                    var isHovering = ArcConnection.Draw(new ImRect(sourcePosOnScreen, sourcePosOnScreen + new Vector2(10, 10)),
                                                        sourcePosOnScreen,
                                                        ImRect.RectWithSize(
                                                                            TransformPosition(connection.TargetItem.PosOnCanvas),
                                                                            TransformDirection(connection.TargetItem.Size)),
                                                        targetPosOnScreen,
                                                        typeColor,
                                                        CanvasScale,
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
                case SnapGraphConnection.ConnectionStyles.Unknown:
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
                if (item.Category == SnapGraphItem.Categories.Operator)
                {
                    NodeSelection.AddSymbolChildToSelection(item, item.Instance);
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
        foreach (var item in _snapGraphLayout.Items.Values)
        {
            visibleArea.Add(item.PosOnCanvas);
        }

        FitAreaOnCanvas(visibleArea);
    }

    private readonly SnapItemMovement _itemMovement;

    private readonly SnapGraphLayout _snapGraphLayout = new();

    //private bool _forceUpdating;
    private Instance _compositionOp;
    private Vector2 GridSizeOnScreen => TransformDirection(SnapGraphItem.GridSize);
    private float CanvasScale => Scale.X;
    private static bool ShowDebug => ImGui.GetIO().KeyCtrl;
}