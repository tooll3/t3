using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Graph.Dialogs;
using T3.Gui.Graph.Interaction;
using T3.Gui.Graph.Rendering;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders a graphic representation of a <see cref="SymbolChild"/> within the current <see cref="GraphWindow"/>
    /// </summary>
    static class GraphNode
    {
        public static void Draw(SymbolChildUi childUi, Instance instance)
        {
            var symbolUi = SymbolUiRegistry.Entries[childUi.SymbolChild.Symbol.Id];
            var nodeHasHiddenMatchingInputs = false;
            var visibleInputUis = FindVisibleInputUis(symbolUi, childUi, ref nodeHasHiddenMatchingInputs);

            _drawList = Graph.DrawList;
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                var newNodeSize = ComputeNodeSize(childUi, visibleInputUis);
                AdjustGroupLayoutAfterResize(childUi, newNodeSize);
                _usableScreenRect = GraphCanvas.Current.TransformRect(new ImRect(childUi.PosOnCanvas,
                                                                                 childUi.PosOnCanvas + childUi.Size));
                _usableScreenRect.Floor();
                _selectableScreenRect = _usableScreenRect;

                if (UserSettings.Config.ShowThumbnails)
                    PreparePreviewAndExpandSelectableArea(instance);

                // Resize indicator
                if (childUi.Style == SymbolChildUi.Styles.Resizable)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNWSE);
                    ImGui.SetCursorScreenPos(_usableScreenRect.Max - new Vector2(10, 10));
                    ImGui.Button("##resize", new Vector2(10, 10));
                    if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
                    {
                        var delta = GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        childUi.Size += delta;
                    }

                    ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
                }

                // Size toggle
                if (GraphCanvas.Current.Scale.X > 0.7f)
                {
                    var pos = new Vector2(_usableScreenRect.Max.X - 15, _usableScreenRect.Min.Y + 2);

                    ImGui.SetCursorScreenPos(pos);
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
                    ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Color(1, 1, 1, .3f).Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, new Color(1, 1, 1, .3f).Rgba);
                    ImGui.PushFont(Icons.IconFont);

                    if (childUi.Style == SymbolChildUi.Styles.Default)
                    {
                        if (ImGui.Button(UnfoldLabel, new Vector2(16, 16)))
                        {
                            childUi.Style = SymbolChildUi.Styles.Expanded;
                        }
                    }
                    else if (childUi.Style != SymbolChildUi.Styles.Default)
                    {
                        if (ImGui.Button(FoldLabel, new Vector2(16, 16)))
                        {
                            childUi.Style = SymbolChildUi.Styles.Default;
                        }
                    }

                    ImGui.PopFont();
                    ImGui.PopStyleVar();
                    ImGui.PopStyleColor(3);
                }

                // FIXME: proof-of-concept stub for custom UI 

                // var usesCustomUi = DrawCustomUi(instance, _selectableScreenRect);
                var usesCustomUi = childUi.DrawCustomUi(instance, _drawList, _selectableScreenRect);

                // Interaction
                ImGui.SetCursorScreenPos(_selectableScreenRect.Min);
                ImGui.InvisibleButton("node", _selectableScreenRect.GetSize());

                SelectableNodeMovement.Handle(childUi, instance);

                // Tooltip
                if (ImGui.IsItemHovered())
                {
                    SelectableNodeMovement.HighlightSnappedNeighbours(childUi);

                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    T3Ui.AddHoveredId(childUi.SymbolChild.Id);

                    ImGui.SetNextWindowSizeConstraints(new Vector2(200, 120), new Vector2(200, 120));
                    if (UserSettings.Config.HoverMode != GraphCanvas.HoverModes.Disabled
                        && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                    {
                        ImGui.BeginTooltip();
                        {
                            ImageCanvasForTooltips.Update();
                            if (instance.Outputs.Count > 0)
                            {
                                var firstOutput = instance.Outputs[0];
                                IOutputUi outputUi = symbolUi.OutputUis[firstOutput.Id];
                                _evaluationContext.Reset();
                                _evaluationContext.RequestedResolution = new Size2(1280, 720);
                                outputUi.DrawValue(firstOutput, _evaluationContext, recompute: UserSettings.Config.HoverMode == GraphCanvas.HoverModes.Live);
                            }

                            if (!string.IsNullOrEmpty(symbolUi.Description))
                            {
                                ImGui.Spacing();
                                ImGui.PushFont(Fonts.FontSmall);
                                ImGui.PushStyleColor(ImGuiCol.Text, new Color(1, 1, 1, 0.5f).Rgba);
                                ImGui.TextWrapped(symbolUi.Description);
                                ImGui.PopStyleColor();
                                ImGui.PopFont();
                            }
                        }
                        ImGui.EndTooltip();
                    }
                }

                //if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup))
                // A work around to detect if node is below mouse while dragging end of new connection
                if (_selectableScreenRect.Contains(ImGui.GetMousePos()))
                {
                    _hoveredNodeIdForConnectionTarget = childUi.Id;
                }

                var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByPopup) || T3Ui.HoveredIdsLastFrame.Contains(instance.SymbolChildId);

                // A horrible work around to prevent exception because CompositionOp changed during drawing.
                // A better solution would defer setting the compositionOp to the beginning of next frame.
                var justOpenedChild = false;
                if (hovered && ImGui.IsMouseDoubleClicked(0))
                {
                    GraphCanvas.Current.SetCompositionToChildInstance(instance);
                    ImGui.CloseCurrentPopup();
                    justOpenedChild = true;
                }

                // Show Parameter window as context menu
                {
                    var isClicked = ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) &&
                                    ImGui.GetMouseDragDelta(ImGuiMouseButton.Left).LengthSquared() < 4;
                    if (isClicked && !ParameterWindow.IsAnyInstanceVisible() && !justOpenedChild)
                    {
                        SelectionManager.SetSelection(childUi, instance);
                        ImGui.OpenPopup("test");
                    }

                    ImGui.SetNextWindowSizeConstraints(new Vector2(280, 40), new Vector2(280, 320));
                    if (!justOpenedChild && ImGui.BeginPopup("test"))
                    {
                        ImGui.PushFont(Fonts.FontSmall);
                        var compositionSymbolUi = SymbolUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                        var symbolChildUi = compositionSymbolUi.ChildUis.Single(symbolChildUi2 => symbolChildUi2.Id == instance.SymbolChildId);
                        ParameterWindow.DrawParameters(instance, symbolUi, symbolChildUi, compositionSymbolUi);
                        ImGui.PopFont();
                        ImGui.EndPopup();
                    }
                }

                var drawList = GraphCanvas.Current.DrawList;

                // Rendering
                var childInstance = GraphCanvas.Current.CompositionOp.Children.SingleOrDefault(c => c.SymbolChildId == childUi.SymbolChild.Id);
                var output = childInstance?.Outputs.FirstOrDefault();
                var framesSinceLastUpdate = output?.DirtyFlag.FramesSinceLastUpdate ?? 100;

                var typeColor = childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0
                                    ? TypeUiRegistry.GetPropertiesForType(childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType).Color
                                    : Color.Gray;

                if (!usesCustomUi)
                {
                    var backgroundColor = typeColor;
                    if (framesSinceLastUpdate > 2)
                    {
                        var fadeFactor = MathUtils.Remap(framesSinceLastUpdate, 0f, 60f, 1f, 0.4f);
                        backgroundColor.Rgba.W *= fadeFactor;
                    }

                    // background
                    drawList.AddRectFilled(_usableScreenRect.Min, _usableScreenRect.Max,
                                           hovered
                                               ? ColorVariations.OperatorHover.Apply(backgroundColor)
                                               : ColorVariations.Operator.Apply(backgroundColor));
                }

                DrawPreview();

                // outline
                drawList.AddRect(_selectableScreenRect.Min,
                                 _selectableScreenRect.Max + Vector2.One,
                                 new Color(0.08f, 0.08f, 0.08f, 0.8f),
                                 rounding: 0,
                                 ImDrawCornerFlags.None);

                // Animation indicator
                {
                    var compositionOp = GraphCanvas.Current.CompositionOp;
                    if (compositionOp.Symbol.Animator.IsInstanceAnimated(instance))
                    {
                        _drawList.AddRectFilled(new Vector2(_usableScreenRect.Max.X - 5, _usableScreenRect.Max.Y - 12),
                                                new Vector2(_usableScreenRect.Max.X - 2, _usableScreenRect.Max.Y - 3),
                                                Color.Orange);
                    }
                }

                // Hidden inputs indicator
                if (nodeHasHiddenMatchingInputs)
                {
                    var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.5f);
                    var colorForType = TypeUiRegistry.Entries[ConnectionMaker.DraftConnectionType].Color;
                    colorForType.Rgba.W *= blink;
                    _drawList.AddRectFilled(
                                            new Vector2(_usableScreenRect.Min.X, _usableScreenRect.Max.Y + 3),
                                            new Vector2(_usableScreenRect.Min.X + 10, _usableScreenRect.Max.Y + 5),
                                            colorForType);
                }

                // Visualize update
                {
                    var updateCountThisFrame = output?.DirtyFlag.NumUpdatesWithinFrame ?? 0;
                    if (updateCountThisFrame > 0)
                    {
                        const double timeScale = 0.125f;
                        var blink = (float)(ImGui.GetTime() * timeScale * updateCountThisFrame) % 1f * _usableScreenRect.GetWidth();
                        drawList.AddRectFilled(new Vector2(_usableScreenRect.Min.X + blink, _usableScreenRect.Min.Y),
                                               new Vector2(_usableScreenRect.Min.X + blink + 2, _usableScreenRect.Max.Y),
                                               new Color(0.06f));
                    }
                }

                // Label
                if (!usesCustomUi)
                {
                    drawList.PushClipRect(_usableScreenRect.Min, _usableScreenRect.Max, true);
                    ImGui.PushFont(GraphCanvas.Current.Scale.X < 1 ? Fonts.FontSmall : Fonts.FontBold);
                    var isRenamed = !string.IsNullOrEmpty(childUi.SymbolChild.Name);

                    drawList.AddText(_usableScreenRect.Min + LabelPos,
                                     ColorVariations.OperatorLabel.Apply(typeColor),
                                     string.Format(isRenamed ? ("\"" + childUi.SymbolChild.ReadableName + "\"") : childUi.SymbolChild.ReadableName));
                    ImGui.PopFont();
                    drawList.PopClipRect();
                }

                if (childUi.IsSelected)
                {
                    drawList.AddRect(_selectableScreenRect.Min - Vector2.One, _selectableScreenRect.Max + Vector2.One, Color.White, 2);
                }
            }
            ImGui.PopID();

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

                // Note: isItemHovered does not work when being dragged from another item
                var hovered = ConnectionMaker.TempConnection != null
                                  ? usableSlotArea.Contains(ImGui.GetMousePos())
                                  : ImGui.IsItemHovered();

                var isPotentialConnectionTarget = ConnectionMaker.IsMatchingInputType(inputDefinition.DefaultValue.ValueType);
                var colorForType = ColorForInputType(inputDefinition);

                var connectedLines = Graph.Connections.GetLinesToNodeInputSlot(childUi, inputDefinition.Id);

                // Render input Label
                {
                    var inputLabelOpacity = MathUtils.Remap(GraphCanvas.Current.Scale.X,
                                                     0.75f, 1.5f,
                                                     0f, 1f);

                    var screenCursor = usableSlotArea.GetCenter() + new Vector2(14, -7);
                    if (inputLabelOpacity > 0)
                    {
                        ImGui.PushFont(Fonts.FontSmall);
                        var labelColor = ColorVariations.OperatorLabel.Apply(colorForType);
                        labelColor.Rgba.W = inputLabelOpacity;
                        var label = inputDefinition.Name;
                        if (inputDefinition.IsMultiInput)
                        {
                            label += " [...]";
                        }

                        var labelSize = ImGui.CalcTextSize(label);
                        _drawList.AddText(screenCursor, labelColor, label);

                        screenCursor += new Vector2(labelSize.X + 8, 0);

                        // Value
                        ImGui.PushStyleColor(ImGuiCol.Text, labelColor.Rgba);
                        var inputSlot = instance.Inputs.Single(slot => inputDefinition.Id == slot.Id);
                        var valueAsString = inputUi.GetSlotValue(inputSlot);

                        var valueColor = labelColor;
                        valueColor.Rgba.W *= 0.6f;
                        _drawList.AddText(screenCursor, valueColor, valueAsString);
                        ImGui.PopStyleColor();

                        ImGui.PopFont();
                    }
                }

                if (inputDefinition.IsMultiInput)
                {
                    var showGaps = isPotentialConnectionTarget;

                    var socketCount = showGaps
                                          ? connectedLines.Count * 2 + 1
                                          : connectedLines.Count;

                    var socketHeight = (usableSlotArea.GetHeight() + 1) / socketCount;
                    var targetPos = new Vector2(usableSlotArea.Max.X - 2,
                                                usableSlotArea.Min.Y + socketHeight * 0.5f);

                    var topLeft = new Vector2(usableSlotArea.Min.X, usableSlotArea.Min.Y);
                    var socketSize = new Vector2(usableSlotArea.GetWidth(), socketHeight - SlotGaps);

                    var reactiveSlotColor = GetReactiveSlotColor(inputDefinition.DefaultValue.ValueType, colorForType, SocketDirections.Input);

                    for (var index = 0; index < socketCount; index++)
                    {
                        var usableSocketArea = new ImRect(topLeft, topLeft + socketSize);
                        var isSocketHovered = usableSocketArea.Contains(ImGui.GetMousePos());

                        bool isGap = false;
                        if (showGaps)
                        {
                            isGap = (index & 1) == 0;
                        }

                        if (!isGap)
                        {
                            var line = showGaps
                                           ? connectedLines[index >> 1]
                                           : connectedLines[index];

                            line.TargetPosition = targetPos;
                            line.TargetNodeArea = _selectableScreenRect;
                            line.IsSelected |= childUi.IsSelected;
                        }

                        DrawMultiInputSocket(childUi, inputDefinition, usableSocketArea, isSocketHovered, index, isGap, colorForType, reactiveSlotColor);

                        targetPos.Y += socketHeight;
                        topLeft.Y += socketHeight;
                    }

                    _drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - 8, usableSlotArea.Min.Y),
                                            new Vector2(usableSlotArea.Max.X - 1, usableSlotArea.Min.Y + 2),
                                            reactiveSlotColor);

                    _drawList.AddRectFilled(new Vector2(usableSlotArea.Max.X - 8, usableSlotArea.Max.Y - 2),
                                            new Vector2(usableSlotArea.Max.X - 1, usableSlotArea.Max.Y),
                                            reactiveSlotColor);
                }
                else
                {
                    foreach (var line in connectedLines)
                    {
                        line.TargetPosition = new Vector2(usableSlotArea.Max.X - 1,
                                                          usableSlotArea.GetCenter().Y);
                        line.TargetNodeArea = _selectableScreenRect;
                        line.IsSelected |= childUi.IsSelected;
                    }

                    DrawInputSlot(childUi, inputDefinition, usableSlotArea, colorForType, hovered);
                }

                ImGui.PopID();
            }

            // Outputs sockets...
            var outputIndex = 0;
            foreach (var output in childUi.SymbolChild.Symbol.OutputDefinitions)
            {
                var usableArea = GetUsableOutputSlotArea(childUi, outputIndex);
                ImGui.SetCursorScreenPos(usableArea.Min);
                ImGui.PushID(childUi.SymbolChild.Id.GetHashCode() + output.Id.GetHashCode());

                ImGui.InvisibleButton("output", usableArea.GetSize());
                THelpers.DebugItemRect();
                var valueType = output.ValueType;
                var colorForType = TypeUiRegistry.Entries[valueType].Color;

                //Note: isItemHovered does not work when dragging is active
                var hovered = ConnectionMaker.TempConnection != null
                                  ? usableArea.Contains(ImGui.GetMousePos())
                                  : ImGui.IsItemHovered();

                foreach (var line in Graph.Connections.GetLinesFromNodeOutput(childUi, output.Id))
                {
                    line.SourcePosition = new Vector2(usableArea.Max.X, usableArea.GetCenter().Y);
                    line.SourceNodeArea = _selectableScreenRect;

                    var dirtyFlagNumUpdatesWithinFrame = instance.Outputs[outputIndex].DirtyFlag.NumUpdatesWithinFrame;

                    line.Thickness = (1 - 1 / (dirtyFlagNumUpdatesWithinFrame + 1f)) * 3 + 1;

                    line.ColorForType = colorForType;
                    line.IsSelected |= childUi.IsSelected;
                }

                DrawOutput(childUi, output, usableArea, colorForType, hovered);

                outputIndex++;
            }
        }

        private static void AdjustGroupLayoutAfterResize(ISelectableNode childUi, Vector2 newNodeSize)
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

        /// <summary>
        /// @cynic: FIXME: this is a stub for custom UI rendering 
        /// </summary>
        private static bool DrawCustomUi(Instance instance, ImRect selectableScreenRect)
        {
            if (!(instance is Value v))
                return false;

            var opacity = (float)Math.Sin(ImGui.GetTime());
            _drawList.AddRectFilled(_selectableScreenRect.Min, _selectableScreenRect.Max, new Color(1, 1, 0, 0.2f * opacity));
            ImGui.SetCursorScreenPos(_selectableScreenRect.Min + Vector2.One * 10);
            ImGui.Text($"{v.Result.Value:0.00}");
            return true;
        }

        // Find visible input slots.
        // TODO: this is a major performance hot spot and needs optimization
        static List<IInputUi> FindVisibleInputUis(SymbolUi symbolUi, SymbolChildUi childUi, ref bool nodeHasHiddenMatchingInputs)
        {
            var connectionsToNode = Graph.Connections.GetLinesIntoNode(childUi);

            if (childUi.Style == SymbolChildUi.Styles.Expanded)
            {
                return symbolUi.InputUis.Values.ToList();
            }

            var isNodeHoveredConnectionTarget = _hoveredNodeIdForConnectionTarget == childUi.Id
                                                && ConnectionMaker.TempConnection != null
                                                && ConnectionMaker.TempConnection.TargetParentOrChildId == ConnectionMaker.NotConnectedId;

            VisibleInputs.Clear();
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
                    VisibleInputs.Add(inputUi);
                }
                else if (ConnectionMaker.IsMatchingInputType(inputUi.Type))
                {
                    if (isNodeHoveredConnectionTarget)
                    {
                        VisibleInputs.Add(inputUi);
                    }
                    else
                    {
                        nodeHasHiddenMatchingInputs = true;
                    }
                }
            }

            return VisibleInputs;
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
                            : ColorVariations.Operator;
            if (ConnectionMaker.TempConnection != null)
            {
                if (direction == SocketDirections.Input
                        ? ConnectionMaker.IsMatchingInputType(type)
                        : ConnectionMaker.IsMatchingOutputType(type))
                {
                    var blink = (float)(Math.Sin(ImGui.GetTime() * 10) / 2f + 0.5f);
                    colorForType.Rgba.W *= blink;
                    style = ColorVariations.Highlight;
                }
                else
                {
                    style = ColorVariations.Muted;
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

            Graph.DrawList.AddImage((IntPtr)_previewTextureView, _previewArea.Min, _previewArea.Max);
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
                var connectedLines = Graph.Connections.GetLinesToNodeInputSlot(childUi, input.Id);
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

                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (hovered)
            {
                if (ConnectionMaker.IsMatchingOutputType(outputDef.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));

                    var instance = GraphCanvas.Current.CompositionOp.Children.Single(child => child.SymbolChildId == childUi.Id);
                    var output = instance.Outputs.Single(output2 => output2.Id == outputDef.Id);

                    ImGui.SetTooltip($".{outputDef.Name}<{TypeNameRegistry.Entries[outputDef.ValueType]}>\nevaluated: {output.DirtyFlag.NumUpdatesWithinFrame}");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    {
                        ConnectionMaker.StartFromOutputSlot(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        GraphCanvas.Current.EditNodeOutputDialog.OpenForOutput(GraphCanvas.Current.CompositionOp.Symbol, childUi, outputDef);
                    }
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

        private static ImRect GetUsableOutputSlotArea(SymbolChildUi targetUi, int outputIndex)
        {
            var thickness = MathUtils.Remap(GraphCanvas.Current.Scale.X, 0.5f, 1f, 3f, UsableSlotThickness);

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

        private static void DrawInputSlot(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea, Color colorForType, bool hovered)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (hovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

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
                                            ColorVariations.OperatorHover.Apply(colorForType)
                                           );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}<{TypeNameRegistry.Entries[inputDef.DefaultValue.ValueType]}>");
                    ImGui.PopStyleVar();
                    if (ImGui.IsItemClicked(0))
                    {
                        ConnectionMaker.StartFromInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef);
                    }
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
            }
        }

        private static void DrawMultiInputSocket(SymbolChildUi targetUi, Symbol.InputDefinition inputDef, ImRect usableArea,
                                                 bool isInputHovered, int multiInputIndex, bool isGap, Color colorForType,
                                                 Color reactiveSlotColor)
        {
            if (ConnectionMaker.IsInputSlotCurrentConnectionTarget(targetUi, inputDef, multiInputIndex))
            {
                if (ImGui.IsMouseDragging(0))
                {
                    ConnectionMaker.Update();
                }
            }
            else if (isInputHovered)
            {
                if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                {
                    _drawList.AddRectFilled(usableArea.Min, usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType));

                    if (ImGui.IsMouseReleased(0))
                    {
                        ConnectionMaker.CompleteAtInputSlot(GraphCanvas.Current.CompositionOp.Symbol, targetUi, inputDef, multiInputIndex);
                    }
                }
                else
                {
                    _drawList.AddRectFilled(
                                            usableArea.Min,
                                            usableArea.Max,
                                            ColorVariations.OperatorHover.Apply(colorForType)
                                           );

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 2));
                    ImGui.SetTooltip($"-> .{inputDef.Name}[{multiInputIndex}] <{TypeNameRegistry.Entries[inputDef.DefaultValue.ValueType]}>");
                    ImGui.PopStyleVar();
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
                                                   areaForParams.Min.Y + (inputHeight + SlotGaps) * inputIndex
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
        public static Vector2 LabelPos = new Vector2(4, 2);
        public static float UsableSlotThickness = 10;
        public static float InputSlotThickness = 3;
        public static float InputSlotMargin = 1;
        public static float SlotGaps = 2;
        public static float OutputSlotMargin = 1;
        #endregion

        private static readonly string UnfoldLabel = (char)Icon.ChevronLeft + "##size";
        private static readonly string FoldLabel = (char)Icon.ChevronDown + "##size";
        private static readonly List<IInputUi> VisibleInputs = new List<IInputUi>(15);

        private static EvaluationContext _evaluationContext = new EvaluationContext();

        private static readonly ImageOutputCanvas ImageCanvasForTooltips = new ImageOutputCanvas();
        private static Guid _hoveredNodeIdForConnectionTarget;

        private static ImRect _usableScreenRect;
        private static ImRect _selectableScreenRect;
        private static ImDrawListPtr _drawList;
    }
}