using ImGuiNET;
using UiHelpers;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.TypeColors;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders a graphic representation of a <see cref="SymbolChild"/> within the current <see cref="GraphCanvasWindow"/>
    /// </summary>
    static class GraphOperator
    {
        private static ChangeSelectableCommand _moveCommand = null;

        public static void Draw(SymbolChildUi childUi)
        {
            ImGui.PushID(childUi.SymbolChild.Id.GetHashCode());
            {
                _lastScreenRect = GraphCanvas.Current.TransformRect(new ImRect(childUi.PosOnCanvas, childUi.PosOnCanvas + childUi.Size));
                _lastScreenRect.Floor();

                // Interaction
                ImGui.SetCursorScreenPos(_lastScreenRect.Min);
                ImGui.InvisibleButton("node", _lastScreenRect.GetSize());

                THelpers.DebugItemRect();
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    T3UI.AddHoveredId(childUi.SymbolChild.Id);
                }

                SelectableMovement.Handle(childUi);

                if (ImGui.IsItemActive() && ImGui.IsMouseDoubleClicked(0))
                {
                    var instance = GraphCanvas.Current.CompositionOp.Children.Find(c => c.Symbol == childUi.SymbolChild.Symbol);
                    GraphCanvas.Current.CompositionOp = instance;
                }

                bool hovered = ImGui.IsItemHovered();
                if (hovered)
                {
                    //NodeDetailsPanel.Draw(childUi);
                }

                // Rendering
                var typeColor = childUi.SymbolChild.Symbol.OutputDefinitions.Count > 0
                                    ? TypeUiRegistry.GetPropertiesForType(childUi.SymbolChild.Symbol.OutputDefinitions[0].ValueType).Color
                                    : Color.Gray;

                var dl = GraphCanvas.Current.DrawList;
                dl.AddRectFilled(_lastScreenRect.Min, _lastScreenRect.Max,
                                 hovered
                                     ? ColorVariations.OperatorHover.Apply(typeColor)
                                     : ColorVariations.Operator.Apply(typeColor));

                dl.AddRectFilled(new Vector2(_lastScreenRect.Min.X, _lastScreenRect.Max.Y),
                                 new Vector2(_lastScreenRect.Max.X, _lastScreenRect.Max.Y + _inputSlotHeight + _inputSlotMargin),
                                 ColorVariations.OperatorInputZone.Apply(typeColor));

                dl.AddText(_lastScreenRect.Min + _labelPos,
                           ColorVariations.OperatorLabel.Apply(typeColor),
                           string.Format($"{childUi.SymbolChild.ReadableName}"));

                if (childUi.IsSelected)
                {
                    dl.AddRect(_lastScreenRect.Min - Vector2.One, _lastScreenRect.Max + Vector2.One, Color.White, 1);
                }
            }
            ImGui.PopID();
        }

        #region style variables
        public static Vector2 _labelPos = new Vector2(4, 4);
        public static float _usableSlotHeight = 12;
        public static float _inputSlotMargin = 1;
        public static float _inputSlotHeight = 2;
        public static float _slotGaps = 2;
        public static float _outputSlotMargin = 1;
        public static float _outputSlotHeight = 2;
        public static float _multiInputSize = 5;
        #endregion

        public static ImRect _lastScreenRect;
    }
}