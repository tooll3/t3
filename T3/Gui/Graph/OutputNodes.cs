using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.TypeColors;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="BuildingConnections"/> 
    /// create new connections with it.
    /// </summary>
    static class OutputNodes
    {
        //public static void DrawAll()
        //{
        //    var outputUisForSymbol = OutputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
        //    var index = 0;
        //    foreach (var outputDef in GraphCanvas.Current.CompositionOp.Symbol.OutputDefinitions)
        //    {
        //        var outputUi = outputUisForSymbol[outputDef.Id];
        //        Draw(outputDef, outputUi);
        //        index++;
        //    }
        //}


        public static void Draw(Symbol.OutputDefinition outputDef, IOutputUi outputUi)
        {
            ImGui.PushID(outputDef.Id.GetHashCode());
            {
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(outputUi.PosOnCanvas, outputUi.PosOnCanvas + outputUi.Size));
                _lastScreenRect.Floor();

                // Interaction
                ImGui.SetCursorScreenPos(_lastScreenRect.Min);
                ImGui.InvisibleButton("node", _lastScreenRect.GetSize());

                THelpers.DebugItemRect();
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsItemClicked(0))
                    {
                        if (!GraphCanvas.Current.SelectionHandler.SelectedElements.Contains(outputUi))
                        {
                            GraphCanvas.Current.SelectionHandler.SetElement(outputUi);
                        }
                    }

                    if (ImGui.IsMouseDragging(0))
                    {
                        foreach (var e in GraphCanvas.Current.SelectionHandler.SelectedElements)
                        {
                            e.PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        }
                    }
                }

                // Rendering
                var typeColor = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                var dl = GraphCanvas.Current.DrawList;
                dl.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                    hovered
                        ? ColorVariations.OperatorHover.Apply(typeColor)
                        : ColorVariations.OutputNodes.Apply(typeColor));

                dl.AddRectFilled(
                    new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                    new Vector2(_lastScreenRect.Max.X,
                                _lastScreenRect.Max.Y + GraphOperator._inputSlotHeight + GraphOperator._inputSlotMargin),
                    ColorVariations.OperatorInputZone.Apply(typeColor));

                var label = string.Format($"{outputDef.Name}");
                var size = ImGui.CalcTextSize(label);
                var pos = _lastScreenRect.GetCenter() - size / 2;

                dl.AddText(
                    pos,
                    ColorVariations.OperatorLabel.Apply(typeColor),
                    label);

                if (outputUi.IsSelected)
                {
                    dl.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }

                // Draw slot 
                {
                    var virtualRectInCanvas = ImRect.RectWithSize(
                        new Vector2(outputUi.PosOnCanvas.X + 1, outputUi.PosOnCanvas.Y + outputUi.Size.Y),
                        new Vector2(outputUi.Size.X - 2, 6));

                    var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);

                    ImGui.SetCursorScreenPos(rInScreen.Min);
                    ImGui.InvisibleButton("output", rInScreen.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                    //Note: isItemHovered will not work
                    var slotHovered = BuildingConnections.TempConnection != null
                        ? rInScreen.Contains(ImGui.GetMousePos())
                        : ImGui.IsItemHovered();

                    if (BuildingConnections.IsOutputNodeCurrentConnectionTarget(outputDef))
                    {
                        GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            BuildingConnections.Update();
                        }
                    }
                    else if (slotHovered)
                    {
                        if (BuildingConnections.IsMatchingInputType(outputDef.ValueType))
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                BuildingConnections.CompleteAtSymbolOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                        else
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                BuildingConnections.StartFromOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                    }
                    else
                    {
                        GraphCanvas.Current.DrawRectFilled(
                            ImRect.RectWithSize(
                                new Vector2(outputUi.PosOnCanvas.X + 1 + 3, outputUi.PosOnCanvas.Y + outputUi.Size.Y - 1),
                                new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3))
                            , BuildingConnections.IsMatchingInputType(outputDef.ValueType) ? Color.White : color);
                    }
                }

                //dl.ChannelsMerge();
            }
            ImGui.PopID();
        }
        internal static ImRect _lastScreenRect;

    }
}
