using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Templates
{
    /// <summary>
    /// Handles the creation of symbols from <see cref="TemplateDefinition"/> 
    /// </summary>
    public static class TemplateUse
    {
        public static void TryToApplyTemplate(TemplateDefinition template, string symbolName, string nameSpace, string description, string resourceFolder)
        {
            var defaultCanvasWindow = GraphWindow.GetPrimaryGraphWindow();
            if (defaultCanvasWindow == null)
            {
                Log.Warning("Can't create from template without open graph window");
                return;
            }

            var defaultComposition = GraphWindow.GetMainComposition();
            if (defaultComposition == null || !SymbolUiRegistry.Entries.TryGetValue(defaultComposition.Symbol.Id, out var compositionSymbolUi))
            {
                Log.Warning("Can't find default op");
                return;
            }

            var graphCanvas = defaultCanvasWindow.GraphCanvas;
            var centerOnScreen = graphCanvas.WindowPos + graphCanvas.WindowSize / 2;
            var positionOnCanvas2 = graphCanvas.InverseTransformPositionFloat(centerOnScreen);
            var freePosition = FindFreePositionOnCanvas(graphCanvas, positionOnCanvas2);
            var newSymbol = Duplicate.DuplicateAsNewType(compositionSymbolUi, template.TemplateSymbolId, symbolName, nameSpace, description, freePosition);
            
            // Select instance of new symbol
            var newChildUi = compositionSymbolUi.ChildUis.SingleOrDefault(c => c.SymbolChild.Symbol.Id == newSymbol.Id);
            if (newChildUi == null)
            {
                Log.Debug("Creating symbol for template failed.");
                return;
            }
            T3Ui.SelectAndCenterChildIdInView(newChildUi.SymbolChild.Id);
            var newInstance = NodeSelection.GetSelectedInstance(); 
            template.AfterSetupAction?.Invoke(newInstance,
                                              symbolName,
                                              nameSpace, 
                                              description, 
                                              resourceFolder);
            T3Ui.SaveModified();
        }

        private static Vector2 FindFreePositionOnCanvas(GraphCanvas canvas, Vector2 pos)
        {
            if (!SymbolUiRegistry.Entries.TryGetValue(canvas.CompositionOp.Symbol.Id, out var symbolUi))
            {
                Log.Error("Can't find symbol child on composition op?");
                return Vector2.Zero;
            }

            while (true)
            {
                var isPositionFree = true;
                foreach (var childUi in symbolUi.ChildUis)
                {
                    var rect = ImRect.RectWithSize(childUi.PosOnCanvas, childUi.Size);
                    rect.Expand(20);
                    if (!rect.Contains(pos))
                        continue;

                    pos.X += childUi.Size.X;
                    isPositionFree = false;
                    break;
                }

                if (isPositionFree)
                    return pos;
            }
        }
    }
}