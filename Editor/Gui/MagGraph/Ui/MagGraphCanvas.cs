#nullable enable
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

/**
 * Draws and handles interaction with graph.
 */
internal sealed class MagGraphCanvas : ScalableCanvas
{
    public MagGraphCanvas(MagGraphWindow window, NodeSelection nodeSelection)
    {
        EnableParentZoom = false;
        _window = window;
        _nodeSelection = nodeSelection;
        _itemMovement = new MagItemMovement(this, _graphLayout, nodeSelection);
    }

    public void Draw()
    {
        _compositionOp = _window.CompositionOp;
        if (_compositionOp == null)
            return;

        _graphLayout.ComputeLayout(_compositionOp);
        _itemMovement.PrepareFrame();

        if (ImGui.Button("Center"))
        {
            CenterView();
        }

        ImGui.SameLine(0, 5);
        if (ImGui.Button("Rescan"))
        {
            _graphLayout.ComputeLayout(_compositionOp, forceUpdate: true);
        }

        ImGui.SameLine(0, 5);
        ImGui.Checkbox("Debug", ref _enableDebug);

        //Log.Debug("Updating canvas...");
        UpdateCanvas(out _);
        var drawList = ImGui.GetWindowDrawList();

        DrawBackgroundGrids(drawList);

        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup)
            && ConnectionMaker.GetTempConnectionsFor(_window).Count == 0)
            HandleFenceSelection(_window.CompositionOp, _selectionFence);

        foreach (var item in _graphLayout.Items.Values)
        {
            DrawNode(item, drawList);
        }

        foreach (var connection in _graphLayout.MagConnections)
        {
            DrawConnection(connection, drawList);
        }

        // Draw animated Snap indicator
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

        _itemMovement.CompleteFrame();
    }

    private void DrawBackgroundGrids(ImDrawListPtr drawList)
    {
        var minSize = MathF.Min(MagGraphItem.GridSize.X, MagGraphItem.GridSize.Y);
        var gridSize = Vector2.One * minSize;
        var maxOpacity = 0.25f;

        var fineGrid = MathUtils.RemapAndClamp(Scale.X, 0.5f, 2f, 0.0f, maxOpacity);
        if (fineGrid > 0.01f)
        {
            var color = UiColors.BackgroundFull.Fade(fineGrid);
            DrawBackgroundGrid(drawList, gridSize, color);
        }

        var roughGrid = MathUtils.RemapAndClamp(Scale.X, 0.1f, 2f, 0.0f, maxOpacity);
        if (roughGrid > 0.01f)
        {
            var color = UiColors.BackgroundFull.Fade(roughGrid);
            DrawBackgroundGrid(drawList, gridSize * 5, color);
        }
    }

    private void DrawBackgroundGrid(ImDrawListPtr drawList, Vector2 gridSize, Color color)
    {
        var window = new ImRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());

        var topLeftOnCanvas = InverseTransformPositionFloat(ImGui.GetWindowPos());
        var alignedTopLeftCanvas = new Vector2((int)(topLeftOnCanvas.X / gridSize.X) * gridSize.X,
                                               (int)(topLeftOnCanvas.Y / gridSize.Y) * gridSize.Y);

        var topLeftOnScreen = TransformPosition(alignedTopLeftCanvas);
        var screenGridSize = TransformDirection(gridSize);

        var count = new Vector2(window.GetWidth() / screenGridSize.X, window.GetHeight() / screenGridSize.Y);

        for (int ix = 0; ix < 200 && ix <= count.X + 1; ix++)
        {
            var x = (int)(topLeftOnScreen.X + ix * screenGridSize.X);
            drawList.AddRectFilled(new Vector2(x, window.Min.Y),
                                   new Vector2(x + 1, window.Max.Y),
                                   color);
        }

        for (int iy = 0; iy < 200 && iy <= count.Y + 1; iy++)
        {
            var y = (int)(topLeftOnScreen.Y + iy * screenGridSize.Y);
            drawList.AddRectFilled(new Vector2(window.Min.X, y),
                                   new Vector2(window.Max.X, y + 1),
                                   color);
        }

        // Commented out. Sadly drawing a point raster creates too many polys and eventually
        // will case rendering artifacts...
        //
        // for (int ix = 0; ix < 200 && ix <= count.X+1; ix++)
        // {
        //     for (int iy = 0; iy < 200 && iy <= count.Y+1; iy++)
        //     {
        //         var pOnScreen = topLeftOnScreen + new Vector2(ix, iy) * screenGridSize;
        //         drawList.AddRectFilled(pOnScreen, pOnScreen + Vector2.One, color);   
        //     }
        // }
    }

    [Flags]
    private enum Borders
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8,
    }

    private static readonly ImDrawFlags[] _borderRoundings =
        {
            ImDrawFlags.RoundCornersAll, //        0000      
            ImDrawFlags.RoundCornersBottom, //     0001                 up
            ImDrawFlags.RoundCornersLeft, //       0010           right
            ImDrawFlags.RoundCornersBottomLeft, // 0011           right up
            ImDrawFlags.RoundCornersTop, //        0100      down
            ImDrawFlags.RoundCornersNone, //       0101      down       up
            ImDrawFlags.RoundCornersTopLeft, // 0110      down right  
            ImDrawFlags.RoundCornersNone, //       0111      down right up  
            
            ImDrawFlags.RoundCornersRight, //      1000 left
            ImDrawFlags.RoundCornersBottomRight, //1001 left            up
            ImDrawFlags.RoundCornersNone, //       1010 left      right
            ImDrawFlags.RoundCornersNone, //       1011 left      right up
            ImDrawFlags.RoundCornersTopRight, //       1100 left down
            ImDrawFlags.RoundCornersNone, //       1101 left down       up
            ImDrawFlags.RoundCornersNone, //       1110 left down right  
            ImDrawFlags.RoundCornersNone, //       1111 left down right up  

        };

    private void DrawNode(MagGraphItem item, ImDrawListPtr drawList)
    {
        if (_compositionOp == null)
            return;

        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(item.PrimaryType);

        var typeColor = typeUiProperties.Color;
        var labelColor = ColorVariations.OperatorLabel.Apply(typeColor);

        var pMin = TransformPosition(item.PosOnCanvas);
        var pMax = TransformPosition(item.PosOnCanvas + item.Size);
        var pMinVisible = pMin;
        var pMaxVisible = pMax;

        // Adjust size when snapped
        // var isSnappedLeft = false;
        // var isSnappedUp = false;
        // var isSnappedDown = false;
        // var isSnappedRight = false;
        var snappedBorders = Borders.None;

        {
            for (var index = 0; index < 1 && index < item.InputLines.Length; index++)
            {
                ref var il = ref item.InputLines[index];
                var c = il.ConnectionIn;
                if (c != null)
                {
                    if (c.IsSnapped)
                    {
                        switch (c.Style)
                        {
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                                snappedBorders |= Borders.Up;
                                break;
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                                snappedBorders |= Borders.Left;
                                break;
                        }
                    }
                }
            }

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
                                snappedBorders |= Borders.Down;
                                break;
                            case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                                snappedBorders |= Borders.Right;
                                break;
                        }
                    }
                }
            }

            
            
            // There is probably a better method than this...
            const int snapPadding = 1;
            if (!snappedBorders.HasFlag(Borders.Down)) pMaxVisible.Y -= snapPadding * CanvasScale;
            if (!snappedBorders.HasFlag(Borders.Right)) pMaxVisible.X -= snapPadding * CanvasScale;
            if (!snappedBorders.HasFlag(Borders.Up)) pMinVisible.Y += snapPadding * CanvasScale;
            if (!snappedBorders.HasFlag(Borders.Left)) pMinVisible.X += snapPadding * CanvasScale;
        }

        // ImGUI element for selection
        ImGui.SetCursorScreenPos(pMin);
        ImGui.PushID(item.Id.GetHashCode());
        ImGui.InvisibleButton(string.Empty, pMax - pMin);
        _itemMovement.HandleForItem(item, this, _compositionOp);
        ImGui.PopID();

        // Background and Outline

        var imDrawFlags = _borderRoundings[(int)snappedBorders % 16];

        drawList.AddRectFilled(pMinVisible, pMaxVisible - Vector2.One, ColorVariations.OperatorBackground.Apply(typeColor).Fade(0.7f), 6 * CanvasScale, imDrawFlags);

        var isSelected = item.IsSelected(_nodeSelection);
        var outlineColor = isSelected
                               ? UiColors.ForegroundFull
                               : UiColors.BackgroundFull.Fade(0f);
        drawList.AddRect(pMinVisible, pMaxVisible, outlineColor, 6 * CanvasScale, imDrawFlags);

        // Label...
        ImGui.PushFont(Fonts.FontBold);
        var labelSize = ImGui.CalcTextSize(item.ReadableName);
        ImGui.PopFont();
        var downScale = MathF.Min(1, MagGraphItem.Width * 0.9f / labelSize.X);

        var labelPos = pMin + new Vector2(8, 7) * CanvasScale + new Vector2(0, -1);
        labelPos = new Vector2(MathF.Round(labelPos.X), MathF.Round(labelPos.Y));
        drawList.AddText(Fonts.FontBold,
                         Fonts.FontBold.FontSize * downScale * CanvasScale,
                         labelPos,
                         labelColor,
                         item.ReadableName);

        // Input labels...
        int inputIndex;
        for (inputIndex = 1; inputIndex < item.InputLines.Length; inputIndex++)
        {
            var inputLine = item.InputLines[inputIndex];
            drawList.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize * CanvasScale,
                             pMin + new Vector2(8, 9) * CanvasScale + new Vector2(0, GridSizeOnScreen.Y * (inputIndex)),
                             labelColor.Fade(0.7f),
                             inputLine.InputUi.InputDefinition.Name ?? "?"
                            );
        }

        // Draw output labels...
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
                             + new Vector2(MagGraphItem.Width * CanvasScale - outputLabelSize.X * CanvasScale, 0),
                             labelColor.Fade(0.7f),
                             outputDefinitionName);
        }

        // Draw input sockets
        //var blinkFactor = MathF.Sin((float)ImGui.GetTime()/3.15f*20) /2 + 0.5f;

        foreach (var inputAnchor in item.GetInputAnchors())
        {
            var isAlreadyUsed = inputAnchor.ConnectionHash != 0;
            if (isAlreadyUsed)
            {
                continue;
            }

            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(inputAnchor.ConnectionType);
            var p = TransformPosition(inputAnchor.PositionOnCanvas);
            var blinkFactor = (float)((ImGui.GetTime() + (p - ImGui.GetMousePos()).Length() * 0.001f) % 1);
            var color = ColorVariations.OperatorOutline.Apply(type2UiProperties.Color);
            var isPotentialDragTarget = inputAnchor.ConnectionType == _itemMovement.DraggedPrimaryOutputType
                                        && !MagItemMovement.IsItemDragged(item);

            if (isPotentialDragTarget)
            {
                drawList.AddCircleFilled(p, 2 + 15 * blinkFactor, color.Fade((1 - blinkFactor) * 0.7f));
            }

            if (inputAnchor.Direction == MagGraphItem.Directions.Vertical)
            {
                var pp = new Vector2(p.X + 2, pMinVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(-1.5f, 0) * CanvasScale * 2.5f,
                                           pp + new Vector2(1.5f, 0) * CanvasScale * 2.5f,
                                           pp + new Vector2(0, 2) * CanvasScale * 2.5f,
                                           color);
            }
            else
            {
                var pp = new Vector2(pMinVisible.X-1, p.Y );
                drawList.AddTriangleFilled(pp + new Vector2(1, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f,
                                           pp + new Vector2(1, 0) + new Vector2(2, 0) * CanvasScale * 1.5f,
                                           pp + new Vector2(1, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f,
                                           color);
            }

            ShowAnchorPointDebugs(inputAnchor, true);
        }

        // Draw output sockets
        foreach (var oa in item.GetOutputAnchors())
        {
            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(oa.ConnectionType);

            var p = TransformPosition(oa.PositionOnCanvas);
            var color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(0.7f);

            if (oa.Direction == MagGraphItem.Directions.Vertical)
            {
                var pp = new Vector2(p.X, pMaxVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(0, -1) + new Vector2(-1.5f, 0) * CanvasScale * 1.5f,
                                           pp + new Vector2(0, -1) + new Vector2(1.5f, 0) * CanvasScale * 1.5f,
                                           pp + new Vector2(0, -1) + new Vector2(0, 2) * CanvasScale * 1.5f,
                                           color);
            }
            else
            {
                var pp = new Vector2(pMaxVisible.X - 1, p.Y);

                drawList.AddTriangleFilled(pp + new Vector2(0, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f,
                                           pp + new Vector2(0, 0) + new Vector2(2, 0) * CanvasScale * 1.5f,
                                           pp + new Vector2(0, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f,
                                           color);
            }

            ShowAnchorPointDebugs(oa);
        }
    }

    private void ShowAnchorPointDebugs(MagGraphItem.AnchorPoint a, bool isInput = false)
    {
        if (!ShowDebug || ImGui.IsMouseDown(ImGuiMouseButton.Left))
            return;

        ImGui.PushFont(Fonts.FontSmall);
        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(a.ConnectionType);

        ImGui.SetCursorScreenPos(TransformPosition(a.PositionOnCanvas) - Vector2.One * ImGui.GetFrameHeight() / 2);
        ImGui.PushStyleColor(ImGuiCol.Text, typeUiProperties.Color.Rgba);
        ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
        var label = isInput ? "I" : "O";
        ImGui.Button($"{label}##{a.GetHashCode()}");
        ImGui.PopStyleColor(2);
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            //ImGui.SetTooltip("hash:" + oa.ConnectionHash);
            ImGui.Text(isInput ? "Input" : "Output");
            ImGui.Text("" + a.ConnectionType.Name);
            ImGui.Text("" + a.ConnectionHash);
            ImGui.EndTooltip();
        }

        ImGui.PopFont();
    }

    private static float Blink => MathF.Sin((float)ImGui.GetTime() * 10) * 0.5f + 0.5f;

    private void DrawConnection(MagGraphConnection connection, ImDrawListPtr drawList)
    {
        if (connection.Style == MagGraphConnection.ConnectionStyles.Unknown)
            return;

        var type = connection.Type;

        // if (!TypeUiRegistry.TryGetPropertiesForType(type, out var typeUiProperties))
        //     return;

        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(type);

        var anchorSize = 3 * CanvasScale;
        var typeColor = typeUiProperties.Color;
        var sourcePosOnScreen = TransformPosition(connection.SourcePos);
        var targetPosOnScreen = TransformPosition(connection.TargetPos);

        if (connection.IsSnapped)
        {
            switch (connection.Style)
            {
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                {
                    var isPotentialSplitTarget = _itemMovement.SplitInsertionPoints.Count > 0
                                                 && _itemMovement.SplitInsertionPoints
                                                                 .Any(x
                                                                          => x.Direction == MagGraphItem.Directions.Horizontal
                                                                             && x.Type == type);
                    if (isPotentialSplitTarget)
                    {
                        var extend = new Vector2(0, MagGraphItem.GridSize.Y * CanvasScale * 0.4f);

                        drawList.AddRectFilled(
                                               sourcePosOnScreen - extend + new Vector2(-1, 0),
                                               sourcePosOnScreen + extend + new Vector2(0, 0),
                                               typeColor.Fade(Blink)
                                              );
                    }

                    drawList.AddCircleFilled(sourcePosOnScreen, anchorSize * 1.6f, typeColor, 3);
                    break;
                }
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                {
                    var isPotentialSplitTarget = _itemMovement.SplitInsertionPoints.Count > 0
                                                 && _itemMovement.SplitInsertionPoints
                                                                 .Any(x
                                                                          => x.Direction == MagGraphItem.Directions.Vertical
                                                                             && x.Type == type);
                    if (isPotentialSplitTarget)
                    {
                        var extend = new Vector2(MagGraphItem.GridSize.X * CanvasScale * 0.4f, 0);

                        drawList.AddRectFilled(
                                               sourcePosOnScreen - extend + new Vector2(0, -1),
                                               sourcePosOnScreen + extend + new Vector2(0, 0),
                                               typeColor.Fade(Blink)
                                              );
                    }

                    drawList.AddTriangleFilled(
                                               sourcePosOnScreen + new Vector2(-1, -1) * CanvasScale * 4,
                                               sourcePosOnScreen + new Vector2(1, -1) * CanvasScale * 4,
                                               sourcePosOnScreen + new Vector2(0, 1) * CanvasScale * 4,
                                               typeColor);
                    break;
                }
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
                    // break;
                    //case MagGraphConnection.ConnectionStyles.RightToLeft:
                    //var hoverPositionOnLine = Vector2.Zero;
                    // var isHovering = ArcConnection.Draw( Scale,
                    //                                      new ImRect(sourcePosOnScreen, sourcePosOnScreen + new Vector2(10, 10)),
                    //                                     sourcePosOnScreen,
                    //                                     ImRect.RectWithSize(
                    //                                                         TransformPosition(connection.TargetItem.PosOnCanvas),
                    //                                                         TransformDirection(connection.TargetItem.Size)),
                    //                                     targetPosOnScreen,
                    //                                     typeColor,
                    //                                     2,
                    //                                     ref hoverPositionOnLine);

                    // const float minDistanceToTargetSocket = 10;
                    // if (isHovering && Vector2.Distance(hoverPositionOnLine, TargetPosition) > minDistanceToTargetSocket
                    //                && Vector2.Distance(hoverPositionOnLine, SourcePosition) > minDistanceToTargetSocket)
                    // {
                    //     ConnectionSplitHelper.RegisterAsPotentialSplit(Connection, ColorForType, hoverPositionOnLine);
                    // }                        
                    //
                    drawList.AddBezierCubic(sourcePosOnScreen,
                                            sourcePosOnScreen + new Vector2(d, 0),
                                            targetPosOnScreen - new Vector2(d, 0),
                                            targetPosOnScreen,
                                            typeColor.Fade(0.6f),
                                            2);
                    break;
                case MagGraphConnection.ConnectionStyles.Unknown:
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedHorizontal:
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToMainInSnappedVertical:
                    break;
                case MagGraphConnection.ConnectionStyles.MainOutToInputSnappedHorizontal:
                    break;
                case MagGraphConnection.ConnectionStyles.AdditionalOutToMainInputSnappedVertical:
                    break;
            }
        }
    }

    private void HandleFenceSelection(Instance compositionOp, SelectionFence selectionFence)
    {
        switch (selectionFence.UpdateAndDraw(out var selectMode))
        {
            case SelectionFence.States.PressedButNotMoved:
                if (selectMode == SelectionFence.SelectModes.Replace)
                    _nodeSelection.Clear();
                break;

            case SelectionFence.States.Updated:
                HandleSelectionFenceUpdate(selectionFence.BoundsUnclamped, selectMode);
                break;

            case SelectionFence.States.CompletedAsClick:
                // A hack to prevent clearing selection when opening parameter popup
                if (ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
                    break;

                _nodeSelection.Clear();
                _nodeSelection.SetSelectionToComposition(compositionOp);
                break;
            case SelectionFence.States.Inactive:
                break;
            case SelectionFence.States.CompletedAsArea:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // TODO: Support non graph items like annotations.
    private void HandleSelectionFenceUpdate(ImRect bounds, SelectionFence.SelectModes selectMode)
    {
        var boundsInCanvas = InverseTransformRect(bounds);
        var itemsInFence = (from child in _graphLayout.Items.Values
                            let rect = new ImRect(child.PosOnCanvas, child.PosOnCanvas + child.Size)
                            where rect.Overlaps(boundsInCanvas)
                            select child).ToList();

        if (selectMode == SelectionFence.SelectModes.Replace)
        {
            _nodeSelection.Clear();
        }

        foreach (var item in itemsInFence)
        {
            if (selectMode == SelectionFence.SelectModes.Remove)
            {
                _nodeSelection.DeselectNode(item, item.Instance);
            }
            else
            {
                if (item.Variant == MagGraphItem.Variants.Operator)
                {
                    _nodeSelection.AddSelection(item, item.Instance);
                }
                else
                {
                    _nodeSelection.AddSelection(item);
                }
            }
        }
    }

    private void CenterView()
    {
        var visibleArea = new ImRect();
        foreach (var item in _graphLayout.Items.Values)
        {
            visibleArea.Add(item.PosOnCanvas);
        }

        FitAreaOnCanvas(visibleArea);
    }

    private readonly MagItemMovement _itemMovement;
    private readonly MagGraphLayout _graphLayout = new();

    private readonly MagGraphWindow _window;
    private readonly NodeSelection _nodeSelection;
    private Instance? _compositionOp;
    private readonly SelectionFence _selectionFence = new();
    private Vector2 GridSizeOnScreen => TransformDirection(MagGraphItem.GridSize);
    private float CanvasScale => Scale.X;
    public bool ShowDebug => ImGui.GetIO().KeyCtrl || _enableDebug;
    private bool _enableDebug;
}