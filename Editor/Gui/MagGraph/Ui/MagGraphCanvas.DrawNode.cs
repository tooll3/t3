using System.Diagnostics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi;
using T3.Editor.Gui.MagGraph.Interaction;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Editor.UiModel.InputsAndTypes;
using Texture2D = T3.Core.DataTypes.Texture2D;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    private void DrawNode(MagGraphItem item, ImDrawListPtr drawList, GraphUiContext context)
    {
        if (item.Variant == MagGraphItem.Variants.Placeholder)
            return;

        if (!IsRectVisible(item.Area))
            return;

        var idleFadeFactor = 1f;
        var idleFactor = 0f;
        if (item.Variant == MagGraphItem.Variants.Operator && item.Instance != null)
        {
            var framesSinceLastUpdate = 100;
            for (var index = 0; index < item.Instance.Outputs.Count; index++)
            {
                var output = item.Instance.Outputs[index];
                framesSinceLastUpdate = Math.Min(framesSinceLastUpdate, output.DirtyFlag.FramesSinceLastUpdate);
            }

            idleFadeFactor = MathUtils.RemapAndClamp(framesSinceLastUpdate, 0f, 60f, 1f, 0.6f);
            idleFactor = MathUtils.RemapAndClamp(framesSinceLastUpdate, 0f, 60f, 0f, 1f);
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
            const int snapPadding = 2;
            if ((snappedBorders & Borders.Down) == 0) pMaxVisible.Y -= (int)(snapPadding * 2 * CanvasScale);
            if ((snappedBorders & Borders.Right) == 0) pMaxVisible.X -= (int)(snapPadding * CanvasScale);
        }

        // Background and Outline
        var borders = (int)snappedBorders % 16;
        var imDrawFlags = _borderRoundings[borders];

        drawList.AddRectFilled(pMinVisible,
                               pMaxVisible,
                               Color.Mix(
                                         ColorVariations.OperatorBackground.Apply(typeColor),
                                         ColorVariations.OperatorBackgroundIdle.Apply(typeColor),
                                         idleFactor), CanvasScale < 0.5f ? 0 : 5 * CanvasScale,
                               imDrawFlags);

        // Snapped borders
        if ((snappedBorders & Borders.Down) != 0)
        {
            drawList.AddRectFilled(new Vector2(pMinVisible.X, pMaxVisible.Y),
                                   pMaxVisible - new Vector2(0, 2),
                                   ColorVariations.OperatorOutline.Apply(typeColor));
        }

        if ((snappedBorders & Borders.Right) != 0)
        {
            drawList.AddRectFilled(new Vector2(pMaxVisible.X - 2, pMinVisible.Y),
                                   pMaxVisible,
                                   ColorVariations.OperatorOutline.Apply(typeColor));
        }

        var isSelected = _context.Selector.IsSelected(item);
        var outlineColor = isSelected
                               ? UiColors.ForegroundFull
                               : UiColors.BackgroundFull.Fade(0f);

        drawList.AddRect(pMinVisible, pMaxVisible, outlineColor,
                         CanvasScale < 0.5 ? 0 : 6 * CanvasScale,
                         imDrawFlags);

        // Custom Ui
        SymbolUi.Child.CustomUiResult customUiResult = SymbolUi.Child.CustomUiResult.None;
        if (item.Variant == MagGraphItem.Variants.Operator)
        {
            customUiResult = DrawCustomUi(item.Instance, drawList, new ImRect(pMinVisible + Vector2.One, pMaxVisible - Vector2.One), Vector2.One * CanvasScale);
            if ((customUiResult & SymbolUi.Child.CustomUiResult.IsActive) != 0)
            {
                context.ItemWithActiveCustomUi = item;
            }
        }

        // ImGUI element for selection
        ImGui.SetCursorScreenPos(pMin);
        ImGui.PushID(item.Id.GetHashCode());
        ImGui.InvisibleButton(string.Empty, pMax - pMin);
        var isItemHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup
                                                | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

        if (_context.StateMachine.CurrentState == GraphStates.Default
            && isItemHovered
            && (customUiResult & SymbolUi.Child.CustomUiResult.IsActive) == 0)
            _context.ActiveItem = item;

        if ((customUiResult & SymbolUi.Child.CustomUiResult.IsActive) != 0)
        {
            //context.StateMachine.SetState(GraphStates.Default, context);
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

        if (customUiResult == SymbolUi.Child.CustomUiResult.None && CanvasScale > 0.2f)
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
                var t = pMaxVisible.Y - pMinVisible.Y;
                _inputIndicatorPoints[0] = pMinVisible;
                _inputIndicatorPoints[1] = pMinVisible + new Vector2(0.2f, 0) * t;
                _inputIndicatorPoints[2] = pMinVisible + new Vector2(0.5f, 0.5f) * t;
                _inputIndicatorPoints[3] = pMinVisible + new Vector2(0.2f, 1f) * t;
                _inputIndicatorPoints[4] = pMinVisible + new Vector2(0.0f, 1f) * t;
                drawList.AddConvexPolyFilled(ref _inputIndicatorPoints[0], 5, ColorVariations.Highlight.Apply(typeColor));
                name = "   " + name;
            }

            ImGui.PushFont(Fonts.FontBold);
            var labelSize = ImGui.CalcTextSize(name);
            ImGui.PopFont();

            var paddingForPreview = hasPreview ? MagGraphItem.LineHeight + 10 : 0;
            var downScale = MathF.Min(1f, (MagGraphItem.Width - paddingForPreview) * 0.9f / labelSize.X);

            var fontSize = Fonts.FontBold.FontSize * downScale * CanvasScale.Clamp(0.1f, 2f);
            var yCenter = pMin.Y + MagGraphItem.GridSize.Y / 2 * CanvasScale - fontSize / 2 - 2;

            //var labelPos = pMin + new Vector2(8, 8) * CanvasScale + new Vector2(0, -1);
            var labelPos = new Vector2(pMin.X + 8 * CanvasScale, yCenter);

            labelPos = new Vector2(MathF.Round(labelPos.X), MathF.Round(labelPos.Y));
            drawList.AddText(Fonts.FontBold,
                             fontSize,
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
            for (var inputIndex = 0; inputIndex < item.Instance.Inputs.Count; inputIndex++)
            {
                var inputSlot = item.Instance.Inputs[inputIndex];

                if (inputSlot.ValueType == _context.DraggedPrimaryOutputType
                    && !inputSlot.HasInputConnections)
                {
                    hasMatchingTypes = true;
                    break;
                }
            }

            if (hasMatchingTypes && item != _context.ActiveItem)
            {
                var indicatorPos = new Vector2(pMinVisible.X + 5 * CanvasScale, pMaxVisible.Y - 5 * CanvasScale);
                if (!isItemHovered)
                {
                    drawList.AddCircle(indicatorPos, 3, UiColors.ForegroundFull.Fade(Blink));
                }
                else
                {
                    if (_hoveredForInputPickingId != item.Id)
                    {
                        _hoverPickingProgress = 0;
                        _hoveredForInputPickingId = item.Id;
                    }

                    // Small animation from indicator to cursor pos
                    {
                        var animDuration = 0.2f;
                        _hoverPickingProgress = (_hoverPickingProgress + 1 / (ImGui.GetIO().Framerate * animDuration)).Clamp(0, 1);
                        var smoothedProgress = MathF.Pow(_hoverPickingProgress, 4);
                        var centerLerp = MathUtils.SmootherStep(0, 1, _hoverPickingProgress);
                        var center = Vector2.Lerp(indicatorPos, ImGui.GetMousePos(), centerLerp);
                        var radius = 3 + smoothedProgress * 3;
                        var circleColor = UiColors.ForegroundFull.Fade(1 - _hoverPickingProgress * 0.5f);
                        CircleInBoxHelper.DrawClippedCircle(pMinVisible, pMaxVisible, center, radius, circleColor, drawList);
                    }

                    // Draw selection outline...
                    drawList.AddRect(pMinVisible, pMaxVisible, UiColors.ForegroundFull.Fade(_hoverPickingProgress * 0.4f), 6 * CanvasScale, imDrawFlags);

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8, 8));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 3);
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 4));
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
                    ImGui.PopStyleVar(3);
                }
            }
        }

        // Missing primary input indicator
        if (item.InputLines.Length > 0)
        {
            var inputLine = item.InputLines[0];
            var isMissing = inputLine.InputUi?.Relevancy == Relevancy.Required && inputLine.ConnectionIn == null;
            if (isMissing)
            {
                DrawMissingInputIndicator(drawList, pMin, inputLine);
            }
        }

        if (CanvasScale > 0.25f)
        {
            // Input labels...
            if ((customUiResult & SymbolUi.Child.CustomUiResult.PreventInputLabels) == 0)
            {
                int inputIndex;
                for (inputIndex = 1; inputIndex < item.InputLines.Length; inputIndex++)
                {
                    var inputLine = item.InputLines[inputIndex];
                    var isMissing = inputLine.InputUi.Relevancy == Relevancy.Required && inputLine.ConnectionIn == null;
                    if (isMissing)
                    {
                        DrawMissingInputIndicator(drawList, pMin + new Vector2(0, GridSizeOnScreen.Y * inputIndex), inputLine);
                        // drawList.AddCircleFilled(pMin
                        //                          + new Vector2(0, GridSizeOnScreen.Y * (inputIndex + 0.5f)),
                        //                          3,
                        //                          UiColors.StatusAttention);
                    }

                    var inputLabelFontSize = Fonts.FontSmall.FontSize * Fonts.FontSmall.Scale * smallFontScaleFactor;
                    var yCenter = pMin.Y + GridSizeOnScreen.Y * (inputIndex + 0.5f) - inputLabelFontSize / 2 - 2;
                    var labelPos = new Vector2(pMin.X + 8 * CanvasScale, yCenter);
                    //var labelPos = pMin + new Vector2(8, 9) * CanvasScale + new Vector2(0, GridSizeOnScreen.Y * inputIndex);
                    var label = inputLine.InputUi.InputDefinition.Name ?? "?";
                    if (inputLine.MultiInputIndex > 0)
                    {
                        label += " +" + inputLine.MultiInputIndex;
                    }

                    drawList.AddText(Fonts.FontSmall,
                                     inputLabelFontSize,
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
                                                 inputLabelFontSize,
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
        }

        if (item.Variant == MagGraphItem.Variants.Operator)
        {
            // Animation indicator
            var indicatorCount = 0;
            if (item.Instance.Parent.Symbol.Animator.IsInstanceAnimated(item.Instance))
            {
                DrawIndicator(drawList, UiColors.StatusAnimated, idleFadeFactor, pMin, pMax, CanvasScale, ref indicatorCount);
            }

            // Pinned indicator
            if (context.Selector.PinnedIds.Contains(item.Instance.SymbolChildId))
            {
                DrawIndicator(drawList, UiColors.Selection, idleFadeFactor, pMin, pMax, CanvasScale, ref indicatorCount);
            }

            // Snapshot indicator
            {
                if (item.ChildUi.EnabledForSnapshots)
                {
                    DrawIndicator(drawList, UiColors.StatusAutomated, idleFadeFactor, pMin, pMax, CanvasScale, ref indicatorCount);
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

        if (CanvasScale < 0.5f)
            return;

        // Draw free input sockets...
        MagGraphItem.InputAnchorPoint inputAnchor = default;

        var anchorWidth = 1.5f * 2;
        var anchorHeight = 2f * 2;

        var inputAnchorCount = item.GetInputAnchorCount();
        for (var inputIndex = 0; inputIndex < inputAnchorCount; inputIndex++)
        {
            item.GetInputAnchorAtIndex(inputIndex, ref inputAnchor);
            var isMultiInput = inputAnchor.InputLine.InputUi != null && inputAnchor.InputLine.InputUi.InputDefinition.IsMultiInput;
            var isAlreadyUsed = inputAnchor.SnappedConnectionHash != MagGraphItem.FreeAnchor;
            // if (!isMultiInput)
            //     continue;

            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(inputAnchor.ConnectionType);
            var p = TransformPosition(inputAnchor.PositionOnCanvas);
            var color = ColorVariations.OperatorOutline.Apply(type2UiProperties.Color);

            if (inputAnchor.Direction == MagGraphItem.Directions.Vertical)
            {
                var pp = new Vector2(p.X, pMinVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(-anchorWidth, 0) * CanvasScale,
                                           pp + new Vector2(anchorWidth, 0) * CanvasScale,
                                           pp + new Vector2(0, anchorHeight) * CanvasScale,
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
                    if (!isAlreadyUsed && isPotentialConnectionStartDropTarget && item != _context.ActiveItem)
                    {
                        color = ColorVariations.Highlight.Apply(type2UiProperties.Color).Fade(Blink);
                        InputSnapper.RegisterAsPotentialTargetInput(item, p, inputAnchor.SlotId);
                    }
                }

                if (!isAlreadyUsed && showTriangleAnchor)
                {
                    var pp = new Vector2(pMinVisible.X - 1, p.Y);
                    drawList.AddTriangleFilled(pp + new Vector2(1, 0) + new Vector2(-0, -anchorWidth) * CanvasScale,
                                               pp + new Vector2(1, 0) + new Vector2(anchorHeight, 0) * CanvasScale,
                                               pp + new Vector2(1, 0) + new Vector2(0, anchorWidth) * CanvasScale,
                                               color);
                }
            }
        }

        var hoverFactor = hoverProgress.RemapAndClamp(0, 1, 1, 1.5f);

        // Draw output sockets
        var count = item.GetOutputAnchorCount();
        MagGraphItem.OutputAnchorPoint outputAnchor = default;
        for (var index = 0; index < count; index++)
        {
            item.GetOutputAnchorAtIndex(index, ref outputAnchor);

            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(outputAnchor.ConnectionType);

            var posOnCanvas = TransformPosition(outputAnchor.PositionOnCanvas);
            var color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(0.7f);

            Vector2 pp;
            if (outputAnchor.Direction == MagGraphItem.Directions.Vertical)
            {
                pp = new Vector2(posOnCanvas.X, pMaxVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(-anchorWidth, 0) * CanvasScale * hoverFactor,
                                           pp + new Vector2(anchorWidth, 0) * CanvasScale * hoverFactor,
                                           pp + new Vector2(0, anchorHeight) * CanvasScale * hoverFactor,
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

                pp = new Vector2(pMaxVisible.X, posOnCanvas.Y);

                drawList.AddTriangleFilled(pp + new Vector2(0, 0) + new Vector2(0, -anchorWidth) * CanvasScale * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(anchorHeight, 0) * CanvasScale * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(0, anchorWidth) * CanvasScale * hoverFactor,
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
                    var contextActiveSourceOutputId = outputAnchor.SlotId;
                    _context.ActiveSourceOutputId = contextActiveSourceOutputId;
                    _context.ActiveOutputDirection = outputAnchor.Direction;

                    // Show tooltip with output name and type
                    if (item.Variant == MagGraphItem.Variants.Operator)
                    {
                        var outputLine = item.OutputLines.FirstOrDefault(o => o.Id == contextActiveSourceOutputId);

                        if (outputLine.Id == contextActiveSourceOutputId)
                        {
                            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
                            ImGui.BeginTooltip();
                            ImGui.TextUnformatted(outputLine.OutputUi.OutputDefinition.Name);
                            var type = outputLine.OutputUi.OutputDefinition.ValueType;
                            var typeName = type.Name;
                            if (typeName == "Single")
                                typeName = "Float";

                            var uiProperties = TypeUiRegistry.GetPropertiesForType(type);
                            //var valueName = TypeUiRegistry.
                            ImGui.PushStyleColor(ImGuiCol.Text, uiProperties.Color.Rgba);
                            ImGui.TextUnformatted(typeName);
                            ImGui.PopStyleColor();
                            ImGui.EndTooltip();
                            ImGui.PopStyleVar();
                        }
                    }
                }
                else
                {
                    drawList.AddCircle(circleCenter, 3 * hoverFactor * mouseDistanceFactor, color2);
                }
            }

            //ShowAnchorPointDebugs(outputAnchor);
        }

        // Draw additional output indicator
        if (item.Variant == MagGraphItem.Variants.Operator)
        {
            if (item.HasHiddenOutputs)
            {
                if (_context.StateMachine.CurrentState == GraphStates.PickOutput && _context.ActiveItem == item)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5);
                    ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.One * 4);
                    ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0));
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

                    ImGui.PushStyleColor(ImGuiCol.ChildBg, UiColors.BackgroundFull.Fade(0.7f).Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Button, UiColors.BackgroundFull.Fade(0.0f).Rgba);
                    
                    if (ImGui.BeginPopup("pickOutput", ImGuiWindowFlags.Popup))
                    {
                        CustomComponents.HintLabel("Pick output...");
                        WindowContentExtend.ExtendToLastItem();

                        foreach (var o in context.ActiveItem.SymbolUi!.OutputUis.Values)
                        {
                            var isVisible = false;
                            foreach (var visibleOutput in context.ActiveItem.OutputLines)
                            {
                                if (visibleOutput.Id != o.Id) continue;
                                isVisible = true;
                                break;
                            }

                            if (isVisible)
                                continue;

                            var name = o.OutputDefinition.Name;
                            var labelSize = ImGui.CalcTextSize(name);
                            var width = MathF.Max(200 - 4, labelSize.X + 30);
                            var buttonSize = new Vector2(width, ImGui.GetFrameHeight());

                            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0,0.5f));
                            ImGui.Button(name);
                            ImGui.PopStyleVar();

                            if (ImGui.IsItemHovered()
                                && (ImGui.IsItemClicked(ImGuiMouseButton.Left)
                                    || ImGui.IsMouseReleased(ImGuiMouseButton.Left)
                                   )
                               )
                            {
                                var outputSlot = context.ActiveItem?.Instance?.Outputs.FirstOrDefault(os => os.Id == o.Id);
                                if (outputSlot == null)
                                    break;

                                var tempConnection = new MagGraphConnection
                                                         {
                                                             Style = MagGraphConnection.ConnectionStyles.Unknown,
                                                             SourcePos = context.ActiveItem.PosOnCanvas,
                                                             TargetPos = default,
                                                             SourceItem = context.ActiveItem,
                                                             TargetItem = null,
                                                             SourceOutput = outputSlot,
                                                             OutputLineIndex = 0,
                                                             VisibleOutputIndex = 0,
                                                             ConnectionHash = 0,
                                                             IsTemporary = true,
                                                         };
                                context.TempConnections.Add(tempConnection);
                                context.ActiveSourceItem = context.ActiveItem;
                                context.ActiveSourceOutputId = outputSlot.Id;
                                context.DraggedPrimaryOutputType = outputSlot.ValueType;
                                context.StateMachine.SetState(GraphStates.DragConnectionEnd, context);
                                ImGui.CloseCurrentPopup();
                            }

                            WindowContentExtend.ExtendToLastItem();

                        }
                        ImGui.EndPopup();
                    }
                    else
                    {
                        // Reset on release?
                        context.StateMachine.SetState(GraphStates.Default, context);
                    }
                    ImGui.PopStyleVar(5);
                    ImGui.PopStyleColor(2);
                }
                else
                {
                    var opacity = hoverProgress.RemapAndClamp(0, 1, 0.1f, 0.7f);
                    var padding = 0.1f;
                    var p = pMaxVisible - new Vector2(GridSizeOnScreen.Y * padding, GridSizeOnScreen.Y * padding);
                    drawList.AddTriangleFilled(
                                               p + new Vector2(-0.4f, -0.5f) * CanvasScale * 4,
                                               p + new Vector2(0.4f, 0) * CanvasScale * 4,
                                               p + new Vector2(-0.4f, 0.5f) * CanvasScale * 4,
                                               UiColors.ForegroundFull.Fade(opacity)
                                              );

                    if (hoverProgress > 0.5f)
                    {
                        var area = new ImRect(p + new Vector2(-0.4f, -0.5f) * CanvasScale * 6,
                                              p + new Vector2(0.4f, 0.5f) * CanvasScale * 6);
                        var isToggleHovered = area.Contains(ImGui.GetMousePos());
                        if (isToggleHovered)
                        {
                            ImGui.SetNextWindowPos(p);
                            CustomComponents
                               .TooltipForLastItem(() =>
                                                   {
                                                       CustomComponents.HintLabel("Show more outputs...");
                                                       foreach (var o in item.SymbolUi!.OutputUis.Values)
                                                       {
                                                           var isVisible = false;
                                                           foreach (var visibleOutput in item.OutputLines)
                                                           {
                                                               if (visibleOutput.Id != o.Id) continue;
                                                               isVisible = true;
                                                               break;
                                                           }

                                                           if (isVisible)
                                                               continue;

                                                           ImGui.TextUnformatted(o.OutputDefinition.Name);
                                                           ImGui.SameLine();
                                                           ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                                                           ImGui.TextUnformatted($" <{o.OutputDefinition.ValueType.Name}>");
                                                           ImGui.PopStyleColor();
                                                       }
                                                   }, false);

                            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
                                || ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                            {
                                ImGui.OpenPopup("pickOutput");
                                _context.StateMachine.SetState(GraphStates.PickOutput, context);
                                _context.ActiveItem = item;
                            }
                        }
                    }
                }
            }
        }

        if (item.Variant == MagGraphItem.Variants.Operator)
        {
            if (item.Instance is IStatusProvider statusProvider)
            {
                var statusLevel = statusProvider.GetStatusLevel();
                if (statusLevel != IStatusProvider.StatusLevel.Success && statusLevel != IStatusProvider.StatusLevel.Undefined)
                {
                    ImGui.SetCursorScreenPos(pMinVisible + new Vector2(8, -7) * T3Ui.UiScaleFactor);
                    ImGui.InvisibleButton("#warning", new Vector2(15, 15));
                    var color = statusLevel switch
                                    {
                                        IStatusProvider.StatusLevel.Notice  => UiColors.StatusAttention,
                                        IStatusProvider.StatusLevel.Warning => UiColors.StatusWarning,
                                        IStatusProvider.StatusLevel.Error   => UiColors.StatusError,
                                        _                                   => UiColors.StatusError
                                    };
                    Icons.DrawIconOnLastItem(Icon.Warning, color);
                    CustomComponents.TooltipForLastItem(UiColors.StatusWarning, statusLevel.ToString(), statusProvider.GetStatusMessage(), false);
                }
            }
        }
    }

    private void DrawMissingInputIndicator(ImDrawListPtr drawList, Vector2 pMin, MagGraphItem.InputLine inputLine)
    {
        var s = GridSizeOnScreen.Y;
        var c = pMin + new Vector2(-s * 0.2f, s * 0.45f);
        var s2 = s * 0.4f;
        drawList.AddTriangleFilled(c + new Vector2(0, -0.2f) * s2,
                                   c + new Vector2(-0.2f, 0.2f) * s2,
                                   c + new Vector2(0.2f, 0.2f) * s2,
                                   //+ new Vector2(8, 9) * CanvasScale 
                                   UiColors.StatusAttention);

        ImGui.SetCursorScreenPos(c - Vector2.One * s2 / 2);
        ImGui.InvisibleButton("warningArea", new Vector2(s2, s2));
        if (ImGui.IsItemHovered())
        {
            CustomComponents.TooltipForLastItem("Requires " + inputLine.InputUi.InputDefinition.Name);
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

    private static void DrawIndicator(ImDrawListPtr drawList, Color color, float opacity, Vector2 areaMin, Vector2 areaMax, float canvasScale,
                                      ref int indicatorCount)
    {
        const int s = 4;
        var dx = (s + 1) * indicatorCount;

        var pMin = new Vector2(s + dx,
                               s) * canvasScale + areaMin;

        var pMax = pMin + new Vector2(4, 2) * canvasScale;

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

        if (CustomChildUiRegistry.TryGetValue(type, out var drawFunction))
        {
            // Unfortunately we have to test if symbolChild of instance is still valid.
            // This might not be the case for operators like undo/redo.
            if (instance.Parent != null && instance.Parent.Children.TryGetValue(instance.SymbolChildId, out _))
                return drawFunction(instance, drawList, selectableScreenRect, canvasScale);

            return SymbolUi.Child.CustomUiResult.None;
        }

        return SymbolUi.Child.CustomUiResult.None;
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

        var unitScreenHeight = (MagGraphItem.GridSize.Y - 5) * CanvasScale;
        var previewSize = new Vector2(unitScreenHeight * aspect, unitScreenHeight);

        var maxAspect = 1.6f;
        if (previewSize.X > unitScreenHeight * maxAspect)
        {
            previewSize *= unitScreenHeight / (previewSize.X) * maxAspect;
        }

        var min = new Vector2(itemMax.X - previewSize.X - 2 * CanvasScale,
                              itemMin.Y
                              + (unitScreenHeight - previewSize.Y) / 2
                              + 1 * CanvasScale);

        if (previewTextureView == null)
            return false;

        drawList.AddImage((IntPtr)previewTextureView, min,
                          min + previewSize,
                          Vector2.Zero,
                          Vector2.One,
                          Color.White);
        if (CanvasScale > 0.5f)
        {
            drawList.AddRect(min - Vector2.One * 0.5f * CanvasScale,
                             min + previewSize + Vector2.One * 0.5f * CanvasScale,
                             ColorVariations.ConnectionLines.Apply(typeColor),
                             2 * CanvasScale,
                             ImDrawFlags.RoundCornersAll,
                             1 * CanvasScale);
        }

        return true;
    }

    private static Guid _hoveredForInputPickingId;
    private static float _hoverPickingProgress;

    private static readonly Vector2[] _inputIndicatorPoints = new Vector2[5];
}

internal static class CircleInBoxHelper
{
    public static void DrawClippedCircle(Vector2 pMin, Vector2 pMax, Vector2 center, float radius, Color color, ImDrawListPtr drawList)
    {
        if (!new ImRect(pMin, pMax).Contains(center))
            return;

        // Bottom right quadrant (0 to π/2)
        {
            var dx = pMax.X - center.X;
            var start = dx < 0 ? MathF.PI / 2
                        : dx < radius ? MathF.Acos(dx / radius) : 0;

            var dy = pMax.Y - center.Y;
            var end = dy < 0 ? 0
                      : dy < radius ? MathF.PI / 2 - MathF.Acos(dy / radius) : MathF.PI / 2;

            if (start < end)
            {
                drawList.PathArcTo(center, radius, start, end);
            }
            else
            {
                drawList.PathLineTo(new Vector2(pMax.X, center.Y));
                drawList.PathLineTo(pMax);
            }
        }

        // Bottom left quadrant (π/2 to π)
        {
            var dy = pMax.Y - center.Y;
            var start = dy < 0
                            ? MathF.PI / 2
                            : dy < radius
                                ? MathF.PI / 2 + MathF.Acos(dy / radius)
                                : MathF.PI / 1.95f;

            var dx = center.X - pMin.X;
            var end = dx < 0
                          ? MathF.PI
                          : dx < radius
                              ? MathF.PI - MathF.Acos(dx / radius)
                              : MathF.PI * 0.95f;

            if (start < end)
            {
                drawList.PathArcTo(center, radius, start, end);
            }
            else
            {
                drawList.PathLineTo(new Vector2(pMin.X, pMax.Y));
            }
        }

        // Top left quadrant (π to 3π/2)
        {
            var dx = center.X - pMin.X;
            var start = dx < 0 ? MathF.PI
                        : dx < radius ? MathF.PI + MathF.Acos(dx / radius) : MathF.PI;

            var dy = center.Y - pMin.Y;
            var end = dy < 0
                          ? MathF.PI * 1.1f
                          : dy < radius
                              ? 3 * MathF.PI / 2 - MathF.Acos(dy / radius)
                              : 2.95f * MathF.PI / 2;

            if (start < end)
            {
                drawList.PathArcTo(center, radius, start, end);
            }
            else
            {
                drawList.PathLineTo(pMin);
            }
        }

        // Top right quadrant (3π/2 to 2π)
        {
            var dy = center.Y - pMin.Y;
            var start = dy < 0
                            ? 3 * MathF.PI / 2
                            : dy < radius
                                ? 3 * MathF.PI / 2 + MathF.Acos(dy / radius)
                                : 3 * MathF.PI / 2;

            var dx = pMax.X - center.X;
            var end = dx < 0
                          ? 2 * MathF.PI
                          : dx < radius
                              ? 2 * MathF.PI - MathF.Acos(dx / radius)
                              : 2 * MathF.PI;

            if (start < end)
            {
                drawList.PathArcTo(center, radius, start, end);
            }
            else
            {
                drawList.PathLineTo(new Vector2(pMax.X, pMin.Y));
                drawList.PathLineTo(new Vector2(pMax.X, center.Y));
            }
        }

        //drawList.PathStroke(color, ImDrawFlags.Closed , 2);
        drawList.PathFillConvex(color);
    }
}