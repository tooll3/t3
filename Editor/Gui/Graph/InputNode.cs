using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Graph
{
    /// <summary>
    /// Draws published input parameters of a <see cref="Symbol"/> and uses <see cref="ConnectionMaker"/> 
    /// create new connections with it.
    /// </summary>
    static class InputNode
    {
        internal static bool Draw(Symbol.InputDefinition inputDef, IInputUi inputUi, int index)
        { 
            var isSelectedOrHovered = false;
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
                    isSelectedOrHovered = true;
                }

                SelectableNodeMovement.Handle(inputUi);

                // Rendering
                var typeColor = TypeUiRegistry.Entries[inputDef.DefaultValue.ValueType].Color;

                var drawList = GraphCanvas.Current.DrawList;
                drawList.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                       hovered
                                           ? ColorVariations.OperatorBackgroundHover.Apply(typeColor)
                                           : ColorVariations.ConnectionLines.Apply(typeColor).Fade(0.5f));

                var inputUiIsSelected = inputUi.IsSelected;
                isSelectedOrHovered |= inputUiIsSelected;
                
                if (inputUiIsSelected)
                {
                    const float thickness = 1;
                    drawList.AddRect(_lastScreenRect.Min - Vector2.One * thickness,
                                     _lastScreenRect.Max + Vector2.One * thickness ,
                                     UiColors.Selection, 0f, 0, thickness);
                }
                
                // Label
                {
                    
                    var isScaledDown = GraphCanvas.Current.Scale.X < 1;
                    ImGui.PushFont(isScaledDown ? Fonts.FontSmall : Fonts.FontBold);
                    
                    // Index
                    var size = ImGui.CalcTextSize(inputDef.Name);
                    var yPos = _lastScreenRect.GetCenter().Y - size.Y / 2;
                    drawList.AddText(new Vector2(_lastScreenRect.Min.X - 20, yPos),
                                     ColorVariations.ConnectionLines.Apply(typeColor),
                                     (index+1) + ".");
                    
                    drawList.PushClipRect(_lastScreenRect.Min, _lastScreenRect.Max, true);
                    var labelPos = new Vector2(
                                          _lastScreenRect.Max.X - size.X -4,
                                          yPos);

                    drawList.AddText(labelPos,
                                     ColorVariations.OperatorLabel.Apply(typeColor),
                                     inputDef.Name);
                    ImGui.PopFont();
                    drawList.PopClipRect();
                }
                

                // Draw slot 
                {
                    var usableSlotArea = GetUsableOutputSlotArea(_lastScreenRect);
                    ImGui.SetCursorScreenPos(usableSlotArea.Min);
                    ImGui.InvisibleButton("output", usableSlotArea.GetSize());
                    THelpers.DebugItemRect();
                    var color = ColorVariations.ConnectionLines.Apply(typeColor).Fade(0.5f);

                    if (!ConnectionMaker.IsInputNodeCurrentConnectionSource(inputDef) && ImGui.IsItemHovered())
                    {
                        color = ColorVariations.Highlight.Apply(typeColor).Fade(0.8f);
                        if (ConnectionMaker.IsMatchingInputType(inputDef.DefaultValue.ValueType))
                        {
                            if (ImGui.IsMouseReleased(0))
                            {
                                ConnectionMaker.CompleteAtSymbolInputNode(GraphCanvas.Current.CompositionOp.Symbol, inputDef);
                            }
                        }
                        else
                        {
                            if (ImGui.IsItemClicked(0))
                            {
                                ConnectionMaker.StartFromInputNode(inputDef);
                            }
                        }
                    }
                    
                    float foldX = (int)(usableSlotArea.Min.X + usableSlotArea.GetWidth() * 0.5f);
                    drawList.AddRectFilled(usableSlotArea.Min+new Vector2(0,0),
                                           new Vector2(foldX, usableSlotArea.Max.Y),
                                           color);
                    drawList.AddTriangleFilled(new Vector2(foldX, usableSlotArea.Min.Y),
                                               new Vector2(usableSlotArea.Max.X, usableSlotArea.GetCenter().Y),
                                               new Vector2(foldX, usableSlotArea.Max.Y),
                                               color);
                }
            }
            ImGui.PopID();
            return isSelectedOrHovered;
        }
        
        private static ImRect GetUsableOutputSlotArea(ImRect opRect)
        {
            var thickness = (int)MathUtils.RemapAndClamp(GraphCanvas.Current.Scale.X, 0.5f, 1.2f, (int)(GraphNode.UsableSlotThickness * 0.5f), GraphNode.UsableSlotThickness) *
                            T3Ui.UiScaleFactor;

            var outputHeight = opRect.GetHeight();
            if (outputHeight <= 0)
                outputHeight = 1;

            return ImRect.RectWithSize(
                                       new Vector2(
                                                   opRect.Max.X + 1, // - GraphNode._usableSlotThickness,
                                                   opRect.Min.Y
                                                  ),
                                       new Vector2(
                                                   thickness,
                                                   outputHeight
                                                  ));
        }

        internal static ImRect _lastScreenRect;
    }
}