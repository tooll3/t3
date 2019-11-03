using ImGuiNET;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.InputUi;
using T3.Gui.TypeColors;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
    /// create new connections with it.
    /// </summary>
    static class InputNode
    {

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

                SelectableMovement.Handle(inputUi);

                // Rendering
                var typeColor = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                var dl = GraphCanvas.Current.DrawList;
                dl.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.OutputNodes.Apply(typeColor));

                dl.AddRectFilled(new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                                 new Vector2(_lastScreenRect.Max.X,
                                             _lastScreenRect.Max.Y + GraphNode._inputSlotThickness + GraphNode._inputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));

                var label = string.Format($"{inputDef.Name}");
                var size = ImGui.CalcTextSize(label);
                var pos = _lastScreenRect.GetCenter() - size / 2;

                dl.AddText(pos, ColorVariations.OperatorLabel.Apply(typeColor), label);

                if (inputUi.IsSelected)
                {
                    dl.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }

                // Draw slot 
                {
                    var usableSlotArea = new ImRect(
                                                    new Vector2(_lastScreenRect.Max.X,
                                                                _lastScreenRect.Min.Y),
                                                    new Vector2(_lastScreenRect.Max.X + GraphNode._usableSlotThickness,
                                                                _lastScreenRect.Max.Y)); 


                    ImGui.SetCursorScreenPos(usableSlotArea.Min);
                    ImGui.InvisibleButton("output", usableSlotArea.GetSize());
                    THelpers.DebugItemRect();
                    var color = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                    if (ConnectionMaker.IsInputNodeCurrentConnectionSource(inputDef))
                    {
                        dl.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);

                        if (ImGui.IsMouseDragging(0))
                        {
                            ConnectionMaker.Update();
                        }
                    }
                    else if (ImGui.IsItemHovered())
                    {
                        if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                        {
                            dl.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, color);

                            if (ImGui.IsMouseReleased(0))
                            {
                                ConnectionMaker.CompleteAtSymbolInputNode(GraphCanvas.Current.CompositionOp.Symbol, inputDef);
                            }
                        }
                        else
                        {
                            dl.AddRectFilled(usableSlotArea.Min, usableSlotArea.Max, Color.White);
                            if (ImGui.IsItemClicked(0))
                            {
                                ConnectionMaker.StartFromInputNode(inputDef);
                            }
                        }
                    }
                    else
                    {
                        dl.AddRectFilled(new Vector2(usableSlotArea.Min.X,
                                                     usableSlotArea.Min.Y), 
                                         new Vector2(usableSlotArea.Min.X+ GraphNode._inputSlotThickness,
                                                     usableSlotArea.Max.Y), 
                                         
                                         color);
                    }
                }
            }
            ImGui.PopID();
        }
        internal static ImRect _lastScreenRect;
    }
}