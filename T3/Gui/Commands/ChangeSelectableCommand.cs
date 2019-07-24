using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Gui.Selection;

namespace T3.Gui.Commands
{
    public class ChangeSelectableCommand : ICommand
    {
        public string Name => "Move..."; // todo: put meaningful name here
        public bool IsUndoable => true;

        private class Entry
        {
            public Guid SelectableId;

            public Vector2 OriginalPosOnCanvas { get; set; }
            public Vector2 OriginalSize { get; set; }
            public bool OriginalIsSelected { get; set; }

            public Vector2 PosOnCanvas { get; set; }
            public Vector2 Size { get; set; }
            public bool IsSelected { get; set; }
        }

        private readonly Entry[] _entries;
        private readonly Guid _compositionSymbolId;

        public ChangeSelectableCommand(Guid compositionSymbolId, List<ISelectable> selectables)
        {
            _compositionSymbolId = compositionSymbolId;
            _entries = new Entry[selectables.Count()];
            for (int i = 0; i < _entries.Length; i++)
            {
                var selectable = selectables[i];
                var entry = new Entry
                            {
                                SelectableId = selectable.Id,
                                OriginalPosOnCanvas = selectable.PosOnCanvas,
                                OriginalSize = selectable.Size,
                                OriginalIsSelected = selectable.IsSelected,
                                PosOnCanvas = selectable.PosOnCanvas,
                                Size = selectable.Size,
                                IsSelected = selectable.IsSelected
                            };
                _entries[i] = entry;
            }
        }

        public void StoreCurrentValues()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            foreach (var entry in _entries)
            {
                var selectable = compositionUi.GetSelectables().SingleOrDefault(s => s.Id == entry.SelectableId);
                if (selectable == null)
                    continue;
                entry.PosOnCanvas = selectable.PosOnCanvas;
                entry.Size = selectable.Size;
                entry.IsSelected = selectable.IsSelected;
            }
        }

        public void Undo()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            foreach (var entry in _entries)
            {
                var selectable = compositionUi.GetSelectables().SingleOrDefault(s => s.Id == entry.SelectableId);
                if (selectable == null)
                    continue;
                selectable.PosOnCanvas = entry.OriginalPosOnCanvas;
                selectable.Size = entry.OriginalSize;
                selectable.IsSelected = entry.OriginalIsSelected;
            }
        }

        public void Do()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            foreach (var entry in _entries)
            {
                var selectable = compositionUi.GetSelectables().SingleOrDefault(s => s.Id == entry.SelectableId);
                if (selectable == null)
                    continue;
                selectable.PosOnCanvas = entry.PosOnCanvas;
                selectable.Size = entry.Size;
                selectable.IsSelected = entry.IsSelected;
            }
        }
    }
}