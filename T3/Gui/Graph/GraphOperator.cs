using ImGuiNET;
using imHelpers;
using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.TypeColors;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders a graphic representation of a <see cref="SymbolChild"/> within the current <see cref="GraphCanvasWindow"/>
    /// </summary>
    static class GraphOperator
    {
        public static Vector2 _labelPos = new Vector2(4, 4);
        public static float _usableSlotHeight = 8;
        public static float _inputSlotMargin = 1;
        public static float _inputSlotHeight = 2;
        public static float _slotGaps = 2;
        public static float _outputSlotMargin = 1;
        public static float _outputSlotHeight = 2;
        public static float _multiInputSize = 5;

        public static ImRect _screenRect;

        public static void Draw(SymbolChildUi childUi)
        {
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                _screenRect = GraphCanvas.Current.TransformRect(new ImRect(childUi.PosOnCanvas, childUi.PosOnCanvas + childUi.Size));
                _screenRect.Floor();

                // Interaction
                ImGui.SetCursorScreenPos(_screenRect.Min);
                ImGui.InvisibleButton("node", _screenRect.GetSize());

                THelpers.DebugItemRect();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    T3UI.AddHoveredId(childUi.SymbolChild.Id);
                }

                if (ImGui.IsItemActive())
                {
                    if (ImGui.IsItemClicked(0))
                    {
                        if (!GraphCanvas.Current.SelectionHandler.SelectedElements.Contains(childUi))
                        {
                            GraphCanvas.Current.SelectionHandler.SetElement(childUi);
                        }
                    }
                    if (ImGui.IsMouseDragging(0))
                    {
                        foreach (var e in GraphCanvas.Current.SelectionHandler.SelectedElements)
                        {
                            e.PosOnCanvas += GraphCanvas.Current.InverseTransformDirection(ImGui.GetIO().MouseDelta);
                        }
                    }
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                        var instance = GraphCanvas.Current.CompositionOp.Children.Find(c => c.Symbol == childUi.SymbolChild.Symbol);
                        GraphCanvas.Current.CompositionOp = instance;
                    }
                }
                var hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    NodeDetailsPanel.Draw(childUi);
                }

                // Rendering
                var typeColor = childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0
                    ? TypeUiRegistry.GetPropertiesForType(childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType).Color
                    : Color.Gray;


                var dl = GraphCanvas.Current.DrawList;
                dl.AddRectFilled(_screenRect.Min, _screenRect.Max,
                    hovered
                    ? ColorVariations.OperatorHover.GetVariation(typeColor)
                    : ColorVariations.Operator.GetVariation(typeColor));

                dl.AddRectFilled(
                    new Vector2(_screenRect.Min.X, _screenRect.Max.Y),
                    new Vector2(_screenRect.Max.X, _screenRect.Max.Y + _inputSlotHeight + _inputSlotMargin),
                    ColorVariations.OperatorInputZone.GetVariation(typeColor));

                dl.AddText(_screenRect.Min + _labelPos,
                    ColorVariations.OperatorLabel.GetVariation(typeColor),
                    string.Format($"{childUi.SymbolChild.ReadableName}"));



                if (childUi.IsSelected)
                {
                    dl.AddRect(_screenRect.Min - Vector2.One, _screenRect.Max + Vector2.One, Color.White, 1);
                }

                DrawSlots(childUi);
            }
            ImGui.PopID();


        }

        private static void DrawSlots(SymbolChildUi symbolChildUi)
        {
            for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.OutputDefinitions.Count; slot_idx++)
            {
                Slots.DrawOutputSlot(symbolChildUi, slot_idx);
            }

            for (int slot_idx = 0; slot_idx < symbolChildUi.SymbolChild.Symbol.InputDefinitions.Count; slot_idx++)
            {
                Slots.DrawInputSlot(symbolChildUi, slot_idx);
            }
        }
    }
}
