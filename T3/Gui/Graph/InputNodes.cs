using ImGuiNET;
using imHelpers;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.TypeColors;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="BuildingConnections"/> 
    /// create new connections with it.
    /// </summary>
    static class InputNodes
    {
        //public static void DrawAll()
        //{
        //    _drawList = ImGui.GetWindowDrawList();
        //    var inputUisForSymbol = InputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
        //    var index = 0;
        //    foreach (var inputDef in GraphCanvas.Current.CompositionOp.Symbol.InputDefinitions)
        //    {
        //        var inputUi = inputUisForSymbol[inputDef.Id];
        //        Draw(inputDef, inputUi);
        //        index++;
        //    }
        //}

        internal static ImRect _lastScreenRect;

        internal static void Draw(Symbol.InputDefinition inputDef, IInputUi inputUi)
        {
            ImGui.PushID(inputDef.Id.GetHashCode());
            {
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(inputUi.PosOnCanvas, inputUi.PosOnCanvas + inputUi.Size));
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
                        if (!GraphCanvas.Current.SelectionHandler.SelectedElements.Contains(inputUi))
                        {
                            GraphCanvas.Current.SelectionHandler.SetElement(inputUi);
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
                var typeColor = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                var dl = GraphCanvas.Current.DrawList;
                dl.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.OutputNodes.Apply(typeColor));

                dl.AddRectFilled(new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                                 new Vector2(_lastScreenRect.Max.X,
                                             _lastScreenRect.Max.Y + GraphOperator._inputSlotHeight + GraphOperator._inputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));

                var label = string.Format($"{inputDef.Name}");
                var size = ImGui.CalcTextSize(label);
                var pos = _lastScreenRect.GetCenter() - size/2;

                dl.AddText(pos, ColorVariations.OperatorLabel.Apply(typeColor), label);

                if (inputUi.IsSelected)
                {
                    dl.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }

                // Draw slot 
                {
                    var virtualRectInCanvas = ImRect.RectWithSize(new Vector2(inputUi.PosOnCanvas.X + 1, inputUi.PosOnCanvas.Y - 6),
                                                                  new Vector2(inputUi.Size.X - 2, 6));

                    var rInScreen = GraphCanvas.Current.TransformRect(virtualRectInCanvas);

                    ImGui.SetCursorScreenPos(rInScreen.Min);
                    ImGui.InvisibleButton("output", rInScreen.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                    if (BuildingConnections.IsInputNodeCurrentConnectionSource(inputDef))
                    {
                        GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            BuildingConnections.Update();
                        }
                    }
                    else if (ImGui.IsItemHovered())
                    {
                        if (BuildingConnections.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                BuildingConnections.CompleteAtSymbolInputNode(GraphCanvas.Current.CompositionOp.Symbol, inputDef);
                            }
                        }
                        else
                        {
                            GraphCanvas.Current.DrawRectFilled(virtualRectInCanvas, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                BuildingConnections.StartFromInputNode(inputDef);
                            }
                        }
                    }
                    else
                    {
                        GraphCanvas.Current.DrawRectFilled(ImRect.RectWithSize(new Vector2(inputUi.PosOnCanvas.X + 1 + 3, inputUi.PosOnCanvas.Y - 1),
                                                                               new Vector2(virtualRectInCanvas.GetWidth() - 2 - 6, 3)),
                                                           color);
                    }
                }

                //_drawList.ChannelsMerge();
            }
            ImGui.PopID();
        }

        private static ImDrawListPtr _drawList;
    }
}