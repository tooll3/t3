using System;
using System.Collections.Generic;
using T3.Core;
using T3.Core.Operator;

namespace T3.Gui.Commands
{
    public class TimeClipDeleteCommand : ICommand
    {
        public string Name => "Delete time clip";
        public bool IsUndoable => true;

        public TimeClipDeleteCommand(Symbol compositionSymbol, IEnumerable<Animator.Clip> clipsToRemove)
        {
            _clipsToRemove = clipsToRemove;
            _compositionSymbolId = compositionSymbol.Id;
        }

        public void Do()
        {
            var compositionSymbol = SymbolRegistry.Entries[_compositionSymbolId];
            foreach (var layer in compositionSymbol.Animator.Layers)
            {
                var removedClips = new List<Animator.Clip>();

                foreach (var clip in _clipsToRemove)
                {
                    if (!layer.Clips.Contains(clip))
                        continue;
                    
                    removedClips.Add(clip);
                    layer.Clips.Remove(clip);
                }

                if (removedClips.Count > 0)
                    _layersWithRemovedClips[layer] = removedClips;
            }
        }

        public void Undo()
        {
            foreach (var (layer, removedClips) in _layersWithRemovedClips)
            {
                layer.Clips.AddRange(removedClips);
            }            
        }

        private readonly Guid _compositionSymbolId;
        private readonly IEnumerable<Animator.Clip> _clipsToRemove;
        private readonly Dictionary<Animator.Layer, List<Animator.Clip>> _layersWithRemovedClips = new Dictionary<Animator.Layer, List<Animator.Clip>>();
    }
}