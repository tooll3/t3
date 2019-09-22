using ImGuiNET;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;
using T3.Gui.TypeColors;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
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

                SelectableMovement.Handle(outputUi);

                // Rendering
                var typeColor = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                var dl = GraphCanvas.Current.DrawList;
                dl.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.OutputNodes.Apply(typeColor));

                dl.AddRectFilled(new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                                 new Vector2(_lastScreenRect.Max.X,
                                             _lastScreenRect.Max.Y + GraphOperator._inputSlotHeight + GraphOperator._inputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));

                var label = string.Format($"{outputDef.Name}");
                var size = ImGui.CalcTextSize(label);
                var pos = _lastScreenRect.GetCenter() - size / 2;

                dl.AddText(pos, ColorVariations.OperatorLabel.Apply(typeColor), label);

                if (outputUi.IsSelected)
                {
                    dl.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }

                // Draw slot 
                {
                    var virtualRectInCanvas = ImRect.RectWithSize(new Vector2(outputUi.PosOnCanvas.X + 1, outputUi.PosOnCanvas.Y + outputUi.Size.Y),
                                                                  new Vector2(outputUi.Size.X - 2, 6));

                    var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);

                    ImGui.SetCursorScreenPos(rInScreen.Min);
                    ImGui.InvisibleButton("output", rInScreen.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[outputDef.ValueType].Color;

                    //Note: isItemHovered will not work
                    var slotHovered = ConnectionMaker.TempConnection != null
                                          ? rInScreen.Contains(ImGui.GetMousePos())
                                          : ImGui.IsItemHovered();

                    if (ConnectionMaker.IsOutputNodeCurrentConnectionTarget(outputDef))
                    {
                        GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            ConnectionMaker.Update();
                        }
                    }
                    else if (slotHovered)
                    {
                        if (ConnectionMaker.IsMatchingInputType(outputDef.ValueType))
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                ConnectionMaker.CompleteAtSymbolOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                        else
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                ConnectionMaker.StartFromOutputNode(GraphCanvas.Current.CompositionOp.Symbol, outputDef);
                            }
                        }
                    }
                    else
                    {
                        GraphCanvas.Current.DrawRectFilled(ImRect.RectWithSize(new Vector2(outputUi.PosOnCanvas.X + 1 + 3,
                                                                                           outputUi.PosOnCanvas.Y + outputUi.Size.Y - 1),
                                                                               new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3)),
                                                           ConnectionMaker.IsMatchingInputType(outputDef.ValueType) ? Color.White : color);
                    }
                }

                //dl.ChannelsMerge();
            }
            ImGui.PopID();
        }

        internal static ImRect _lastScreenRect;
    }
}