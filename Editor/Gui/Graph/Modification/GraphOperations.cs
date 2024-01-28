using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using T3.Core.Logging;
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
        public static SymbolChildUi AddSymbolChild(Symbol symbol, Symbol parent, Vector2 positionOnCanvas)
        {
            var addCommand = new AddSymbolChildCommand(parent, symbol.Id) { PosOnCanvas = positionOnCanvas };
            UndoRedoStack.AddAndExecute(addCommand);
            var newSymbolChild = parent.Children.Single(entry => entry.Id == addCommand.AddedChildId);

            // Select new node
            var symbolUi = SymbolUiRegistry.Entries[parent.Id];
            var childUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);

            return childUi;
        }

        public static string CopyNodesAsJson(Guid symbolId, 
                                             IEnumerable<SymbolChildUi> selectedChildren, 
                                             List<Annotation> selectedAnnotations)
        {
            var resultJsonString = string.Empty;
            var containerOp = new Symbol(typeof(object), Guid.NewGuid(), null);
            var newContainerUi = new SymbolUi(containerOp);
            
            if(!SymbolUiRegistry.EntriesEditable.TryAdd(newContainerUi.Symbol.Id, newContainerUi))
                throw new Exception("Could not add new container to SymbolUiRegistry. Guid collision!");

            var compositionSymbolUi = SymbolUiRegistry.Entries[symbolId];
            var cmd = new CopySymbolChildrenCommand(compositionSymbolUi,
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

            SymbolUiRegistry.EntriesEditable.Remove(newContainerUi.Symbol.Id, out _);
            return resultJsonString;
        }
    }
}