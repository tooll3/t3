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
            public ISelectable Selectable { get; set; }

            public Vector2 OriginalPosOnCanvas { get; set; }
            public Vector2 OriginalSize { get; set; }
            public bool OriginalIsSelected { get; set; }

            public Vector2 PosOnCanvas { get; set; }
            public Vector2 Size { get; set; }
            public bool IsSelected { get; set; }
        }

        private readonly Entry[] _entries;

        public ChangeSelectableCommand(List<ISelectable> selectables)
        {
            _entries = new Entry[selectables.Count()];
            for (int i = 0; i < _entries.Length; i++)
            {
                var selectable = selectables[i];
                var entry = new Entry
                            {
                                Selectable = selectable,
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
            foreach (var entry in _entries)
            {
                entry.PosOnCanvas = entry.Selectable.PosOnCanvas;
                entry.Size = entry.Selectable.Size;
                entry.IsSelected = entry.Selectable.IsSelected;
            }
        }

        public void Undo()
        {
            foreach (var entry in _entries)
            {
                entry.Selectable.PosOnCanvas = entry.OriginalPosOnCanvas;
                entry.Selectable.Size = entry.OriginalSize;
                entry.Selectable.IsSelected = entry.OriginalIsSelected;
            }
        }

        public void Do()
        {
            foreach (var entry in _entries)
            {
                entry.Selectable.PosOnCanvas = entry.PosOnCanvas;
                entry.Selectable.Size = entry.Size;
                entry.Selectable.IsSelected = entry.IsSelected;
            }
        }
    }
}