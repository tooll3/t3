using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Templates
{
    /// <summary>
    /// Handles the creation of symbols from <see cref="TemplateDefinition"/> 
    /// </summary>
    public static class TemplateUse
    {
        internal static void TryToApplyTemplate(TemplateDefinition template, string symbolName, string nameSpace, string description, EditableSymbolProject project)
        {
            var defaultCanvasWindow = GraphWindow.Focused;
            if (defaultCanvasWindow == null)
            {
                EditorUi.Instance.ShowMessageBox("Can't create from template without open graph window");
                return;
            }

            var compositionSymbolUi = defaultCanvasWindow.CompositionOp.GetSymbolUi();

            var graphCanvas = defaultCanvasWindow.GraphCanvas;
            var centerOnScreen = graphCanvas.WindowPos + graphCanvas.WindowSize / 2;
            var positionOnCanvas2 = graphCanvas.InverseTransformPositionFloat(centerOnScreen);
            var freePosition = FindFreePositionOnCanvas(compositionSymbolUi, positionOnCanvas2);
            var newSymbol = Duplicate.DuplicateAsNewType(compositionSymbolUi, project, template.TemplateSymbolId, symbolName, nameSpace, description, freePosition);
            
            // Select instance of new symbol
            var newChildUi = compositionSymbolUi.ChildUis.SingleOrDefault(c => c.SymbolChild.Symbol.Id == newSymbol.Id);
            if (newChildUi == null)
            {
                Log.Debug("Creating symbol for template failed.");
                return;
            }
            T3Ui.SelectAndCenterChildIdInView(newChildUi.SymbolChild.Id);
            var newInstance = defaultCanvasWindow.GraphCanvas.NodeSelection.GetSelectedInstanceWithoutComposition(); 
            template.AfterSetupAction?.Invoke(newInstance,
                                              symbolName,
                                              nameSpace, 
                                              description, 
                                              project.ResourcesFolder);
            T3Ui.Save(false);
        }

        private static Vector2 FindFreePositionOnCanvas(SymbolUi compositionSymbolUi, Vector2 pos)
        {
            while (true)
            {
                var isPositionFree = true;
                foreach (var childUi in compositionSymbolUi.ChildUis)
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