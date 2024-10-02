using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;

namespace T3.Editor.Gui.Commands.Animation
{
    public class MoveTimeClipsCommand : ICommand
    {
        public string Name => "Move Time Clip";
        public bool IsUndoable => true;
        
        private class  Entry
        {
            public Guid Id { get; set; }
            public TimeRange TimeRange { get; set; }
            public TimeRange SourceRange { get; set; }
            public int LayerIndex { get; set; }
        }

        private readonly Entry[] _entries;
        private readonly Instance _compositionOp;

        public MoveTimeClipsCommand(Instance compositionOp, IReadOnlyList<ITimeClip> clips)
        {
            _compositionOp = compositionOp;
            _entries = new Entry[clips.Count()];
            for (var i = 0; i < _entries.Length; i++)
            {
                var clip = clips[i];
                var entry = new Entry
                            {
                                Id = clip.Id,
                                TimeRange =clip.TimeRange.Clone(),
                                SourceRange =clip.SourceRange.Clone(),
                                LayerIndex = clip.LayerIndex,
                            };
                _entries[i] = entry;
            }
        }
        

        public void StoreCurrentValues()
        {
            foreach (var clip in Structure.GetAllTimeClips(_compositionOp))
            {
                var selectedEntry = _entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                selectedEntry.TimeRange = clip.TimeRange.Clone();
                selectedEntry.SourceRange = clip.SourceRange.Clone();
                selectedEntry.LayerIndex = clip.LayerIndex;
            }            
        }


        public void Undo()
        {
            foreach (var clip in Structure.GetAllTimeClips(_compositionOp))
            {
                var selectedEntry = _entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                clip.TimeRange = selectedEntry.TimeRange.Clone();
                clip.SourceRange = selectedEntry.SourceRange.Clone();
                clip.LayerIndex = selectedEntry.LayerIndex;
            }
        }

        public void Do()
        {
            var allTimeClips = Structure.GetAllTimeClips(_compositionOp).ToList();
            
            foreach (var clip in allTimeClips)
            {
                var selectedEntry = _entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;
                
                clip.TimeRange = selectedEntry.TimeRange.Clone();
                clip.SourceRange = selectedEntry.SourceRange.Clone();
                clip.LayerIndex = selectedEntry.LayerIndex;

                while (clip.IsClipOverlappingOthers(allTimeClips))
                {
                    clip.LayerIndex++;
                }
            }
        }
    }
}