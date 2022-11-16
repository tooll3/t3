using System;
using System.Collections.Generic;
using System.Numerics;
using Editor.Gui;
using T3.Editor.Gui.Commands;
using T3.Core.Operator;

namespace T3.Editor.Gui.Commands.Graph
{
    public class DeleteSymbolChildrenCommand : ICommand
    {
        public string Name => "Delete Operator";
        public bool IsUndoable => true;

        private class ChildEntry
        {
            public Guid SymbolId { get; set; }
            public Guid ChildId { get; set; }
            public string ChildName { get; set; }
            public Vector2 PosInCanvas { get; set; }
            public Vector2 Size { get; set; }
        }

        private class ConnectionEntry
        {
            public Symbol.Connection Connection { get; set; }
            public int MultiInputIndex { get; set; }
        }

        public DeleteSymbolChildrenCommand(SymbolUi compositionSymbolUi, List<SymbolChildUi> childrenToRemove)
        {
            var compositionSymbol = compositionSymbolUi.Symbol;
            _removedChildren = new ChildEntry[childrenToRemove.Count];

            for (int i = 0; i < childrenToRemove.Count; i++)
            {
                var childUi = childrenToRemove[i];
                var child = childUi.SymbolChild;
                _removedChildren[i] = new ChildEntry()
                                      {
                                          SymbolId = child.Symbol.Id,
                                          ChildId = child.Id,
                                          ChildName = child.Name,
                                          PosInCanvas = childUi.PosOnCanvas,
                                          Size = childUi.Size,
                                      };
            }

            _compositionSymbolId = compositionSymbol.Id;
        }

        public void Do()
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var compositionSymbol = compositionSymbolUi.Symbol;
            _removedConnections.Clear();

            foreach (var childEntry in _removedChildren)
            {
                // first get the connections to the child that is removed and store these, this must be done before
                // each child is removed in order to preserve restore-able multi input indices.
                var connectionToRemove = compositionSymbol.Connections.FindAll(con => con.SourceParentOrChildId == childEntry.ChildId
                                                                                      || con.TargetParentOrChildId == childEntry.ChildId);
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

                compositionSymbolUi.RemoveChild(childEntry.ChildId);
            }

            _removedConnections.Reverse(); // reverse in order to restore in reversed order
        }

        public void Undo()
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            foreach (var child in _removedChildren)
            {
                var symbol = SymbolRegistry.Entries[child.SymbolId];
                compositionSymbolUi.AddChild(symbol, child.ChildId, child.PosInCanvas, child.Size);
                compositionSymbolUi.Symbol.Children.Find(c => c.Id == child.ChildId).Name = child.ChildName; // todo: ugly
            }

            foreach (var entry in _removedConnections)
            {
                compositionSymbolUi.Symbol.AddConnection(entry.Connection, entry.MultiInputIndex);
            }
        }

        private readonly Guid _compositionSymbolId;
        private readonly ChildEntry[] _removedChildren;
        private readonly List<ConnectionEntry> _removedConnections = new List<ConnectionEntry>();
    }
}