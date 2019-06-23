using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders connection lines between <see cref="GraphOperator"/>s and <see cref="InputNodes"/>s and <see cref="OutputNodes"/>s.
    /// </summary>
    public static class ConnectionLine
    {
        public static void DrawAll(GraphCanvas canvas)
        {
            _canvas = canvas;
            _drawlist = ImGui.GetWindowDrawList();

            foreach (var c in _canvas.CompositionOp.Symbol.Connections)
            {
                DrawConnection(c);
            }

            if (DraftConnection.TempConnection != null)
                DrawConnection(DraftConnection.TempConnection);
        }


        private static void DrawConnection(Symbol.Connection c)
        {
            Color color = Color.White;
            Vector2 sourcePos;

            {
                if (c.SourceSymbolChildId == DraftConnection.NotConnected)
                {
                    sourcePos = ImGui.GetMousePos();
                }

                // Start at input node
                else if (c.SourceSymbolChildId == Guid.Empty)
                {
                    var inputsForSymbol = InputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                    var inputUi = inputsForSymbol[c.SourceSlotId];

                    sourcePos = GraphCanvas.Current.TransformPosition(inputUi.PosOnCanvas);
                }
                else
                {
                    var sourceUi = SymbolChildUiRegistry.Entries[_canvas.CompositionOp.Symbol.Id][c.SourceSymbolChildId];
                    var outputDefinitions = sourceUi.SymbolChild.Symbol.OutputDefinitions;
                    var outputIndex = outputDefinitions.FindIndex(outputDef => outputDef.Id == c.SourceSlotId);

                    var r = Slots.GetOutputSlotSizeInCanvas(sourceUi, outputIndex);
                    sourcePos = GraphCanvas.Current.TransformPosition(r.GetCenter());
                    color = TypeUiRegistry.Entries[outputDefinitions[outputIndex].ValueType].Color;
                }
            }

            Vector2 targetPos;

            {
                if (c.TargetSymboldChildId == DraftConnection.NotConnected)
                {
                    targetPos = ImGui.GetMousePos();
                }

                // Ends at symbol output node
                else if (c.TargetSymboldChildId == Guid.Empty)
                {
                    var outputsForSymbol = OutputUiRegistry.Entries[GraphCanvas.Current.CompositionOp.Symbol.Id];
                    var outputUi = outputsForSymbol[c.TargetSlotId];
                    targetPos = GraphCanvas.Current.TransformPosition(outputUi.PosOnCanvas + outputUi.Size / 2);
                }
                else
                {
                    var uiChildrenFromCurrentOp = SymbolChildUiRegistry.Entries[_canvas.CompositionOp.Symbol.Id];
                    var targetUi = uiChildrenFromCurrentOp[c.TargetSymboldChildId];
                    var inputDefinitions = targetUi.SymbolChild.Symbol.InputDefinitions;
                    var inputIndex = inputDefinitions.FindIndex(inputDef => inputDef.Id == c.TargetSlotId);
                    var r = Slots.GetInputSlotSizeInCanvas(targetUi, inputIndex);
                    targetPos = GraphCanvas.Current.TransformPosition(r.GetCenter());
                    color = TypeUiRegistry.Entries[inputDefinitions[inputIndex].DefaultValue.ValueType].Color;
                }
            }

            _drawlist.AddBezierCurve(
                sourcePos,
                sourcePos + new Vector2(0, -50),
                targetPos + new Vector2(0, 50),
                targetPos,
                color, 3f,
                num_segments: 20);


            _drawlist.AddTriangleFilled(
                targetPos + new Vector2(0, -3),
                targetPos + new Vector2(4, 2),
                targetPos + new Vector2(-4, 2),
                color);
        }

        private static ImDrawListPtr _drawlist;
        private static GraphCanvas _canvas;
    }
}
