using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui.Commands
{
    public class DeleteSymbolChildCommand : ICommand
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
            public bool IsVisible { get; set; }
        }

        private class ConnectionEntry
        {
            public Symbol.Connection Connection { get; set; }
            public int MultiInputIndex { get; set; }
        }

        public DeleteSymbolChildCommand(SymbolUi compositionSymbolUi, List<SymbolChildUi> childrenToRemove)
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
                                          IsVisible = childUi.IsVisible
                                      };

                var connectionToRemove = compositionSymbol.Connections.FindAll(con => con.SourceParentOrChildId == child.Id
                                                                                      || con.TargetParentOrChildId == child.Id);
                foreach (var con in connectionToRemove)
                {
                    var entry = new ConnectionEntry
                                {
                                    Connection = con,
                                    MultiInputIndex = compositionSymbol
                                                      .Connections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
                                                                                && c.TargetSlotId == con.TargetSlotId)
                                                      .FindIndex(cc => cc == con) // todo: fix this mess! connection rework!
                                };
                    _removedConnections.Add(entry);
                }
            }

            _compositionSymbolId = compositionSymbol.Id;
        }

        public void Do()
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            foreach (var child in _removedChildren)
            {
                compositionSymbolUi.RemoveChild(child.ChildId);
            }
        }

        public void Undo()
        {
            var compositionSymbolUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            foreach (var child in _removedChildren)
            {
                var symbol = SymbolRegistry.Entries[child.SymbolId];
                compositionSymbolUi.AddChild(symbol, child.ChildId, child.PosInCanvas, child.Size, child.IsVisible);
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