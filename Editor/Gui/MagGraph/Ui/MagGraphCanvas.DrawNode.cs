using System.Diagnostics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.Graph.Rendering;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using Texture2D = T3.Core.DataTypes.Texture2D;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    private void DrawItem(MagGraphItem item, ImDrawListPtr drawList, GraphUiContext context)
    {
        if (item.Variant == MagGraphItem.Variants.Placeholder)
            return;

        if (!IsRectVisible(item.Area))
            return;

        var idleFadeFactor = 1f;
        if (item.Variant == MagGraphItem.Variants.Operator && item.Instance != null)
        {
            var framesSinceLastUpdate = 100;
            foreach (var output in item.Instance.Outputs)
            {
                framesSinceLastUpdate = Math.Min(framesSinceLastUpdate, output.DirtyFlag.FramesSinceLastUpdate);
            }

            idleFadeFactor = MathUtils.RemapAndClamp(framesSinceLastUpdate, 0f, 60f, 1f, 0.6f);
        }

        var hoverProgress = GetHoverTimeForId(item.Id).RemapAndClamp(0, 0.3f, 0, 1);

        var smallFontScaleFactor = CanvasScale.Clamp(0.5f, 2);

        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(item.PrimaryType);

        var typeColor = typeUiProperties.Color;
        var labelColor = ColorVariations.OperatorLabel.Apply(typeColor);

        var pMin = TransformPosition(item.DampedPosOnCanvas);
        var pMax = TransformPosition(item.DampedPosOnCanvas + item.Size);
        var pMinVisible = pMin;
        var pMaxVisible = pMax;

        // Adjust size when snapped
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
            // if (!snappedBorders.HasFlag(Borders.Up)) pMinVisible.Y += snapPadding * CanvasScale;
            // if (!snappedBorders.HasFlag(Borders.Left)) pMinVisible.X += snapPadding * CanvasScale;
        }

        // Background and Outline
        var imDrawFlags = _borderRoundings[(int)snappedBorders % 16];

        //var isHovered = _context.Selector.HoveredIds.Contains(item.Id);
        //var fade = isHovered ? 1 : 0.7f;

        drawList.AddRectFilled(pMinVisible + Vector2.One * CanvasScale,
                               pMaxVisible,
                               Color.Mix(ColorVariations.OperatorBackgroundIdle.Apply(typeColor),
                                         ColorVariations.OperatorBackground.Apply(typeColor), idleFadeFactor), 5 * CanvasScale,
                               imDrawFlags);

        var isSelected = _context.Selector.IsSelected(item);
        var outlineColor = isSelected
                               ? UiColors.ForegroundFull
                               : UiColors.BackgroundFull.Fade(0f);

        drawList.AddRect(pMinVisible, pMaxVisible, outlineColor, 6 * CanvasScale, imDrawFlags);

        // Custom Ui
        SymbolUi.Child.CustomUiResult customUiResult = SymbolUi.Child.CustomUiResult.None;
        if (item.Variant == MagGraphItem.Variants.Operator)
        {
            customUiResult = DrawCustomUi(item.Instance, drawList, new ImRect(pMinVisible + Vector2.One, pMaxVisible - Vector2.One), Vector2.One * CanvasScale);
        }

        // ImGUI element for selection
        ImGui.SetCursorScreenPos(pMin);
        ImGui.PushID(item.Id.GetHashCode());
        ImGui.InvisibleButton(string.Empty, pMax - pMin);
        var isItemHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup
                                                | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

        if (_context.StateMachine.CurrentState == GraphStates.Default
            && isItemHovered
            && !customUiResult.HasFlag(SymbolUi.Child.CustomUiResult.IsActive))
            _context.ActiveItem = item;

        if (customUiResult.HasFlag(SymbolUi.Child.CustomUiResult.IsActive))
        {
            context.StateMachine.SetState(GraphStates.Default, context);
        }

        ImGui.PopID();
        // Todo: We eventually need to handle right clicking to select and open context menu when dragging with right mouse button. 
        // var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        // if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
        //     && !wasDraggingRight
        //     && ImGui.IsItemHovered()
        //     && !_nodeSelection.IsNodeSelected(item))
        // {
        //     item.Select(_nodeSelection);
        // }

        if (customUiResult == SymbolUi.Child.CustomUiResult.None)
        {
            // Draw Texture thumbnail
            var hasPreview = TryDrawTexturePreview(item, pMinVisible, pMaxVisible, drawList, typeColor);

            // Label...
            var name = item.ReadableName;
            if (item.Variant == MagGraphItem.Variants.Output)
            {
                name = "OUT: " + name;
            }
            else if (item.Variant == MagGraphItem.Variants.Input)
            {
                name = "IN: " + name;
            }

            ImGui.PushFont(Fonts.FontBold);
            var labelSize = ImGui.CalcTextSize(name);
            ImGui.PopFont();

            var paddingForPreview = hasPreview ? MagGraphItem.LineHeight + 10 : 0;
            var downScale = MathF.Min(1f, (MagGraphItem.Width - paddingForPreview) * 0.9f / labelSize.X);

            var labelPos = pMin + new Vector2(8, 8) * CanvasScale + new Vector2(0, -1);
            labelPos = new Vector2(MathF.Round(labelPos.X), MathF.Round(labelPos.Y));
            drawList.AddText(Fonts.FontBold,
                             Fonts.FontBold.FontSize * downScale * CanvasScale.Clamp(0.1f, 2f),
                             labelPos,
                             labelColor,
                             name);
        }

        // Indicate hidden matching inputs...
        if (_context.DraggedPrimaryOutputType != null
            && item.Variant == MagGraphItem.Variants.Operator
            && _context.StateMachine.CurrentState == GraphStates.DragConnectionEnd
            && !context.ItemMovement.IsItemDragged(item)
            && _context.ActiveSourceItem != null)
        {
            Debug.Assert(item.Instance != null); // should be true to operator variant

            var hasMatchingTypes = false;
            foreach (var i in item.Instance.Inputs)
            {
                if (i.ValueType == _context.DraggedPrimaryOutputType
                    && !i.HasInputConnections)
                {
                    hasMatchingTypes = true;
                    break;
                }
            }

            if (hasMatchingTypes)
            {
                var indicatorPos = new Vector2(pMin.X, pMin.Y + MagGraphItem.GridSize.Y / 2 * CanvasScale);
                var isPeeked = item.Area.Contains(_context.PeekAnchorInCanvas);
                if (isPeeked)
                {
                    drawList.AddCircleFilled(indicatorPos, 4, UiColors.ForegroundFull);
                }
                else
                {
                    drawList.AddCircle(indicatorPos, 3, UiColors.ForegroundFull.Fade(Blink));
                }

                var isHoveredForInputPicking = ImRect.RectWithSize(item.DampedPosOnCanvas, item.Size).Contains(context.PeekAnchorInCanvas);
                if (isHoveredForInputPicking)
                {
                    drawList.AddRect(pMinVisible, pMaxVisible, UiColors.ForegroundFull, 6 * CanvasScale, imDrawFlags);

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 3);
                    ImGui.BeginTooltip();
                    var childUi = item.SymbolUi;
                    if (childUi != null)
                    {
                        if (!TypeNameRegistry.Entries.TryGetValue(_context.DraggedPrimaryOutputType, out var typeName))
                            typeName = _context.DraggedPrimaryOutputType.Name;

                        ImGui.PushFont(Fonts.FontSmall);
                        ImGui.TextColored(UiColors.TextMuted, typeName + " inputs");
                        ImGui.PopFont();

                        var inputIndex = 0;
                        foreach (var inputUi in childUi.InputUis.Values)
                        {
                            var input = item.Instance!.Inputs[inputIndex];
                            if (inputUi.Type == context.DraggedPrimaryOutputType)
                            {
                                var isConnected = input.HasInputConnections;
                                var prefix = isConnected ? "× " : "   ";
                                ImGui.Selectable(prefix + inputUi.InputDefinition.Name);
                            }

                            inputIndex++;
                        }
                    }

                    ImGui.EndTooltip();
                    ImGui.PopStyleVar(2);
                }
            }
        }

        // Missing primary input indicator
        if (item.InputLines.Length > 0 && item.InputLines[0].InputUi != null)
        {
            var inputLine = item.InputLines[0];
            var isMissing = inputLine.InputUi.Relevancy == Relevancy.Required && inputLine.ConnectionIn == null;
            if (isMissing)
            {
                drawList.AddCircleFilled(pMin
                                         //+ new Vector2(8, 9) * CanvasScale 
                                         + new Vector2(0, GridSizeOnScreen.Y * (0.5f)),
                                         3,
                                         UiColors.StatusAttention);
            }
        }

        // Input labels...
        if (!customUiResult.HasFlag(SymbolUi.Child.CustomUiResult.PreventInputLabels))
        {
            int inputIndex;
            for (inputIndex = 1; inputIndex < item.InputLines.Length; inputIndex++)
            {
                var inputLine = item.InputLines[inputIndex];
                var isMissing = inputLine.InputUi.Relevancy == Relevancy.Required && inputLine.ConnectionIn == null;
                if (isMissing)
                {
                    drawList.AddCircleFilled(pMin
                                             + new Vector2(0, GridSizeOnScreen.Y * (inputIndex + 0.5f)),
                                             3,
                                             UiColors.StatusAttention);
                }

                var labelPos = pMin + new Vector2(8, 9) * CanvasScale + new Vector2(0, GridSizeOnScreen.Y * inputIndex);
                var label = inputLine.InputUi.InputDefinition.Name ?? "?";
                if (inputLine.MultiInputIndex > 0)
                {
                    label += " +" + inputLine.MultiInputIndex;
                }

                drawList.AddText(Fonts.FontSmall,
                                 Fonts.FontSmall.FontSize * Fonts.FontSmall.Scale * smallFontScaleFactor,
                                 labelPos,
                                 labelColor.Fade(0.7f),
                                 label
                                );

                // Draw Value if possible
                if (CanvasScale > 0.4f)
                {
                    var inputSlot = item.Instance.GetInput(inputLine.Input.Id);
                    var valueAsString = ValueUtils.GetValueString(inputSlot);

                    if (!string.IsNullOrWhiteSpace(valueAsString))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor.Rgba);
                        var valueColor = labelColor;
                        valueColor.Rgba.W *= 0.6f;

                        ImGui.PushFont(Fonts.FontSmall);
                        var labelSize = ImGui.CalcTextSize(valueAsString) * smallFontScaleFactor;
                        ImGui.PopFont();

                        var valuePos = new Vector2(pMin.X + (item.Size.X - 5) * CanvasScale - labelSize.X, labelPos.Y);

                        if (!string.IsNullOrEmpty(valueAsString))
                        {
                            drawList.AddText(Fonts.FontSmall,
                                             Fonts.FontSmall.FontSize * Fonts.FontSmall.Scale * smallFontScaleFactor,
                                             valuePos,
                                             labelColor.Fade(0.5f),
                                             valueAsString
                                            );
                        }

                        ImGui.PopStyleColor();
                    }
                }
            }

            // Draw output labels...
            for (var outputIndex = 1; outputIndex < item.OutputLines.Length; outputIndex++)
            {
                var outputLine = item.OutputLines[outputIndex];
                if (outputLine.OutputUi == null)
                    continue;

                ImGui.PushFont(Fonts.FontSmall);
                var outputDefinitionName = outputLine.OutputUi.OutputDefinition.Name;
                var outputLabelSize = ImGui.CalcTextSize(outputDefinitionName) * smallFontScaleFactor;
                ImGui.PopFont();

                drawList.AddText(Fonts.FontSmall,
                                 Fonts.FontSmall.FontSize * smallFontScaleFactor,
                                 pMin
                                 + new Vector2(-8, 9) * CanvasScale.Clamp(0.1f, 2f)
                                 + new Vector2(0, GridSizeOnScreen.Y * (outputIndex + inputIndex - 1))
                                 + new Vector2(MagGraphItem.Width * CanvasScale - outputLabelSize.X, 0),
                                 labelColor.Fade(0.7f),
                                 outputDefinitionName);
            }

            // Indicator primary output op peek position...
            if (_context.ActiveSourceItem != null && item.Id == _context.ActiveSourceItem.Id)
            {
                drawList.AddCircleFilled(TransformPosition(new Vector2(item.Area.Max.X - MagGraphItem.GridSize.Y * 0.25f,
                                                                       item.Area.Min.Y + MagGraphItem.GridSize.Y * 0.5f)),
                                         3 * CanvasScale,
                                         UiColors.ForegroundFull);
            }
        }

        if (item.Variant == MagGraphItem.Variants.Operator)
        {
            // Animation indicator
            var indicatorCount = 0;
            if (item.Instance.Symbol.Animator.IsInstanceAnimated(item.Instance))
            {
                DrawIndicator(drawList, UiColors.StatusAnimated, idleFadeFactor, pMin, pMax, ref indicatorCount);
            }

            // Pinned indicator
            if (context.Selector.PinnedIds.Contains(item.Instance.SymbolChildId))
            {
                DrawIndicator(drawList, UiColors.Selection, idleFadeFactor, pMin, pMax, ref indicatorCount);
            }

            // Snapshot indicator
            {
                if (item.ChildUi.EnabledForSnapshots)
                {
                    DrawIndicator(drawList, UiColors.StatusAutomated, idleFadeFactor, pMin, pMax, ref indicatorCount);
                }
            }

            // Disabled indicator
            if (item.SymbolChild.IsDisabled)
            {
                DrawUtils.DrawOverlayLine(drawList, idleFadeFactor, Vector2.Zero, Vector2.One, pMinVisible, pMaxVisible);
                DrawUtils.DrawOverlayLine(drawList, idleFadeFactor, new Vector2(1, 0), new Vector2(0, 1), pMinVisible, pMaxVisible);
            }

            // Bypass indicator
            if (item.SymbolChild.IsBypassed)
            {
                DrawUtils.DrawOverlayLine(drawList, idleFadeFactor, new Vector2(0.05f, 0.5f), new Vector2(0.4f, 0.5f), pMinVisible, pMaxVisible);
                DrawUtils.DrawOverlayLine(drawList, idleFadeFactor, new Vector2(0.6f, 0.5f), new Vector2(0.95f, 0.5f), pMinVisible, pMaxVisible);

                DrawUtils.DrawOverlayLine(drawList, idleFadeFactor, new Vector2(0.35f, 0.1f), new Vector2(0.65f, 0.9f), pMinVisible, pMaxVisible);
                DrawUtils.DrawOverlayLine(drawList, idleFadeFactor, new Vector2(0.65f, 0.1f), new Vector2(0.35f, 0.9f), pMinVisible, pMaxVisible);
            }

            if (!string.IsNullOrEmpty(item.ChildUi.Comment))
            {
                ImGui.SetCursorScreenPos(new Vector2(pMax.X, pMin.Y) - new Vector2(3, 12) * T3Ui.UiScaleFactor * T3Ui.UiScaleFactor);
                if (ImGui.InvisibleButton("#comment", new Vector2(15, 15)))
                {
                    context.Selector.SetSelection(item.ChildUi, item.Instance);
                    context.EditCommentDialog.ShowNextFrame();
                }

                Icons.DrawIconOnLastItem(Icon.Comment, UiColors.ForegroundFull);
                CustomComponents.TooltipForLastItem(UiColors.Text, item.ChildUi.Comment, null, false);
            }
        }

        // Draw free input sockets...
        foreach (var inputAnchor in item.GetInputAnchors())
        {
            var isMultiInput = inputAnchor.InputLine.InputUi.InputDefinition.IsMultiInput;
            var isAlreadyUsed = inputAnchor.SnappedConnectionHash != MagGraphItem.FreeAnchor;
            if (!isMultiInput && isAlreadyUsed)
                continue;

            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(inputAnchor.ConnectionType);
            var p = TransformPosition(inputAnchor.PositionOnCanvas);
            var color = ColorVariations.OperatorOutline.Apply(type2UiProperties.Color);

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
                var isPotentialConnectionStartDropTarget = _context.StateMachine.CurrentState == GraphStates.DragConnectionEnd
                                                           && _context.DraggedPrimaryOutputType == inputAnchor.ConnectionType;
                var showTriangleAnchor = true;

                if (isMultiInput)
                {
                    var isConnected = inputAnchor.InputLine.ConnectionIn != null;
                    if (isPotentialConnectionStartDropTarget)
                    {
                        color = ColorVariations.Highlight.Apply(type2UiProperties.Color);
                        DrawMultiInputIndicator(item, inputAnchor.SlotId, inputAnchor.InputLine.MultiInputIndex, drawList, inputAnchor.PositionOnCanvas, color,
                                                InputSnapper.InputSnapTypes.ReplaceMultiInput);

                        if (isConnected)
                        {
                            if (inputAnchor.InputLine.MultiInputIndex == 0)
                                DrawMultiInputIndicator(item, inputAnchor.SlotId, inputAnchor.InputLine.MultiInputIndex, drawList, inputAnchor.PositionOnCanvas,
                                                        color, InputSnapper.InputSnapTypes.InsertBeforeMultiInput);

                            DrawMultiInputIndicator(item, inputAnchor.SlotId, inputAnchor.InputLine.MultiInputIndex, drawList, inputAnchor.PositionOnCanvas,
                                                    color, InputSnapper.InputSnapTypes.InsertAfterMultiInput);
                        }

                        showTriangleAnchor = false;
                    }
                }
                else
                {
                    // Register for input snapping...
                    if (isPotentialConnectionStartDropTarget && inputAnchor.SnappedConnectionHash == MagGraphItem.FreeAnchor)
                    {
                        color = ColorVariations.Highlight.Apply(type2UiProperties.Color).Fade(Blink);
                        InputSnapper.RegisterAsPotentialTargetInput(item, p, inputAnchor.SlotId);
                    }
                }

                if (showTriangleAnchor)
                {
                    var pp = new Vector2(pMinVisible.X - 1, p.Y);
                    drawList.AddTriangleFilled(pp + new Vector2(1, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f,
                                               pp + new Vector2(1, 0) + new Vector2(2, 0) * CanvasScale * 1.5f,
                                               pp + new Vector2(1, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f,
                                               color);
                }
            }

            //ShowAnchorPointDebugs(inputAnchor, true);
        }

        var hoverFactor = hoverProgress.RemapAndClamp(0, 1, 1, 2);

        // Draw output sockets
        foreach (var outputAnchor in item.GetOutputAnchors())
        {
            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(outputAnchor.ConnectionType);

            var posOnCanvas = TransformPosition(outputAnchor.PositionOnCanvas);
            var color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(0.7f);

            Vector2 pp;
            if (outputAnchor.Direction == MagGraphItem.Directions.Vertical)
            {
                pp = new Vector2(posOnCanvas.X, pMaxVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(0, -1) + new Vector2(-1.5f, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, -1) + new Vector2(1.5f, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, -1) + new Vector2(0, 2) * CanvasScale * 1.5f * hoverFactor,
                                           color);
                pp += new Vector2(0, -3);
            }
            else
            {
                // Register for output snapping...
                var isPotentialConnectionStartDropTarget = _context.StateMachine.CurrentState == GraphStates.DragConnectionBeginning
                                                           && _context.DraggedPrimaryOutputType == outputAnchor.ConnectionType
                                                           && outputAnchor.SnappedConnectionHash == MagGraphItem.FreeAnchor;

                if (isPotentialConnectionStartDropTarget)
                {
                    color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(Blink);
                    OutputSnapper.RegisterAsPotentialTargetOutput(context, item, outputAnchor);
                }

                pp = new Vector2(pMaxVisible.X - 1, posOnCanvas.Y);

                drawList.AddTriangleFilled(pp + new Vector2(0, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(2, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f * hoverFactor,
                                           color);
                pp += new Vector2(-3, 0);
            }

            // Draw primary output socket for drag or click
            if (isItemHovered && context.StateMachine.CurrentState == GraphStates.Default)
            {
                var color2 = ColorVariations.OperatorLabel.Apply(type2UiProperties.Color).Fade(0.7f * hoverProgress);
                var circleCenter = pp;
                var mouseDistance = Vector2.Distance(ImGui.GetMousePos(), circleCenter);

                var animationStartRadius = 30 * CanvasScale;
                var animationEndRadius = 10;
                var mouseDistanceFactor = mouseDistance.RemapAndClamp(animationStartRadius, animationEndRadius, 0.6f, 1.1f);

                var isActivated = mouseDistance < 7 * CanvasScale;
                if (isActivated)
                {
                    drawList.AddCircleFilled(circleCenter, 5 * CanvasScale, color2);

                    var e = MathF.Round(2 * CanvasScale);
                    drawList.AddRectFilled(circleCenter + new Vector2(-e, 0),
                                           circleCenter + new Vector2(e + 1, 1),
                                           UiColors.BackgroundFull);

                    drawList.AddRectFilled(circleCenter + new Vector2(0, -e),
                                           circleCenter + new Vector2(1, e + 1),
                                           UiColors.BackgroundFull);

                    // This will later be used for by DefaultState to create a connection
                    _context.ActiveSourceOutputId = outputAnchor.SlotId;
                    _context.ActiveOutputDirection = outputAnchor.Direction;
                }
                else
                {
                    drawList.AddCircle(circleCenter, 3 * hoverFactor * mouseDistanceFactor, color2);
                }
            }

            //ShowAnchorPointDebugs(outputAnchor);
        }
    }

    private void DrawMultiInputIndicator(MagGraphItem item, Guid slotId, int multiInputIndex, ImDrawListPtr drawList, Vector2 inputPosOnCanvas, Color color,
                                         InputSnapper.InputSnapTypes snapType)
    {
        var verticalOffset = snapType switch
                                 {
                                     InputSnapper.InputSnapTypes.Normal                 => 0,
                                     InputSnapper.InputSnapTypes.InsertBeforeMultiInput => -1f,
                                     InputSnapper.InputSnapTypes.ReplaceMultiInput      => 0,
                                     InputSnapper.InputSnapTypes.InsertAfterMultiInput  => +1f,
                                     _                                                  => 0f
                                 };

        var padding = MagGraphItem.GridSize.Y * 0.15f;
        var snapPosOnCanvas = inputPosOnCanvas
                              + new Vector2(padding, MagGraphItem.LineHeight * 0.5f * verticalOffset);

        snapPosOnCanvas.Y = snapPosOnCanvas.Y.Clamp(item.PosOnCanvas.Y + padding, item.PosOnCanvas.Y + item.Size.Y - padding);

        if (snapType != InputSnapper.InputSnapTypes.ReplaceMultiInput)
            drawList.AddCircleFilled(TransformPosition(snapPosOnCanvas), 2 * CanvasScale, color.Fade(0.5f), 8);

        var pOnScreen = TransformPosition(snapPosOnCanvas);
        InputSnapper.RegisterAsPotentialTargetInput(item, pOnScreen, slotId, snapType, multiInputIndex);
    }

    private static void DrawIndicator(ImDrawListPtr drawList, Color color, float opacity, Vector2 areaMin, Vector2 areaMax, ref int indicatorCount)
    {
        const int s = 4;
        var dx = (s + 1) * indicatorCount;

        var pMin = new Vector2(areaMax.X - 2 - s - dx,
                               (areaMax.Y - 2 - s).Clamp(areaMin.Y + 2, areaMax.Y));
        var pMax = new Vector2(areaMax.X - 2 - dx, areaMax.Y - 2);
        drawList.AddRectFilled(pMin, pMax, color.Fade(opacity));
        drawList.AddRect(pMin - Vector2.One,
                         pMax + Vector2.One,
                         UiColors.WindowBackground.Fade(0.4f * opacity));
        indicatorCount++;
    }

    // todo - move outta here
    private static SymbolUi.Child.CustomUiResult DrawCustomUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect, Vector2 canvasScale)
    {
        var type = instance.Type;
        var result = CustomChildUiRegistry.TryGetValue(type, out var drawFunction)
                         ? drawFunction(instance, drawList, selectableScreenRect, canvasScale)
                         : SymbolUi.Child.CustomUiResult.None;

        return result;
    }

    private bool TryDrawTexturePreview(MagGraphItem item, Vector2 itemMin, Vector2 itemMax, ImDrawListPtr drawList, Color typeColor)
    {
        if (item.Variant != MagGraphItem.Variants.Operator)
            return false;

        var instance = item.Instance;
        if (instance == null || instance.Outputs.Count == 0)
            return false;

        var firstOutput = instance.Outputs[0];
        if (firstOutput is not Slot<Texture2D> textureSlot)
            return false;

        var texture = textureSlot.Value;
        if (texture == null || texture.IsDisposed)
            return false;

        var previewTextureView = SrvManager.GetSrvForTexture(texture);

        var aspect = (float)texture.Description.Width / texture.Description.Height;

        var usableScreenRect = new ImRect(itemMin, itemMax);
        var opWidth = usableScreenRect.GetWidth();
        var unitScreenHeight = MagGraphItem.GridSize.Y * CanvasScale - 6 * CanvasScale;

        var previewSize = new Vector2(unitScreenHeight * aspect, unitScreenHeight);

        var maxAspect = 1.6f;
        if (previewSize.X > unitScreenHeight * maxAspect)
        {
            previewSize *= unitScreenHeight / (previewSize.X) * maxAspect;
        }

        var min = new Vector2(itemMax.X - previewSize.X - 2 * CanvasScale,
                              itemMin.Y
                              + (unitScreenHeight - previewSize.Y) / 2
                              + 3 * CanvasScale);

        //var min = new Vector2(itemMin.X, itemMin.Y - previewSize.Y - 1);
        //var max = new Vector2(itemMin.X + previewSize.X, itemMin.Y - 1);

        if (previewTextureView == null)
            return false;

        drawList.AddImage((IntPtr)previewTextureView, min,
                          min + previewSize,
                          Vector2.Zero,
                          Vector2.One,
                          Color.White);
        if (CanvasScale > 0.5f)
        {
            drawList.AddRect(min - Vector2.One,
                             min + previewSize + Vector2.One,
                             ColorVariations.OperatorBackground.Apply(typeColor),
                             2 * CanvasScale,
                             ImDrawFlags.RoundCornersAll,
                             1 * CanvasScale);
        }

        return true;
    }
}