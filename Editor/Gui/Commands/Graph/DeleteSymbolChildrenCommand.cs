using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.Commands.Graph
{
    public class DeleteSymbolChildrenCommand : ICommand
    {
        public string Name => "Delete Operator";
        public bool IsUndoable => true;
        
        public DeleteSymbolChildrenCommand(SymbolUi compositionSymbolUi, List<SymbolChildUi> uiChildrenToRemove)
        {
            var compositionSymbol = compositionSymbolUi.Symbol;
            _removedChildren = new ChildEntry[uiChildrenToRemove.Count];

            for (int i = 0; i < uiChildrenToRemove.Count; i++)
            {
                var childUi = uiChildrenToRemove[i];
                var child = childUi.SymbolChild;

                var values = new Dictionary<Guid, InputValue>();
                foreach (var (id, input) in child.Inputs)
                {
                    if(input.IsDefault )
                        continue;

                    values[id] = input.Value.Clone();
                }

                _removedChildren[i] = new ChildEntry()
                                          {
                                              SymbolId = child.Symbol.Id,
                                              ChildId = child.Id,
                                              ChildName = child.Name,
                                              PosInCanvas = childUi.PosOnCanvas,
                                              Size = childUi.Size,
                                              OriginalValuesForInputsIds = values
                                          };
            }

            _compositionSymbolId = compositionSymbol.Id;
        }

        public void Do()
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var compositionSymbol = compositionSymbolUi.Symbol;
            _removedConnections.Clear();

            foreach (var childUndoData in _removedChildren)
            {
                // first get the connections to the child that is removed and store these, this must be done before
                // each child is removed in order to preserve restore-able multi input indices.
                var connectionToRemove = compositionSymbol.Connections.FindAll(con => con.SourceParentOrChildId == childUndoData.ChildId
                                                                                      || con.TargetParentOrChildId == childUndoData.ChildId);
                connectionToRemove.Reverse();
                foreach (var con in connectionToRemove)
                {
                    var entry = new ConnectionEntry
                                {
                                    Connection = con,
                                    MultiInputIndex = compositionSymbol.GetMultiInputIndexFor(con)
                                };
                    _removedConnections.Add(entry);
                    compositionSymbol.RemoveConnection(con, entry.MultiInputIndex);
                }

                compositionSymbolUi.RemoveChild(childUndoData.ChildId);
            }

            _removedConnections.Reverse(); // reverse in order to restore in reversed order
        }

        public void Undo()
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            foreach (var childUndoData in _removedChildren)
            {
                var symbol = SymbolRegistry.Entries[childUndoData.SymbolId];
                var id = compositionSymbolUi.AddChild(symbol, childUndoData.ChildId, childUndoData.PosInCanvas, childUndoData.Size);
                var symbolChild = compositionSymbolUi.Symbol.Children.FirstOrDefault(c => c.Id == childUndoData.ChildId);
                
                foreach (var (inputId, input) in symbolChild.Inputs)
                {
                    if(childUndoData.OriginalValuesForInputsIds.TryGetValue(inputId, out var originalValue))
                    {
                        input.Value.Assign(originalValue);
                        input.IsDefault = false;
                    }
                }
                    
                
                compositionSymbolUi.Symbol.Children.Find(c => c.Id == childUndoData.ChildId).Name = childUndoData.ChildName; // todo: ugly
            }

            foreach (var entry in _removedConnections)
            {
                compositionSymbolUi.Symbol.AddConnection(entry.Connection, entry.MultiInputIndex);
            }
        }
        
        private class ChildEntry
        {
            public Guid SymbolId { get; set; }
            public Guid ChildId { get; set; }
            public string ChildName { get; set; }
            public Vector2 PosInCanvas { get; set; }
            public Vector2 Size { get; set; }
            
            public Dictionary<Guid, InputValue> OriginalValuesForInputsIds { get; set; }
        }

        private class ConnectionEntry
        {
            public Symbol.Connection Connection { get; set; }
            public int MultiInputIndex { get; set; }
        }

        private readonly Guid _compositionSymbolId;
        private readonly ChildEntry[] _removedChildren;
        private readonly List<ConnectionEntry> _removedConnections = new List<ConnectionEntry>();
    }
}