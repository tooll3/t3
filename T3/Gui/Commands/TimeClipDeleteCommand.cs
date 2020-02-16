using System;
using System.Collections.Generic;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.Commands
{
    public class TimeClipDeleteCommand : ICommand
    {
        public string Name => "Delete time clip";
        public bool IsUndoable => true;

        public TimeClipDeleteCommand(Symbol compositionSymbol, IEnumerable<TimeClip> clipsToRemove)
        {
            _clipsToRemove = clipsToRemove;
            _compositionSymbolId = compositionSymbol.Id;
        }

        public void Do()
        {
            var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            var allClips = NodeOperations.GetAllTimeClips(compositionSymbol);
            foreach (var clipToRemove in _clipsToRemove)
            {
                allClips.Remove(clipToRemove);
            }
        }

        public void Undo()
        {
            var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            var allClips = NodeOperations.GetAllTimeClips(compositionSymbol);
            allClips.AddRange(_clipsToRemove);
        }

        private readonly Guid _compositionSymbolId;
        private readonly IEnumerable<TimeClip> _clipsToRemove;
    }
}