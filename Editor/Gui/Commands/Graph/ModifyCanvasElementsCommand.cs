using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Selection;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Graph
{
    internal class ModifyCanvasElementsCommand : ICommand
    {
        public string Name => "Move canvas elements";
        public bool IsUndoable => true;

        private class Entry
        {
            public Guid SelectableId;

            public Vector2 OriginalPosOnCanvas { get; set; }
            public Vector2 OriginalSize { get; set; }

            public Vector2 PosOnCanvas { get; set; }
            public Vector2 Size { get; set; }
            public bool IsSelected { get; set; }
        }

        private readonly ISelectionContainer _selectionContainer;
        private Entry[] _entries;
        private readonly Guid _compositionSymbolId;
        private readonly ISelection _nodeSelection;

        public ModifyCanvasElementsCommand(Guid compositionSymbolId, List<ISelectableCanvasObject> selectables, ISelection nodeSelection)
        {
            _compositionSymbolId = compositionSymbolId;
            _nodeSelection = nodeSelection;
            StoreSelectable(selectables);
        }

        private void StoreSelectable(List<ISelectableCanvasObject> selectables)
        {
            _entries = new Entry[selectables.Count()];

            for (int i = 0; i < _entries.Length; i++)
            {
                var selectable = selectables[i];
                var entry = new Entry
                                {
                                    SelectableId = selectable.Id,
                                    OriginalPosOnCanvas = selectable.PosOnCanvas,
                                    OriginalSize = selectable.Size,
                                    PosOnCanvas = selectable.PosOnCanvas,
                                    Size = selectable.Size,
                                    IsSelected = _nodeSelection.IsNodeSelected(selectable)
                                };
                _entries[i] = entry;
            }
        }

        public ModifyCanvasElementsCommand(ISelectionContainer selectionContainer, List<ISelectableCanvasObject> selectables, ISelection nodeSelection)
        {
            _selectionContainer = selectionContainer;
            _nodeSelection = nodeSelection;
            StoreSelectable(selectables);
        }

        public void StoreCurrentValues()
        {
            foreach (var entry in _entries)
            {
                var selectable = GetSelectables()?.SingleOrDefault(s => s.Id == entry.SelectableId);
                if (selectable == null)
                    continue;
                
                entry.PosOnCanvas = selectable.PosOnCanvas;
                entry.Size = selectable.Size;
                entry.IsSelected = _nodeSelection.IsNodeSelected(selectable);
            }
        }

        public void Undo()
        {
            if (_entries == null)
            {
                Log.Warning("Undoing ModifyCanvasElementsCommand without stored values?");
                return;    
            }
            
            foreach (var entry in _entries)
            {
                var selectable = GetSelectables()?.SingleOrDefault(s => s.Id == entry.SelectableId);
                if (selectable == null)
                    continue;
                
                selectable.PosOnCanvas = entry.OriginalPosOnCanvas;
                selectable.Size = entry.OriginalSize;
            }
        }

        public void Do()
        {
            foreach (var entry in _entries)
            {
                var selectable = GetSelectables()?.SingleOrDefault(s => s.Id == entry.SelectableId);
                if (selectable == null)
                    continue;
                
                selectable.PosOnCanvas = entry.PosOnCanvas;
                selectable.Size = entry.Size;
            }
        }

        private IEnumerable<ISelectableCanvasObject>? GetSelectables()
        {
            ISelectionContainer container;
            if(_compositionSymbolId == Guid.Empty)
            {
                container = _selectionContainer;
            }
            else
            {
                SymbolUiRegistry.TryGetValue(_compositionSymbolId, out var symbolUi);
                container = symbolUi;
            }
            
            return container?.GetSelectables();
        }
    }
}