using ImGuiNET;
using System;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    public static class ConnectionLine
    {
        public static void DrawAll(GraphCanvas canvas)
        {
            _canvas = canvas;

            foreach (var c in _canvas.CompositionOp.Symbol.Connections)
            {
                DrawConnection(c);
            }

            if (DraftConnection.TempConnection != null)
                DrawConnection(DraftConnection.TempConnection);
        }

        private static GraphCanvas _canvas;
        private static Symbol _parent;


        private static void DrawConnection(Symbol.Connection c)
        {
            Vector2 sourcePos;
            if (c.SourceChildId == Guid.Empty)
            {
                sourcePos = ImGui.GetMousePos();
            }
            else
            {
                var sourceUi = SymbolChildUiRegistry.Entries[_canvas.CompositionOp.Symbol.Id][c.SourceChildId];
                sourcePos = _canvas.ScreenPosFromCanvas(sourceUi.Position);
            }

            Vector2 targetPos;
            if (c.TargetChildId == Guid.Empty)
            {
                targetPos = ImGui.GetMousePos();
            }
            else
            {
                var targetUi = SymbolChildUiRegistry.Entries[_canvas.CompositionOp.Symbol.Id][c.TargetChildId];

                var inputIndex = targetUi.SymbolChild.Symbol.InputDefinitions.FindIndex(inputDef => inputDef.Id == c.InputDefinitionId);
                var r = GraphOperator.GetImputSlotSizeInCanvas(targetUi, inputIndex);

                targetPos = _canvas.ScreenPosFromCanvas(r.GetCenter());
                //targetPos = _canvas.ScreenPosFromCanvas(targetUi.Position + new Vector2(0, targetUi.Size.Y));
            }

            _canvas.DrawList.AddBezierCurve(
                sourcePos,
                sourcePos + new Vector2(0, -50),
                targetPos + new Vector2(0, 50),
                targetPos,
                Color.White, 3f);

            _canvas.DrawList.AddTriangleFilled(
                targetPos + new Vector2(0, -3),
                targetPos + new Vector2(4, 2),
                targetPos + new Vector2(-4, 2),
                Color.White);
        }
    }
}
