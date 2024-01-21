using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using SharpDX.Direct3D11;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Graph.Rendering;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction.TransformGizmos;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;
using Color = T3.Core.DataTypes.Vector.Color;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// Renders a graphic representation of a <see cref="SymbolChild"/> within the current <see cref="GraphWindow"/>
    /// </summary>
    internal static class GraphNode
    {
        public static void Draw(SymbolChildUi childUi, Instance instance, bool preventInteraction= false)
        {
            if (instance == null)
                return;

            var symbolUi = SymbolUiRegistry.Entries[childUi.SymbolChild.Symbol.Id];
            var nodeHasHiddenMatchingInputs = false;
            var visibleInputUis = FindVisibleInputUis(symbolUi, childUi, ref nodeHasHiddenMatchingInputs);

            var framesSinceLastUpdate = 100;
            foreach (var output in instance.Outputs)
            {
                framesSinceLastUpdate = Math.Min(framesSinceLastUpdate, output.DirtyFlag.FramesSinceLastUpdate);
            }

            SymbolChildUi.CustomUiResult customUiResult  = SymbolChildUi.CustomUiResult.None;
            _drawList = global::T3.Editor.Gui.Graph.Graph.DrawList;

            var newNodeSize = ComputeNodeSize(childUi, visibleInputUis);
            AdjustGroupLayoutAfterResize(childUi, newNodeSize);
            _usableScreenRect = GraphCanvas.Current.TransformRect(new ImRect(childUi.PosOnCanvas,
                                                                             childUi.PosOnCanvas + childUi.Size));
            _selectableScreenRect = _usableScreenRect;
            if (UserSettings.Config.ShowThumbnails)
                PreparePreviewAndExpandSelectableArea(instance);

            _isVisible = ImGui.IsRectVisible(_selectableScreenRect.Min, _selectableScreenRect.Max);

            
            var isNodeHovered = false;
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                var drawList = GraphCanvas.Current.DrawList;

                if (_isVisible)
                {
                    if (instance is IStatusProvider statusProvider)
                    {
                        var statusLevel = statusProvider.GetStatusLevel();
                        if (statusLevel == IStatusProvider.StatusLevel.Warning || statusLevel ==IStatusProvider.StatusLevel.Error)
                        {
                            ImGui.SetCursorScreenPos(_usableScreenRect.Min - new Vector2(10, 12) * T3Ui.UiScaleFactor);
                            ImGui.InvisibleButton("#warning", new Vector2(15, 15));
                            Icons.DrawIconOnLastItem(Icon.Warning, UiColors.StatusWarning);
                            CustomComponents.TooltipForLastItem( UiColors.StatusWarning, statusLevel.ToString(), statusProvider.GetStatusMessage(), false);
                        }
                    }

                    if (!string.IsNullOrEmpty(childUi.Comment))
                    {
                        ImGui.SetCursorScreenPos(new Vector2(_usableScreenRect.Max.X,  _usableScreenRect.Min.Y) -  new Vector2(3, 12) * T3Ui.UiScaleFactor * T3Ui.UiScaleFactor);
                        if (ImGui.InvisibleButton("#comment", new Vector2(15, 15)))
                        {
                            NodeSelection.SetSelectionToChildUi(childUi,instance);
                            GraphCanvas.EditCommentDialog.ShowNextFrame();
                        }
                        Icons.DrawIconOnLastItem(Icon.Comment, UiColors.ForegroundFull);
                        CustomComponents.TooltipForLastItem( UiColors.Text, childUi.Comment, null, false);
                    }
                    
                    // Resize indicator
                    if (childUi.Style == SymbolChildUi.Styles.Resizable)
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                        ImGui.SetCursorScreenPos(_usableScreenRect.Max - new Vector2(10, 10) * T3Ui.UiScaleFactor);
                        ImGui.Button("##resize", new Vector2(10, 10) * T3Ui.UiScaleFactor);
                        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                        {
                            var delta = GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                            childUi.Size += delta;
                        }

                        ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                    }

                    // Rendering
                    //var childInstance = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(c => c.SymbolChildId == childUi.SymbolChild.Id);

                    var typeColor = (childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0
                                        ? TypeUiRegistry.GetPropertiesForType(childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType).Color
                                        : UiColors.Gray).Fade(Graph.GraphOpacity);

                    var backgroundColor = typeColor;

                    // Background
                    var isHighlighted = FrameStats.Last.HoveredIds.Contains(instance.SymbolChildId);
                    if (framesSinceLastUpdate > 2)
                    {
                        var fadeFactor = MathUtils.RemapAndClamp(framesSinceLastUpdate, 0f, 60f, 0f, 1.0f);
                        var mutedColor = ColorVariations.OperatorBackgroundIdle.Apply(backgroundColor).Fade(Graph.GraphOpacity);
                        backgroundColor = Color.Mix(backgroundColor, mutedColor, fadeFactor);
                    }

                    var backgroundColorWithHover = isHighlighted
                                                       ? ColorVariations.OperatorBackgroundHover.Apply(backgroundColor)
                                                       : ColorVariations.OperatorBackground.Apply(backgroundColor);

                    drawList.AddRectFilled(_usableScreenRect.Min, _usableScreenRect.Max,
                                           backgroundColorWithHover.Fade(Graph.GraphOpacity));

                    // Custom ui
                    customUiResult = SymbolChildUi.DrawCustomUi(instance, _drawList, _selectableScreenRect);

                    // Size toggle
                    if (customUiResult == SymbolChildUi.CustomUiResult.None && GraphCanvas.Current.Scale.X > 0.7f)
                    {
                        var pos = new Vector2(_usableScreenRect.Max.X - 15, _usableScreenRect.Min.Y + 2);

                        ImGui.SetCursorScreenPos(pos);
                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                        ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba * Graph.GraphOpacity);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Color(1, 1, 1, .3f * Graph.GraphOpacity).Rgba);
                        ImGui.PushStyleColor(ImGuiCol.Text, new Color(1, 1, 1, .3f * Graph.GraphOpacity).Rgba);
                        ImGui.PushFont(Icons.IconFont);

                        if (childUi.Style == SymbolChildUi.Styles.Default)
                        {
                            if (ImGui.Button(_unfoldLabel, new Vector2(16, 16)))
                            {
                                childUi.Style = SymbolChildUi.Styles.Expanded;
                            }
                        }
                        else if (childUi.Style != SymbolChildUi.Styles.Default)
                        {
                            if (ImGui.Button(_foldLabel, new Vector2(16, 16)))
                            {
                                childUi.Style = SymbolChildUi.Styles.Default;
                            }
                        }

                        ImGui.PopFont();
                        ImGui.PopStyleVar();
                        ImGui.PopStyleColor(3);
                    }

                    // Disabled indicator
                    if (childUi.IsDisabled)
                    {
                        DrawOverlayLine(drawList, Vector2.Zero, Vector2.One );
                        DrawOverlayLine(drawList, new Vector2(1,0), new Vector2(0,1) );
                    }
                    
                    // Bypass indicator
                    if (childUi.SymbolChild.IsBypassed)
                    {
                        DrawOverlayLine(drawList, new Vector2(0.05f,0.5f), new Vector2(0.4f,0.5f) );
                        DrawOverlayLine(drawList, new Vector2(0.6f,0.5f), new Vector2(0.95f,0.5f) );
                        
                        DrawOverlayLine(drawList, new Vector2(0.35f,0.1f), new Vector2(0.65f,0.9f) );
                        DrawOverlayLine(drawList, new Vector2(0.65f,0.1f), new Vector2(0.35f,0.9f) );
                    }

                    // Interaction
                    if (preventInteraction)
                    {
                        ImGui.SetCursorScreenPos( new Vector2(-5000,-5000));
                    }
                    else
                    {
                        ImGui.SetCursorScreenPos(_selectableScreenRect.Min);
                    }

                    //--------------------------------------------------------------------------
                    ImGui.InvisibleButton("node", _selectableScreenRect.GetSize());
                    //--------------------------------------------------------------------------

                    if(!preventInteraction)
                        SelectableNodeMovement.Handle(childUi, instance);

                    isNodeHovered = !preventInteraction 
                                    && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) 
                                    && !GraphCanvas.Current.SymbolBrowser.IsOpen
                                    && ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup);
                    
                    // Tooltip
                    if (isNodeHovered
                        && UserSettings.Config.EditorHoverPreview
                        && (customUiResult & SymbolChildUi.CustomUiResult.PreventTooltip) != SymbolChildUi.CustomUiResult.PreventTooltip
                        )
                    {
                        if (UserSettings.Config.SmartGroupDragging)
                            SelectableNodeMovement.HighlightSnappedNeighbours(childUi);

                        //ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        FrameStats.AddHoveredId(childUi.SymbolChild.Id);

                        if (UserSettings.Config.HoverMode != GraphCanvas.HoverModes.Disabled
                            && !ImGui.IsMouseDragging(ImGuiMouseButton.Left)
                            && !RenameInstanceOverlay.IsOpen)
                        {
                            ImGui.BeginTooltip();
                            {
                                ImGui.SetNextWindowSizeConstraints(new Vector2(200, 200*9/16f), new Vector2(200, 200*9/16f));
                                ImGui.BeginChild("##innerTooltip");
                                {
                                    TransformGizmoHandling.SetDrawList(drawList);
                                    _imageCanvasForTooltips.Update();
                                    _imageCanvasForTooltips.SetAsCurrent();
                                    if (instance.Outputs.Count > 0)
                                    {
                                        var firstOutput = instance.Outputs[0];
                                        IOutputUi outputUi = symbolUi.OutputUis[firstOutput.Id];
                                        _evaluationContext.Reset();
                                        _evaluationContext.RequestedResolution = new Int2(1280 / 2, 720 / 2);
                                        outputUi.DrawValue(firstOutput, _evaluationContext,
                                                           recompute: UserSettings.Config.HoverMode == GraphCanvas.HoverModes.Live);
                                        
                                    }
                                    _imageCanvasForTooltips.Deactivate();
                                    TransformGizmoHandling.RestoreDrawList();
                                }
                                ImGui.EndChild();

                            }
                            ImGui.EndTooltip();
                        }
                    }

                    // A work around to detect if node is below mouse while dragging end of new connection
                    if (_selectableScreenRect.Contains(ImGui.GetMousePos()))
                    {
                        _hoveredNodeIdForConnectionTarget = childUi.Id;
                    }

                    var hovered =  (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) ||
                                      FrameStats.Last.HoveredIds.Contains(instance.SymbolChildId));

                    // A horrible work around to prevent exception because CompositionOp changed during drawing.
                    // A better solution would defer setting the compositionOp to the beginning of next frame.
                    var justOpenedChild = false;
                    if (hovered && ImGui.IsMouseDoubleClicked(0)
                                && !RenameInstanceOverlay.IsOpen
                                && (customUiResult & SymbolChildUi.CustomUiResult.PreventOpenSubGraph) == 0)
                    {
                        if (ImGui.IsWindowFocused() || ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
                        {
                            var blocked = false;
                            if (UserSettings.Config.WarnBeforeLibEdit && instance.Symbol.Namespace.StartsWith("lib."))
                            {
                                if (UserSettings.Config.WarnBeforeLibEdit)
                                {
                                    var count = Structure.CollectDependingSymbols(instance.Symbol).Count();
                                    LibWarningDialog.DependencyCount = count;
                                    LibWarningDialog.HandledInstance = instance;
                                    GraphCanvas.LibWarningDialog.ShowNextFrame();
                                    blocked = true;
                                }
                            }

                            if (!blocked)
                            {
                                GraphCanvas.Current.SetCompositionToChildInstance(instance);
                                ImGui.CloseCurrentPopup();
                                justOpenedChild = true;
                            }
                        }
                    }

                    if (!justOpenedChild)
                    {
                        ParameterPopUp.HandleOpenParameterPopUp(childUi, instance, customUiResult, _selectableScreenRect);
                    }

                    DrawPreview();

                    // Outline shadow
                    drawList.AddRect(_selectableScreenRect.Min,
                                     _selectableScreenRect.Max + Vector2.One,
                                     ColorVariations.OperatorOutline.Apply(typeColor),
                                     rounding: 0,
                                     ImDrawFlags.None);

                    if (isHighlighted)
                    {
                        drawList.AddRect(_selectableScreenRect.Min - Vector2.One  * 2,
                                         _selectableScreenRect.Max + Vector2.One * 3 ,
                                         UiColors.ForegroundFull.Fade(0.6f),
                                         rounding: 0,
                                         ImDrawFlags.None);
                    }

                    // Animation indicator
                    var indicatorCount = 0;
                    var compositionOp = GraphCanvas.Current.CompositionOp;
                    var isInstanceAnimated = compositionOp.Symbol.Animator.IsInstanceAnimated(instance);
                    if (isInstanceAnimated)
                    {
                        DrawIndicator(UiColors.StatusAnimated, ref indicatorCount);
                    }

                    // Pinned indicator
                    if (FrameStats.Last.RenderedIds.Contains(instance.SymbolChildId))
                    {
                        DrawIndicator(UiColors.Selection, ref indicatorCount);
                    }

                    // Snapshot indicator
                    {
                        if (childUi.SnapshotGroupIndex > 0)
                        {
                            DrawIndicator(UiColors.StatusAutomated, ref indicatorCount);
                        }
                    }

                    // Hidden inputs indicator
                    if (nodeHasHiddenMatchingInputs)
                    {
                        var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.8f);
                        var colorForType = TypeUiRegistry.Entries[ConnectionMaker.TempConnections[0].ConnectionType].Color;
                        colorForType.Rgba.W *= blink;
                        _drawList.AddRectFilled(
                                                new Vector2(_usableScreenRect.Min.X, _usableScreenRect.Max.Y + 3),
                                                new Vector2(_usableScreenRect.Min.X + 10, _usableScreenRect.Max.Y + 5),
                                                colorForType);
                    }

                    // Label
                    if (customUiResult == SymbolChildUi.CustomUiResult.None
                        && _selectableScreenRect.GetHeight() > 8)
                    {
                        drawList.PushClipRect(_usableScreenRect.Min, _usableScreenRect.Max, true);
                        var useSmallFont = GraphCanvas.Current.Scale.X < 1 * T3Ui.UiScaleFactor;
                        var font = Fonts.FontBold;
                        
                        var isRenamed = !string.IsNullOrEmpty(childUi.SymbolChild.Name);
                        var fade = MathUtils.SmootherStep(0.2f, 0.6f, GraphCanvas.Current.Scale.X);
                        
                        drawList.AddText(font,
                                         font.FontSize * ( useSmallFont ?  GraphCanvas.Current.Scale.X : 1) ,
                                                        _usableScreenRect.Min + LabelPos,
                                                        ColorVariations.OperatorLabel.Apply(typeColor).Fade(fade),
                                                        isRenamed ? $"\"{childUi.SymbolChild.ReadableName}\"" : childUi.SymbolChild.ReadableName);
                        
                        drawList.PopClipRect();
                    }

                    if (childUi.IsSelected)
                    {
                        drawList.AddRect(_selectableScreenRect.Min - Vector2.One , _selectableScreenRect.Max + Vector2.One * 2, 
                                         UiColors.BackgroundFull.Fade(Graph.GraphOpacity));
                        drawList.AddRect(_selectableScreenRect.Min , _selectableScreenRect.Max + Vector2.One * 1, 
                                         UiColors.Selection.Fade(Graph.GraphOpacity));
                    }
                }
            }
            ImGui.PopID();
            
            var connectionBorderArea = _selectableScreenRect;
            connectionBorderArea.Min.X -= 4;

            // Input Sockets...
            for (var inputIndex = 0; inputIndex < visibleInputUis.Count; inputIndex++)
            {
                var inputUi = visibleInputUis[inputIndex];
                var inputDefinition = inputUi.InputDefinition;

                var usableSlotArea = GetUsableInputSlotSize(inputIndex, visibleInputUis.Count);

                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + inputDefinition.GetHashCode());
                ImGui.SetCursorScreenPos(usableSlotArea.Min);
                ImGui.InvisibleButton("input", usableSlotArea.GetSize());
                THelpers.DebugItemRect("input-slot");
                
                // Note: isItemHovered does not work when a connection is being dragged from another item
                var hovered = ConnectionMaker.TempConnections.Count > 0
                                  ? usableSlotArea.Contains(ImGui.GetMousePos())
                                  : ImGui.IsItemHovered();
                
                var isPotentialConnectionTarget = ConnectionMaker.IsMatchingInputType(inputDefinition.DefaultValue.ValueType);
                var colorForType = ColorForInputType(inputDefinition).Fade(Graph.GraphOpacity);
                
                var connectedLines = global::T3.Editor.Gui.Graph.Graph.Connections.GetLinesToNodeInputSlot(childUi, inputDefinition.Id);
                
                // Render input Label
                if ((customUiResult & SymbolChildUi.CustomUiResult.PreventInputLabels) == 0)
                {
                    var inputLabelOpacity = MathUtils.RemapAndClamp(GraphCanvas.Current.Scale.X,
                                                                    0.75f, 1.5f,
                                                                    0f, 1f) * Graph.GraphOpacity;
                
                    var screenCursor = usableSlotArea.GetCenter() + new Vector2(14, -7);
                    if (inputLabelOpacity > 0)
                    {
                        ImGui.PushFont(Fonts.FontSmall);
                        var labelColor = ColorVariations.OperatorLabel.Apply(colorForType);
                        labelColor.Rgba.W = inputLabelOpacity;
                        var label = inputDefinition.Name;
                        if (inputDefinition.IsMultiInput)
                        {
                            label = $"  {label} [...]";
                        }
                
                        var labelSize = ImGui.CalcTextSize(label);
                        _drawList.AddText(screenCursor, labelColor, label);
                
                        screenCursor += new Vector2(labelSize.X + 8, 0);
                
                        // Value
                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor.Rgba);
                        //var inputSlot = instance.Inputs.Single(slot => inputDefinition.Id == slot.Id);
                        var inputSlot = instance.GetInput(inputDefinition.Id);
                        //var valueAsString = inputUi.GetSlotValue(inputSlot);
                        var valueAsString = GetValueString(inputSlot);
                        // if (inputSlot is InputSlot<float> f)
                        // {
                        //     var xxx = f.TypedInputValue.Value;
                        // } 

                        var valueColor = labelColor;
                        valueColor.Rgba.W *= 0.6f;
                
                        // Avoid clipping because it increases ImGui draw calls and is expensive
                        var estimatedValueWidth = valueAsString.Length * 8;
                        var needClipping = estimatedValueWidth > _usableScreenRect.Max.X - screenCursor.X;
                        if (needClipping)
                        {
                            _drawList.PushClipRect(_usableScreenRect.Min, _usableScreenRect.Max, true);
                        }
                
                        if(!string.IsNullOrEmpty(valueAsString))
                            _drawList.AddText(screenCursor, valueColor, valueAsString);
                        
                        if (needClipping)
                            _drawList.PopClipRect();
                
                        ImGui.PopStyleColor();
                
                        ImGui.PopFont();
                    }
                }
                
                // Draw input slots
                if (inputDefinition.IsMultiInput)
                {
                    var showGaps = isPotentialConnectionTarget;
                
                    var socketCount = showGaps
                                          ? connectedLines.Count * 2 + 1
                                          : connectedLines.Count;
                
                    var socketHeight = (usableSlotArea.GetHeight() + 1) / socketCount;
                    var targetPos = new Vector2(usableSlotArea.Max.X - 4,
                                                usableSlotArea.Min.Y + socketHeight * 0.5f);
                
                    var topLeft = new Vector2(usableSlotArea.Min.X, usableSlotArea.Min.Y);
                    var socketSize = new Vector2(usableSlotArea.GetWidth(), socketHeight - SlotGaps);
                
                    var reactiveSlotColor = GetReactiveSlotColor(inputDefinition.DefaultValue.ValueType, colorForType, SocketDirections.Input);
                
                    for (var socketIndex = 0; socketIndex < socketCount; socketIndex++)
                    {
                        var usableSocketArea = new ImRect(topLeft, topLeft + socketSize);
                
                        var isSocketHovered = usableSocketArea.Contains(ImGui.GetMousePos());
                        ConnectionSnapEndHelper.RegisterAsPotentialTarget(childUi, inputUi, socketIndex, usableSocketArea);
                
                        bool isGap = false;
                        if (showGaps)
                        {
                            isGap = (socketIndex & 1) == 0;
                        }
                
                        if (!isGap)
                        {
                            var line = showGaps
                                           ? connectedLines[socketIndex >> 1]
                                           : connectedLines[socketIndex];
                            if (_isVisible && socketHeight > 10)
                            {
                                ImGui.PushStyleVar(ImGuiStyleVar.Alpha,
                                                   MathUtils.RemapAndClamp(socketHeight, 10, 20, 0, 0.5f).Clamp(0, 0.5f) * ImGui.GetStyle().Alpha);
                                ImGui.PushFont(Fonts.FontSmall);
                                //ImGui.SetCursorScreenPos(targetPos +  new Vector2(0, -ImGui.GetFontSize()/2));
                                //ImGui.Value(socketIndex % 4 == 0 ? ">" : "", socketIndex);
                
                                var sockedInputIndex = showGaps ? socketIndex / 2 : socketIndex;
                                var markerForFourAligned = sockedInputIndex % 4 == 0 ? " <" : "";
                                _drawList.AddText(targetPos + new Vector2(7, -ImGui.GetFontSize() / 2),
                                                  new Color(MathUtils.RemapAndClamp(socketHeight, 10, 20, 0, 0.5f).Clamp(0, 0.5f)),
                                                  $"{sockedInputIndex}" + markerForFourAligned);
                                ImGui.PopFont();
                                ImGui.PopStyleVar();
                            }
                
                            line.TargetPosition = targetPos;
                            line.TargetNodeArea = connectionBorderArea;
                            line.IsSelected |= childUi.IsSelected | isSocketHovered | isNodeHovered;
                            line.FramesSinceLastUsage = framesSinceLastUpdate;
                            line.IsAboutToBeReplaced = ConnectionSnapEndHelper.IsNextBestTarget(childUi, inputDefinition.Id, socketIndex);
                        }
                
                        DrawMultiInputSocket(childUi, inputDefinition, usableSocketArea, isSocketHovered, socketIndex, isGap, colorForType, reactiveSlotColor);
                
                        targetPos.Y += socketHeight;
                        topLeft.Y += socketHeight;
                    }
                
                    if (_isVisible)
                    {
                        _drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - 8, usableSlotArea.Min.Y),
                                                new Vector2(usableSlotArea.Max.X - 1, usableSlotArea.Min.Y + 2),
                                                reactiveSlotColor);
                
                        _drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - 8, usableSlotArea.Max.Y - 2),
                                                new Vector2(usableSlotArea.Max.X - 1, usableSlotArea.Max.Y),
                                                reactiveSlotColor);
                    }
                }
                else
                {
                    ConnectionSnapEndHelper.RegisterAsPotentialTarget(childUi, inputUi, 0, usableSlotArea);
                    //ConnectionMaker.ConnectionSnapEndHelper.IsNextBestTarget(targetUi, inputDef.Id,0)
                    var isAboutToBeReconnected = ConnectionSnapEndHelper.IsNextBestTarget(childUi, inputDefinition.Id, 0);
                    foreach (var line in connectedLines)
                    {
                        line.TargetPosition = new Vector2(usableSlotArea.Max.X - 4,
                                                          usableSlotArea.GetCenter().Y);
                        line.TargetNodeArea = connectionBorderArea;
                        line.IsSelected |= childUi.IsSelected | hovered | isNodeHovered;
                        line.IsAboutToBeReplaced = isAboutToBeReconnected;
                        line.FramesSinceLastUsage = framesSinceLastUpdate;
                    }
                
                    if (_isVisible)
                    {
                        var isMissing = inputUi.Relevancy == Relevancy.Required && connectedLines.Count == 0;
                        DrawInputSlot(childUi, inputDefinition, usableSlotArea, colorForType, hovered, isMissing);
                    }
                }

                ImGui.PopID();
            }

            // Outputs sockets...
            var outputIndex = 0;
            //foreach(var output in instance.Outputs)
            foreach (var outputDef in childUi.SymbolChild.Symbol.OutputDefinitions)
            {
                var output = instance.Outputs[outputIndex];
                var usableArea = GetUsableOutputSlotArea(childUi, outputIndex);


                if (!preventInteraction)
                {
                    ImGui.SetCursorScreenPos(usableArea.Min);
                }
                
                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + outputDef.Id.GetHashCode());
                ImGui.InvisibleButton("output", usableArea.GetSize());
                THelpers.DebugItemRect();
                var valueType = outputDef.ValueType;
                var colorForType = TypeUiRegistry.Entries[valueType].Color.Fade(Graph.GraphOpacity);
            
                //Note: isItemHovered does not work when dragging is active
                var hovered = ConnectionMaker.TempConnections.Count > 0
                                  ? usableArea.Contains(ImGui.GetMousePos())
                                  : ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup);
            
                // Update connection lines
                var dirtyFlagNumUpdatesWithinFrame = output.DirtyFlag.NumUpdatesWithinFrame;
            
                foreach (var line in global::T3.Editor.Gui.Graph.Graph.Connections.GetLinesFromNodeOutput(childUi, outputDef.Id))
                {
                    line.SourcePosition = new Vector2(usableArea.Max.X, usableArea.GetCenter().Y);
                    line.SourceNodeArea = _selectableScreenRect;
                    line.IsSelected |= hovered;
                    line.ColorForType = colorForType;
                    line.UpdateCount = output.DirtyFlag.NumUpdatesWithinFrame;
            
                    if (childUi.ConnectionStyleOverrides.ContainsKey(outputDef.Id))
                    {
                        line.ColorForType.Rgba.W = 0.3f;
                    }
            
                    line.IsSelected |= childUi.IsSelected || hovered | isNodeHovered;
                }
            
                {
            
                    // Visualize update
                    if (_isVisible)
                    {
                        // Draw update indicator
                        if (output.DirtyFlag.Trigger != DirtyFlagTrigger.None && usableArea.GetHeight() > 6)
                        {
                            var r = usableArea.GetWidth() / 4;
                            var center = new Vector2(usableArea.Max.X + 2*r, usableArea.GetCenter().Y - 3*r);
                            if (output.DirtyFlag.Trigger == DirtyFlagTrigger.Always)
                            {
                                _drawList.AddCircle(center, r, colorForType,3);
                            }
                            else if (output.DirtyFlag.Trigger == DirtyFlagTrigger.Animated)
                            {
                                _drawList.AddCircleFilled(center, r, colorForType, 3);
                            }
                        }
                        
                        DrawOutput(childUi, outputDef, usableArea, colorForType, hovered);
                        if (dirtyFlagNumUpdatesWithinFrame > 0)
                        {
                            var movement = (float)(ImGui.GetTime() * dirtyFlagNumUpdatesWithinFrame) % 1f * (usableArea.GetWidth() - 1);
                            _drawList.AddRectFilled(new Vector2(usableArea.Min.X + movement - 1, usableArea.Min.Y),
                                                    new Vector2(usableArea.Min.X + movement + 1, usableArea.Max.Y),
                                                    new Color(0.2f));
                        }
                    }
                }
            
                outputIndex++;
                ImGui.PopID();
            }
            
        }

        private static void DrawOverlayLine(ImDrawListPtr drawList, Vector2 p1, Vector2 p2)
        {
            var padding = new Vector2(3, 2);
            var size = _usableScreenRect.GetSize() - padding * 2;
            drawList.AddLine(_usableScreenRect.Min + p1 * size + padding,
                             _usableScreenRect.Min + p2 * size + padding,
                             UiColors.StatusWarning.Fade(Graph.GraphOpacity), 3);

        }

        private static void DrawIndicator(Color color, ref int indicatorCount)
        {
            const int s = 4;
            var dx = (s + 1) * indicatorCount;

            var pMin = new Vector2(_usableScreenRect.Max.X - 2 - s - dx,
                                   (_usableScreenRect.Max.Y - 2 - s).Clamp(_usableScreenRect.Min.Y + 2, _usableScreenRect.Max.Y));
            var pMax = new Vector2(_usableScreenRect.Max.X - 2 - dx, _usableScreenRect.Max.Y - 2);
            _drawList.AddRectFilled(pMin, pMax, color.Fade(Graph.GraphOpacity));
            _drawList.AddRect(pMin-Vector2.One, 
                              pMax+Vector2.One, 
                              UiColors.WindowBackground.Fade(0.4f * Graph.GraphOpacity));
            indicatorCount++;
        }

        private static void AdjustGroupLayoutAfterResize(ISelectableCanvasObject childUi, Vector2 newNodeSize)
        {
            if (childUi.Size == newNodeSize)
                return;

            var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var groupMembers = SelectableNodeMovement.FindSnappedNeighbours(parentUi, childUi);
            if (groupMembers.Count > 0)
            {
                var heightDelta = newNodeSize.Y - childUi.Size.Y;
                var offset = new Vector2(0, heightDelta);
                if (heightDelta > 0)
                {
                    foreach (var neighbour in groupMembers)
                    {
                        if (neighbour == childUi)
                            return;

                        if (neighbour.PosOnCanvas.Y > childUi.PosOnCanvas.Y
                            && Math.Abs(neighbour.PosOnCanvas.X - childUi.PosOnCanvas.X) < SelectableNodeMovement.Tolerance)
                        {
                            neighbour.PosOnCanvas += offset;
                        }
                    }
                }

                else if (heightDelta < 0)
                {
                    foreach (var neighbour in groupMembers)
                    {
                        if (neighbour == childUi)
                            return;

                        if (neighbour.PosOnCanvas.Y > childUi.PosOnCanvas.Y
                            && Math.Abs(neighbour.PosOnCanvas.X - childUi.PosOnCanvas.X) < SelectableNodeMovement.Tolerance)
                        {
                            neighbour.PosOnCanvas += offset;
                        }
                    }
                }
            }

            childUi.Size = newNodeSize;
        }

        // Find visible input slots.
        // TODO: this is a major performance hot spot and needs optimization
        private static List<IInputUi> FindVisibleInputUis(SymbolUi symbolUi, SymbolChildUi childUi, ref bool nodeHasHiddenMatchingInputs)
        {
            var connectionsToNode = global::T3.Editor.Gui.Graph.Graph.Connections.GetLinesIntoNode(childUi);

            if (childUi.Style == SymbolChildUi.Styles.Expanded)
            {
                return symbolUi.InputUis.Values.ToList();
            }

            var isNodeHoveredAsConnectionTarget = _hoveredNodeIdForConnectionTarget == childUi.Id
                                                  && ConnectionMaker.TempConnections != null
                                                  && ConnectionMaker.TempConnections.Count == 1
                                                  && ConnectionMaker.TempConnections[0].TargetParentOrChildId == ConnectionMaker.NotConnectedId
                                                  && ConnectionMaker.TempConnections[0].SourceParentOrChildId != childUi.Id;

            _visibleInputs.Clear();
            foreach (var inputUi in symbolUi.InputUis.Values)
            {
                bool inputIsConnectionTarget = false;
                for (int i = 0; i < connectionsToNode.Count; i++)
                {
                    if (connectionsToNode[i].Connection.TargetSlotId == inputUi.Id)
                    {
                        inputIsConnectionTarget = true;
                        break;
                    }
                }

                if (inputUi.Relevancy != Relevancy.Optional || inputIsConnectionTarget)
                {
                    _visibleInputs.Add(inputUi);
                }
                else if (ConnectionMaker.IsMatchingInputType(inputUi.Type))
                {
                    if (isNodeHoveredAsConnectionTarget)
                    {
                        _visibleInputs.Add(inputUi);
                    }
                    else
                    {
                        nodeHasHiddenMatchingInputs = true;
                    }
                }
            }

            return _visibleInputs;
        }

        private enum SocketDirections
        {
            Input,
            Output,
        }

        private static Color GetReactiveSlotColor(Type type, Color colorForType, SocketDirections direction)
        {
            var style = direction == SocketDirections.Input
                            ? ColorVariations.ConnectionLines
                            : ColorVariations.OperatorBackground;
            if (ConnectionMaker.TempConnections.Count > 0)
            {
                if (direction == SocketDirections.Input
                        ? ConnectionMaker.IsMatchingInputType(type)
                        : ConnectionMaker.IsMatchingOutputType(type))
                {
                    var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.8f);
                    colorForType.Rgba.W *= blink;
                    style = ColorVariations.Highlight;
                }
                else
                {
                    style = ColorVariations.OperatorBackgroundIdle;
                }
            }

            return style.Apply(colorForType);
        }

        /// <summary>
        /// Set
        /// </summary>
        /// <param name="instance"></param>
        private static void PreparePreviewAndExpandSelectableArea(Instance instance)
        {
            _previewTextureView = null;
            if (instance.Outputs.Count == 0)
                return;

            var firstOutput = instance.Outputs[0];
            if (!(firstOutput is Slot<Texture2D> textureSlot))
                return;

            var texture = textureSlot.Value;
            if (texture == null || texture.IsDisposed)
                return;

            _previewTextureView = SrvManager.GetSrvForTexture(texture);

            var aspect = (float)texture.Description.Width / texture.Description.Height;
            var opWidth = _usableScreenRect.GetWidth();
            var previewSize = new Vector2(opWidth, opWidth / aspect);

            if (previewSize.Y > opWidth)
            {
                previewSize *= opWidth / previewSize.Y;
            }

            var min = new Vector2(_usableScreenRect.Min.X, _usableScreenRect.Min.Y - previewSize.Y - 1);
            var max = new Vector2(_usableScreenRect.Min.X + previewSize.X, _usableScreenRect.Min.Y - 1);
            _selectableScreenRect.Add(min);
            _previewArea = new ImRect(min, max);
        }

        private static ImRect _previewArea;
        private static ShaderResourceView _previewTextureView;

        private static void DrawPreview()
        {
            if (_previewTextureView == null)
                return;

            global::T3.Editor.Gui.Graph.Graph.DrawList.AddImage((IntPtr)_previewTextureView, _previewArea.Min, 
                                                                _previewArea.Max,
                                                                Vector2.Zero,
                                                                Vector2.One,
                                                                Color.White.Fade(Graph.GraphOpacity));
        }

        private static Vector2 ComputeNodeSize(SymbolChildUi childUi, List<IInputUi> visibleInputUis)
        {
            if (childUi.Style == SymbolChildUi.Styles.Resizable)
            {
                return childUi.Size;
            }

            var additionalMultiInputSlots = 0;
            foreach (var input in visibleInputUis)
            {
                if (!input.InputDefinition.IsMultiInput)
                    continue;

                //TODO: this should be refactored, because it's very slow and is later repeated
                var connectedLines = global::T3.Editor.Gui.Graph.Graph.Connections.GetLinesToNodeInputSlot(childUi, input.Id);
                additionalMultiInputSlots += connectedLines.Count;
            }

            return new Vector2(SymbolChildUi.DefaultOpSize.X,
                               23 + (visibleInputUis.Count + additionalMultiInputSlots) * 13);
        }

        private static void DrawOutput(SymbolChildUi childUi, Symbol.OutputDefinition outputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (ConnectionMaker.IsOutputSlotCurrentConnectionSource(childUi, outputDef))
            {
                _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                        ColorVariations.Highlight.Apply(colorForType));
                
                var isMouseReleasedWithoutDrag =
                    ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                    ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).Length() < UserSettings.Config.ClickThreshold;
                if (isMouseReleasedWithoutDrag)
                {
                    //Graph.Connections.GetLinesFromNodeOutput(childUi, outputDef.Id);
                    GraphCanvas.Current.OpenSymbolBrowserForOutput(childUi, outputDef);
                }
            }
            else if (hovered)
            {
                if (ConnectionMaker.IsMatchingOutputType(outputDef.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorBackgroundHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
                else
                {
                    if (_isVisible)
                    {
                        _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                                ColorVariations.OperatorBackgroundHover.Apply(colorForType));

                        var instance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
                        var output = instance.Outputs.Single(output2 => output2.Id == outputDef.Id);

                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted($".{outputDef.Name}");
                        ImGui.PushFont(Fonts.FontSmall);
                        ImGui.TextColored(UiColors.Gray, $"<{TypeNameRegistry.Entries[outputDef.ValueType]}>\n{output.DirtyFlag.NumUpdatesWithinFrame} Updates\n({output.DirtyFlag.Trigger})");
                        ImGui.PopFont();
                        ImGui.EndTooltip();

                        if(ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            _draggedOutputOpId = childUi.Id;
                            _draggedOutputDefId = outputDef.Id;
                        }

                        // Clicked
                        else
                        {
                            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                            {
                                _draggedOutputOpId = Guid.Empty;
                                _draggedOutputDefId = Guid.Empty;
                                if (ImGui.GetMouseDragDelta().Length() < UserSettings.Config.ClickThreshold)
                                {
                                    ConnectionMaker.OpenSymbolBrowserAtOutput(GraphCanvas.Current.SymbolBrowser, childUi, instance, output.Id);
                                }
                            }
                            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                            {
                                GraphCanvas.Current.EditNodeOutputDialog.OpenForOutput(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                            }
                        }
                    }
                }
            }
            else if (_draggedOutputOpId == childUi.Id && _draggedOutputDefId == outputDef.Id)
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left)
                    && ImGui.GetMouseDragDelta().Length() > UserSettings.Config.ClickThreshold)
                {
                    _draggedOutputOpId = Guid.Empty;
                    _draggedOutputDefId = Guid.Empty;
                    ConnectionMaker.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                }
            }
            else
            {
                var color = GetReactiveSlotColor(outputDef.ValueType, colorForType, SocketDirections.Output);
                var pos = usableArea.Min;
                _drawList.AddRectFilled(
                                        pos,
                                        usableArea.Max,
                                        color
                                       );
            }
        }

        private static Guid _draggedOutputOpId;
        private static Guid _draggedOutputDefId;

        private static Guid _draggedInputOpId;
        private static Guid _draggedInputDefId;

        private static ImRect GetUsableOutputSlotArea(SymbolChildUi targetUi, int outputIndex)
        {
            var thickness = (int)MathUtils.RemapAndClamp(GraphCanvas.Current.Scale.X, 0.5f, 1.2f, (int)(UsableSlotThickness * 0.5f), UsableSlotThickness ) * T3Ui.UiScaleFactor ;

            var opRect = _usableScreenRect;
            var outputCount = targetUi.SymbolChild.Symbol.OutputDefinitions.Count;
            var outputHeight = outputCount == 0
                                   ? opRect.GetHeight()
                                   : (opRect.GetHeight() - 1 + SlotGaps) / outputCount - SlotGaps;
            if (outputHeight <= 0)
                outputHeight = 1;

            return ImRect.RectWithSize(
                                       new Vector2(
                                                   opRect.Max.X + 1, // - GraphNode._usableSlotThickness,
                                                   opRect.Min.Y + (outputHeight + SlotGaps) * outputIndex + 1
                                                  ),
                                       new Vector2(
                                                   thickness,
                                                   outputHeight
                                                  ));
        }

        /// <summary>
        /// Draws slot for non multi-input
        /// </summary>
        private static void DrawInputSlot(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool hovered,
                                          bool isMissing)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef))
            {
            }
            else if (ConnectionSnapEndHelper.IsNextBestTarget(targetUi, inputDef.Id, 0) || hovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorBackgroundHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(
                                            usableArea.Min,
                                            usableArea.Max,
                                            ColorVariations.OperatorBackgroundHover.Apply(colorForType)
                                           );

                    Symbol.Connection connection;
                    SymbolChild sourceOp = null;
                    SymbolChild.Output output = null;
                    ImGui.BeginTooltip();
                    {
                        DrawInputSources(targetUi, inputDef);

                        connection = GraphCanvas.Current.CompositionOp.Symbol.Connections.SingleOrDefault(c => c.TargetParentOrChildId == targetUi.Id
                                                                                                              && c.TargetSlotId == inputDef.Id);
                        if (connection != null)
                        {
                            sourceOp = GraphCanvas.Current.CompositionOp.Symbol.Children.SingleOrDefault(child => child.Id == connection.SourceParentOrChildId);
                            if (sourceOp != null)
                            {
                                if (!sourceOp.Outputs.TryGetValue(connection.SourceSlotId, out output))
                                {
                                    GraphCanvas.Current.CompositionOp.Symbol.Connections.Remove(connection);
                                }
                                //output = sourceOp.Outputs[connection.SourceSlotId];
                            }
                        }

                        ImGui.TextUnformatted($".{inputDef.Name}");
                        ImGui.PushFont(Fonts.FontSmall);
                        ImGui.TextColored(UiColors.Gray, $"<{TypeNameRegistry.Entries[inputDef.DefaultValue.ValueType]}>");
                        ImGui.PopFont();
                    }
                    ImGui.EndTooltip();

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        var createCopy = ImGui.GetIO().KeyCtrl && connection != null;
                        if (createCopy)
                        {
                            if (sourceOp != null)
                            {
                                Log.Debug("Cloning connection from source op...");
                                var parentUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                                var sourceOpUi = parentUi.ChildUis.Single(ui => ui.Id == sourceOp.Id);
                                ConnectionMaker.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, sourceOpUi, output.OutputDefinition);
                            }
                            else if (connection.IsConnectedToSymbolInput)
                            {
                                Log.Debug("Cloning connection from input node...");
                                var inputDef2 = GraphCanvas.Current.CompositionOp.Symbol.InputDefinitions.Single(id => id.Id == connection.SourceSlotId);
                                ConnectionMaker.StartFromInputNode(inputDef2);
                            }
                            else
                            {
                                Log.Warning("This should not happen. Please contact customer support.");
                            }
                        }
                        else
                        {
                            _draggedInputOpId = targetUi.Id;
                            _draggedInputDefId = inputDef.Id;
                        }
                    }
                    else
                    {
                        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                        {
                            _draggedInputOpId = Guid.Empty;
                            _draggedInputDefId = Guid.Empty;
                            if (ImGui.GetMouseDragDelta().Length() < UserSettings.Config.ClickThreshold)
                            {
                                ConnectionMaker.StartFromInputSlot(targetUi.SymbolChild.Parent, targetUi, inputDef);
                                var freePosition = NodeGraphLayouting.FindPositionForNodeConnectedToInput(targetUi.SymbolChild.Parent, targetUi, inputDef);
                                ConnectionMaker.InitSymbolBrowserOnPrimaryGraphWindow(freePosition);
                            }
                            else if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                            {
                                ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                            }
                        }
                    }
                }
            }
            else if (_draggedInputOpId == targetUi.Id && _draggedInputDefId == inputDef.Id)
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Left)
                    && ImGui.GetMouseDragDelta().Length() > UserSettings.Config.ClickThreshold)
                {
                    _draggedInputOpId = Guid.Empty;
                    _draggedInputDefId = Guid.Empty;
                    ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                }
            }            
            else
            {
                var connectionColor = GetReactiveSlotColor(inputDef.DefaultValue.ValueType, colorForType, SocketDirections.Input);
                var pos = new Vector2(
                                      usableArea.Max.X - GraphNode.InputSlotThickness - InputSlotMargin,
                                      usableArea.Min.Y
                                     );
                var size = new Vector2(GraphNode.InputSlotThickness, usableArea.GetHeight());
                _drawList.AddRectFilled(
                                        pos,
                                        pos + size,
                                        connectionColor
                                       );

                if (isMissing)
                {
                    _drawList.AddCircleFilled(
                                              pos + new Vector2(-6, size.Y / 2),
                                              MathF.Min(3, size.Y - 1),
                                              connectionColor
                                             );
                }
            }
        }

        private static void DrawInputSources(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, int inputIndex = 0)
        {
            var compositionUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
            var sources = CollectSourcesForInput(compositionUi, GraphCanvas.Current.CompositionOp, targetUi, inputDef, inputIndex);
            if (sources.Count <= 0)
                return;

            ImGui.PushFont(Fonts.FontSmall);
            foreach (var source in sources)
            {
                ImGui.TextColored(UiColors.Gray, source);
            }

            ImGui.PopFont();
        }

        /// <summary>
        /// Crawl down the graph and collect a list of inputs that contributed to the given input   
        /// </summary>
        private static List<string> CollectSourcesForInput(SymbolUi compositionUi, Instance compositionOp, SymbolChildUi targetUi,
                                                           Symbol.InputDefinition inputDef, int inputIndex)
        {
            var sources = new List<string>();

            while (true)
            {
                Symbol.Connection connection = null;
                if (inputDef.IsMultiInput)
                {
                    var connections = compositionOp.Symbol.Connections.Where(c => c.TargetParentOrChildId == targetUi.Id
                                                                                  && c.TargetSlotId == inputDef.Id).ToList();
                    if (connections.Count > 0 && connections.Count > inputIndex)
                    {
                        connection = connections[inputIndex];
                    }
                }
                else
                {
                    connection = compositionUi.Symbol.Connections.FirstOrDefault(c => c.TargetParentOrChildId == targetUi.Id
                                                                                      && c.TargetSlotId == inputDef.Id
                                                                                );
                }

                if (connection == null)
                    break;

                if (connection.IsConnectedToSymbolInput)
                {
                    var compInputDef = compositionUi.Symbol.InputDefinitions.SingleOrDefault(inp => inp.Id == connection.SourceSlotId);
                    var input = compositionOp.Inputs.SingleOrDefault(inp => inp.Id == connection.SourceSlotId);
                    sources.Insert(0, $". {compInputDef?.Name}  " + GetValueString(input?.Input.Value));
                    break;
                }

                var connectionSourceId = connection.SourceParentOrChildId;
                var connectionSourceUi = compositionUi.ChildUis.SingleOrDefault(c => c.Id == connectionSourceId);

                var instance = compositionOp.Children.SingleOrDefault(child => child.SymbolChildId == connectionSourceId);
                if (connectionSourceUi != null && instance != null)
                {
                    var outputDef = connectionSourceUi.SymbolChild.Symbol.OutputDefinitions.SingleOrDefault(outp => outp.Id == connection.SourceSlotId);
                    var output = instance.Outputs.SingleOrDefault(outp => outp.Id == connection.SourceSlotId);

                    var outputName = (instance.Outputs.Count > 1 && outputDef?.Name != "Output" && outputDef?.Name != "Result")
                                         ? "." + outputDef?.Name
                                         : "";
                    sources.Insert(0, $"{connectionSourceUi?.SymbolChild.ReadableName} {outputName}  " + GetValueString(output));
                }

                if (connectionSourceUi?.SymbolChild.Symbol.InputDefinitions.Count > 0)
                {
                    targetUi = connectionSourceUi;
                    inputDef = connectionSourceUi?.SymbolChild.Symbol.InputDefinitions[0]; // FIXME: this should pick the first connected.
                }
                else
                {
                    break;
                }
            }

            return sources;
        }

        private static string GetValueString(InputValue inputValue)
        {
            return inputValue switch
                       {
                           InputValue<float> f    => $"{f.Value:0.000}",
                           InputValue<int> i      => $"{i.Value:G3}",
                           InputValue<Int3> i     => $"{i.Value:G3}",
                           InputValue<bool> b     => $"{b.Value}",
                           InputValue<Vector3> v3 => $"{v3.Value:0.0}",
                           InputValue<Vector2> v2 => $"{v2.Value:0.0}",
                           InputValue<string> s   => Truncate(s.Value),
                           _                      => ""
                       };
        }
        
        private static string GetValueString(IInputSlot outputSlot)
        {
            
            return outputSlot switch
                       {
                           InputSlot<float> f    => $"{f.GetCurrentValue():0.000}",
                           InputSlot<int> i      => $"{i.GetCurrentValue():G3}",
                           InputSlot<Int3> i     => $"{i.GetCurrentValue():G3}",
                           InputSlot<bool> b     => $"{b.GetCurrentValue()}",
                           InputSlot<Vector3> v3 => $"{v3.GetCurrentValue():0.0}",
                           InputSlot<Vector2> v2 => $"{v2.GetCurrentValue():0.0}",
                           InputSlot<string> s   => Truncate(s.GetCurrentValue()),
                           _                     => ""
                       };
        }

        private static string GetValueString(ISlot outputSlot)
        {
            return outputSlot switch
                       {
                           Slot<float> f    => $"{f.Value:0.00}",
                           Slot<int> i      => $"{i.Value:G3}",
                           Slot<Int3> i     => $"{i.Value:G3}",
                           Slot<bool> b     => $"{b.Value}",
                           Slot<Vector3> v3 => $"{v3.Value:0.0}",
                           Slot<Vector2> v2 => $"{v2.Value:0.0}",
                           Slot<string> s   => Truncate(s.Value),
                           _                => ""
                       };
        }

        private static string Truncate(string input, int maxLength = 10)
        {
            if (input == null)
                return "null";

            if (input.Length < maxLength)
            {
                return input;
            }

            return input[..Math.Min(input.Length, maxLength)] + "...";
        }

        private static void DrawMultiInputSocket(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea,
                                                 bool isInputHovered, int multiInputIndex, bool isGap, Color colorForType,
                                                 Color reactiveSlotColor)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef, multiInputIndex))
            {
            }
            else if (ConnectionSnapEndHelper.IsNextBestTarget(targetUi, inputDef.Id, multiInputIndex) || isInputHovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorBackgroundHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex, true);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(
                                            usableArea.Min,
                                            usableArea.Max,
                                            ColorVariations.OperatorBackgroundHover.Apply(colorForType)
                                           );

                    ImGui.BeginTooltip();
                    {
                        DrawInputSources(targetUi, inputDef, multiInputIndex);

                        // var connectionSource = "";
                        // var connections = GraphCanvas.Current.CompositionOp.Symbol.Connections.Where(c => c.TargetParentOrChildId == targetUi.Id
                        //                                                                                  && c.TargetSlotId == inputDef.Id).ToList();
                        // if (connections.Count > 0 && connections.Count > multiInputIndex)
                        // {
                        //     var connection = connections[multiInputIndex];
                        //
                        //     // var sourceOp =
                        //     //     GraphCanvas.Current.CompositionOp.Symbol.Children.SingleOrDefault(child => child.Id == connection.SourceParentOrChildId);
                        //     // if (sourceOp != null)
                        //     // {
                        //     //     var output = sourceOp.Outputs[connection.SourceSlotId];
                        //     //     connectionSource = sourceOp.ReadableName + "." + output.OutputDefinition.Name;
                        //     //     //connectionSource = sourceOp.ReadableName;
                        //     // }
                        // }

                        // if (!string.IsNullOrEmpty(connectionSource))
                        // {
                        //     ImGui.PushFont(Fonts.FontSmall);
                        //     ImGui.TextColored(Color.Gray, $"{connectionSource} -> ");
                        //     ImGui.PopFont();
                        // }

                        ImGui.TextUnformatted($".{inputDef.Name}");
                        ImGui.PushFont(Fonts.FontSmall);
                        ImGui.TextColored(UiColors.Gray, $"<{TypeNameRegistry.Entries[inputDef.DefaultValue.ValueType]}>");
                        ImGui.PopFont();
                        //ImGui.PopStyleVar();
                    }
                    ImGui.EndTooltip();
                    //ImGui.SetTooltip($"-> .{inputDef.Name}[{multiInputIndex}] <{TypeNameRegistry.Entries[inputDef.DefaultValue.ValueType]}>");
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                        Log.Debug("started connection at MultiInputIndex:" + multiInputIndex);
                    }
                }
            }
            else
            {
                //var pos = usableArea.Min + Vector2.UnitY * GraphNode._inputSlotMargin;
                var gapOffset = isGap ? new Vector2(2, 0) : Vector2.Zero;
                var pos = new Vector2(usableArea.Max.X - InputSlotMargin - InputSlotThickness,
                                      usableArea.Min.Y) - gapOffset;
                var size = new Vector2(InputSlotThickness, usableArea.GetHeight()) + gapOffset;
                _drawList.AddRectFilled(
                                        pos,
                                        pos + size,
                                        reactiveSlotColor
                                       );
            }
        }

        private static float _nodeTitleHeight = 22;

        private static ImRect GetUsableInputSlotSize(int inputIndex, int visibleSlotCount)
        {
            var areaForParams = new ImRect(new Vector2(
                                                       _usableScreenRect.Min.X,
                                                       _usableScreenRect.Min.Y + _nodeTitleHeight),
                                           _usableScreenRect.Max);
            var inputHeight = visibleSlotCount == 0
                                  ? areaForParams.GetHeight()
                                  : (areaForParams.GetHeight() + SlotGaps) / visibleSlotCount - SlotGaps;
            if (inputHeight <= 0)
                inputHeight = 1;

            return ImRect.RectWithSize(
                                       new Vector2(
                                                   areaForParams.Min.X - UsableSlotThickness,
                                                   Math.Min(_selectableScreenRect.Max.Y, areaForParams.Min.Y + (inputHeight + SlotGaps) * inputIndex)
                                                  ),
                                       new Vector2(
                                                   UsableSlotThickness,
                                                   inputHeight
                                                  ));
        }

        private static Color ColorForInputType(Symbol.InputDefinition inputDef)
        {
            return TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;
        }

        #region style variables
        public static Vector2 LabelPos = new(4, 2);
        public static float UsableSlotThickness = 10;
        public static float InputSlotThickness = 3;
        public static float InputSlotMargin = 1;
        public static float SlotGaps = 2;
        public static float OutputSlotMargin = 1;
        #endregion

        private static readonly string _unfoldLabel = (char)Icon.ChevronLeft + "##size";
        private static readonly string _foldLabel = (char)Icon.ChevronDown + "##size";
        private static readonly List<IInputUi> _visibleInputs = new(15); // A static variable to avoid GC allocations

        private static readonly EvaluationContext _evaluationContext = new();

        private static readonly ImageOutputCanvas _imageCanvasForTooltips = new() { DisableDamping = true };
        private static Guid _hoveredNodeIdForConnectionTarget;

        private static ImRect _usableScreenRect;
        private static ImRect _selectableScreenRect;
        private static ImDrawListPtr _drawList;
        private static bool _isVisible;
    }
}