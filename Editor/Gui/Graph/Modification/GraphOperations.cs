using System.IO;
using Newtonsoft.Json;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.UiModel;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace T3.Editor.Gui.Graph.Modification
{
    internal static class GraphOperations
    {
        public static SymbolChildUi AddSymbolChild(Symbol symbol, SymbolUi parentUi, Vector2 positionOnCanvas)
        {
            var addCommand = new AddSymbolChildCommand(parentUi.Symbol, symbol.Id) { PosOnCanvas = positionOnCanvas };
            UndoRedoStack.AddAndExecute(addCommand);
            
            var parentSymbol = parentUi.Symbol;
            var newSymbolChild = parentSymbol.Children[addCommand.AddedChildId];

            // Select new node
            return newSymbolChild.GetSymbolChildUi();
        }

        public static string CopyNodesAsJson(Instance composition, 
                                             IEnumerable<SymbolChildUi> selectedChildren, 
                                             List<Annotation> selectedAnnotations)
        {
            var resultJsonString = string.Empty;

            Guid newGuid = new Guid();
            var allSymbolUis = EditorSymbolPackage.AllSymbols.Select(x => x.Id).ToHashSet();

            while (allSymbolUis.Contains(newGuid))
                newGuid = new Guid();
            
            var containerOp = new Symbol(typeof(object), Guid.NewGuid(), null);
            var newContainerUi = new SymbolUi(containerOp, true);
            
            var cmd = new CopySymbolChildrenCommand(composition.GetSymbolUi(),
                                                    selectedChildren,
                                                    selectedAnnotations,
                                                    newContainerUi,
                                                    Vector2.Zero);
            cmd.Do();

            using (var writer = new StringWriter())
            {
                var jsonWriter = new JsonTextWriter(writer);
                jsonWriter.WriteStartArray();
                SymbolJson.WriteSymbol(containerOp, jsonWriter);
                SymbolUiJson.WriteSymbolUi(newContainerUi, jsonWriter);
                jsonWriter.WriteEndArray();

                try
                {
                    resultJsonString = writer.ToString();
                }
                catch (Exception)
                {
                    Log.Error("Could not copy elements to clipboard. Perhaps a tool like TeamViewer locks it.");
                }
            }

            return resultJsonString;
        }
    }
}