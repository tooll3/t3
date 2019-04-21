using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    public static class ConnectionLine
    {
        public static void DrawAll(Canvas canvas)
        {
            _canvas = canvas;

            foreach (var c in _canvas.CompositionOp.Symbol.Connections)
            {
                DrawConnection(c);
            }

            if (DraftConnection.TempConnection != null)
                DrawConnection(DraftConnection.TempConnection);
        }

        private static Canvas _canvas;

        private static void DrawConnection(Symbol.Connection c)
        {
            Vector2 sourcePos;
            Color color = Color.White;
            if (c.SourceChildId == DraftConnection.NotConnected)
            {
                sourcePos = ImGui.GetMousePos();
            }
            // Start at input node
            else if (c.SourceChildId == Guid.Empty)
            {
                var inputsForSymbol = InputUiRegistry.Entries[Canvas.Current.CompositionOp.Symbol.Id];
                var inputUi = inputsForSymbol[c.OutputDefinitionId];

                sourcePos = Canvas.ScreenPosFromCanvas(inputUi.Position);
            }
            else
            {
                var sourceUi = SymbolChildUiRegistry.Entries[_canvas.CompositionOp.Symbol.Id][c.SourceChildId];
                var outputIndex = sourceUi.SymbolChild.Symbol.OutputDefinitions.FindIndex(outputDef => outputDef.Id == c.OutputDefinitionId);

                var r = Slots.GetOutputSlotSizeInCanvas(sourceUi, outputIndex);
                sourcePos = Canvas.ScreenPosFromCanvas(r.GetCenter());
                color = InputUiRegistry.EntriesByType[sourceUi.SymbolChild.Symbol.OutputDefinitions[outputIndex].ValueType].Color;
            }

            Vector2 targetPos;
            if (c.TargetChildId == DraftConnection.NotConnected)
            {
                targetPos = ImGui.GetMousePos();
            }
            // Output node
            else if (c.TargetChildId == Guid.Empty)
            {
                targetPos = new Vector2(200, 200);
                //var targetUi = InputUiRegistry.Entries[_canvas.CompositionOp.Symbol.Id][c.TargetChildId];
                //var inputIndex = targetUi.SymbolChild.Symbol.InputDefinitions.FindIndex(inputDef => inputDef.Id == c.InputDefinitionId);
                //var r = Slots.GetInputSlotSizeInCanvas(targetUi, inputIndex);
                //targetPos = Canvas.ScreenPosFromCanvas(r.GetCenter());
            }
            else
            {
                var uiChildrenFromCurrentOp = SymbolChildUiRegistry.Entries[_canvas.CompositionOp.Symbol.Id];
                var targetUi = uiChildrenFromCurrentOp[c.TargetChildId];
                var inputIndex = targetUi.SymbolChild.Symbol.InputDefinitions.FindIndex(inputDef => inputDef.Id == c.InputDefinitionId);
                var r = Slots.GetInputSlotSizeInCanvas(targetUi, inputIndex);
                targetPos = Canvas.ScreenPosFromCanvas(r.GetCenter());
                color = InputUiRegistry.EntriesByType[targetUi.SymbolChild.Symbol.InputDefinitions[inputIndex].DefaultValue.ValueType].Color;
            }

            Canvas.DrawList.AddBezierCurve(
                sourcePos,
                sourcePos + new Vector2(0, -50),
                targetPos + new Vector2(0, 50),
                targetPos,
                color, 3f);

            Canvas.DrawList.AddTriangleFilled(
                targetPos + new Vector2(0, -3),
                targetPos + new Vector2(4, 2),
                targetPos + new Vector2(-4, 2),
                color);
        }
    }
}
