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

        public TimeClipDeleteCommand(Instance compositionOp, IEnumerable<ITimeClip> clipsToRemove)
        {
            _clipsToRemove = clipsToRemove;
            _compositionOp = compositionOp;
        }

        public void Do()
        {
            // var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            var allClips = NodeOperations.GetAllTimeClips(_compositionOp);
            foreach (var clipToRemove in _clipsToRemove)
            {
                allClips.Remove(clipToRemove);
            }
        }

        public void Undo()
        {
            // var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            var allClips = NodeOperations.GetAllTimeClips(_compositionOp);
            allClips.AddRange(_clipsToRemove);
        }

        private readonly Instance _compositionOp;
        private readonly IEnumerable<ITimeClip> _clipsToRemove;
    }
}