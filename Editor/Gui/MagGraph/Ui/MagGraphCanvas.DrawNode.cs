using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Rendering;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.MagGraph.Model;
using T3.Editor.Gui.MagGraph.States;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using Texture2D = T3.Core.DataTypes.Texture2D;

namespace T3.Editor.Gui.MagGraph.Ui;

internal sealed partial class MagGraphCanvas
{
    private void DrawItem(MagGraphItem item, ImDrawListPtr drawList, GraphUiContext context)
    {
        if (item.Variant == MagGraphItem.Variants.Placeholder)
            return;

        var typeUiProperties = TypeUiRegistry.GetPropertiesForType(item.PrimaryType);

        var typeColor = typeUiProperties.Color;
        var labelColor = ColorVariations.OperatorLabel.Apply(typeColor);

        var pMin = TransformPosition(item.PosOnCanvas);
        var pMax = TransformPosition(item.PosOnCanvas + item.Size);
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

        // ImGUI element for selection
        ImGui.SetCursorScreenPos(pMin);
        ImGui.PushID(item.Id.GetHashCode());
        ImGui.InvisibleButton(string.Empty, pMax - pMin);
        var isItemHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup
                                                | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

        if (_context.StateMachine.CurrentState is DefaultState && isItemHovered)
            _context.ActiveItem = item;

        // Todo: We eventually need to handle right clicking to select and open context menu when dragging with right mouse button. 
        // var wasDraggingRight = ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Length() > UserSettings.Config.ClickThreshold;
        // if (ImGui.IsMouseReleased(ImGuiMouseButton.Right)
        //     && !wasDraggingRight
        //     && ImGui.IsItemHovered()
        //     && !_nodeSelection.IsNodeSelected(item))
        // {
        //     item.Select(_nodeSelection);
        // }
        ImGui.PopID();

        // Background and Outline
        var imDrawFlags = _borderRoundings[(int)snappedBorders % 16];

        var isHovered = isItemHovered || _context.Selector.HoveredIds.Contains(item.Id);
        var fade = isHovered ? 1 : 0.7f;
        drawList.AddRectFilled(pMinVisible + Vector2.One * CanvasScale, 
                               pMaxVisible,
                               ColorVariations.OperatorBackground.Apply(typeColor).Fade(fade), 5 * CanvasScale,
                               imDrawFlags);

        var isSelected = _context.Selector.IsSelected(item);
        var outlineColor = isSelected
                               ? UiColors.ForegroundFull
                               : UiColors.BackgroundFull.Fade(0f);
        
        drawList.AddRect(pMinVisible, pMaxVisible, outlineColor, 6 * CanvasScale, imDrawFlags);

        
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
                         Fonts.FontBold.FontSize * downScale * CanvasScale.Clamp(0.1f,2f),
                         labelPos,
                         labelColor,
                         name);

        // Indicate hidden matching inputs...
        if (_context.DraggedPrimaryOutputType != null
            && item.Variant == MagGraphItem.Variants.Operator
            && !context.ItemMovement.IsItemDragged(item))
        {
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
                if (_context.PrimaryOutputItem != null)
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
                }
            }
        }

        // Primary input indicator
        if(item.InputLines.Length > 0 && item.InputLines[0].InputUi != null)
        {
            var inputLine = item.InputLines[0];
            var isMissing = inputLine.InputUi.Relevancy == Relevancy.Required && inputLine.ConnectionIn == null;
            if (isMissing)
            {
                drawList.AddCircleFilled(pMin 
                                         //+ new Vector2(8, 9) * CanvasScale 
                                         + new Vector2(0, GridSizeOnScreen.Y * ( 0.5f)),
                                         3,
                                         UiColors.StatusAttention);
            }
        }
        
        // Input labels...
        int inputIndex;
        for (inputIndex = 1; inputIndex < item.InputLines.Length; inputIndex++)
        {
            var inputLine = item.InputLines[inputIndex];
            var isMissing = inputLine.InputUi.Relevancy == Relevancy.Required && inputLine.ConnectionIn == null;
            if (isMissing)
            {
                drawList.AddCircleFilled(pMin 
                                         //+ new Vector2(8, 9) * CanvasScale 
                                        + new Vector2(0, GridSizeOnScreen.Y * (inputIndex + 0.5f)),
                                   3,
                                   UiColors.StatusAttention);
            }
            
            drawList.AddText(Fonts.FontSmall, 
                             Fonts.FontSmall.FontSize * CanvasScale.Clamp(0.1f,2f),
                             pMin + new Vector2(8, 9) * CanvasScale + new Vector2(0, GridSizeOnScreen.Y * inputIndex),
                             labelColor.Fade(0.7f),
                             inputLine.InputUi.InputDefinition.Name ?? "?"
                            );
        }

        // Draw output labels...
        for (var outputIndex = 1; outputIndex < item.OutputLines.Length; outputIndex++)
        {
            var outputLine = item.OutputLines[outputIndex];
            if (outputLine.OutputUi == null)
                continue;

            ImGui.PushFont(Fonts.FontSmall);
            var outputDefinitionName = outputLine.OutputUi.OutputDefinition.Name;
            var outputLabelSize = ImGui.CalcTextSize(outputDefinitionName);
            ImGui.PopFont();

            drawList.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize * CanvasScale,
                             pMin
                             + new Vector2(-8, 9) * CanvasScale.Clamp(0.1f,2f)
                             + new Vector2(0, GridSizeOnScreen.Y * (outputIndex + inputIndex - 1))
                             + new Vector2(MagGraphItem.Width * CanvasScale - outputLabelSize.X * CanvasScale, 0),
                             labelColor.Fade(0.7f),
                             outputDefinitionName);
        }

        // Indicator primary output op peek position...
        if (_context.PrimaryOutputItem != null && item.Id == _context.PrimaryOutputItem.Id)
        {
            drawList.AddCircleFilled(TransformPosition(new Vector2(item.Area.Max.X - MagGraphItem.GridSize.Y * 0.25f,
                                                                   item.Area.Min.Y + MagGraphItem.GridSize.Y * 0.5f)),
                                     3 * CanvasScale,
                                     UiColors.ForegroundFull);
        }
        
        // Draw input sockets
        foreach (var inputAnchor in item.GetInputAnchors())
        {
            var isAlreadyUsed = inputAnchor.ConnectionHash != 0;
            if (isAlreadyUsed)
            {
                continue;
            }
            
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
                var pp = new Vector2(pMinVisible.X - 1, p.Y);
                drawList.AddTriangleFilled(pp + new Vector2(1, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f,
                                           pp + new Vector2(1, 0) + new Vector2(2, 0) * CanvasScale * 1.5f,
                                           pp + new Vector2(1, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f,
                                           color);
            }

            ShowAnchorPointDebugs(inputAnchor, true);
        }

        var hoverFactor = isItemHovered ? 2 : 1;

        // Draw output sockets
        foreach (var oa in item.GetOutputAnchors())
        {
            var type2UiProperties = TypeUiRegistry.GetPropertiesForType(oa.ConnectionType);

            var p = TransformPosition(oa.PositionOnCanvas);
            var color = ColorVariations.OperatorBackground.Apply(type2UiProperties.Color).Fade(0.7f);

            if (oa.Direction == MagGraphItem.Directions.Vertical)
            {
                var pp = new Vector2(p.X, pMaxVisible.Y);
                drawList.AddTriangleFilled(pp + new Vector2(0, -1) + new Vector2(-1.5f, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, -1) + new Vector2(1.5f, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, -1) + new Vector2(0, 2) * CanvasScale * 1.5f * hoverFactor,
                                           color);
            }
            else
            {
                var pp = new Vector2(pMaxVisible.X - 1, p.Y);

                drawList.AddTriangleFilled(pp + new Vector2(0, 0) + new Vector2(-0, -1.5f) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(2, 0) * CanvasScale * 1.5f * hoverFactor,
                                           pp + new Vector2(0, 0) + new Vector2(0, 1.5f) * CanvasScale * 1.5f * hoverFactor,
                                           color);

                if (isItemHovered)
                {
                    var color2 = ColorVariations.OperatorLabel.Apply(type2UiProperties.Color).Fade(0.7f);
                    var circleCenter = pp + new Vector2(-3, 0);
                    var mouseDistance = Vector2.Distance(ImGui.GetMousePos(), circleCenter);

                    var mouseDistanceFactor = mouseDistance.RemapAndClamp(30, 10, 0.6f, 1.1f);
                    if (mouseDistance < 7)
                    {
                        drawList.AddCircleFilled(circleCenter, 3 * hoverFactor * 0.8f, color2);
                        _context.ActiveOutputId = oa.SlotId;
                    }
                    else
                    {
                        drawList.AddCircle(circleCenter, 3 * hoverFactor * mouseDistanceFactor, color2);
                    }
                }
            }

            ShowAnchorPointDebugs(oa);
        }
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

        var min = new Vector2(itemMax.X - previewSize.X - 3 * CanvasScale,
                              itemMin.Y
                              + (unitScreenHeight - previewSize.Y) / 2
                              + 2 * CanvasScale);

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
            drawList.AddRect(min- Vector2.One,
                              min + previewSize+ Vector2.One,
                              ColorVariations.OperatorBackground.Apply(typeColor),
                              2 * CanvasScale, 
                              ImDrawFlags.RoundCornersAll, 
                              1 * CanvasScale);
            
        }
        return true;
    }
}