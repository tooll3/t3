using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.Commands
{
    public class MoveTimeClipCommand : ICommand
    {
        public string Name => "Move Time Clip";
        public bool IsUndoable => true;

        public class Entry
        {
            public Guid Id;
            public float OriginalStartTime { get; set; }
            public float OriginalEndTime { get; set; }
            public float OriginalSourceStartTime { get; set; }
            public float OriginalSourceEndTime { get; set; }
            public float StartTime { get; set; }
            public float EndTime { get; set; }
            public float SourceStartTime { get; set; }
            public float SourceEndTime { get; set; }
        }

        private readonly Entry[] _entries;
        private readonly Guid _compositionSymbolId;

        public MoveTimeClipCommand(Guid compositionSymbolId, IReadOnlyList<TimeClip> clips)
        {
            _compositionSymbolId = compositionSymbolId;
            _entries = new Entry[clips.Count()];
            for (var i = 0; i < _entries.Length; i++)
            {
                var clip = clips[i];
                var entry = new Entry
                            {
                                Id = clip.Id,
                                 OriginalStartTime= clip.VisibleRange.Start,
                                 OriginalEndTime= clip.VisibleRange.End,
                                 OriginalSourceStartTime= clip.SourceRange.Start,
                                 OriginalSourceEndTime= clip.SourceRange.End,
                            };
                _entries[i] = entry;
            }
        }
        

        public void StoreCurrentValues()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var animator = compositionUi.Symbol.Animator;

            foreach (var clip in NodeOperations.GetAllTimeClips(compositionUi.Symbol))
            {
                var selectedEntry = _entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                selectedEntry.StartTime = clip.VisibleRange.Start;
                selectedEntry.EndTime = clip.VisibleRange.End;
                selectedEntry.SourceStartTime = clip.SourceRange.Start;
                selectedEntry.SourceEndTime = clip.SourceRange.End;
            }            
        }


        public void Undo()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var animator = compositionUi.Symbol.Animator;

            foreach (var clip in NodeOperations.GetAllTimeClips(compositionUi.Symbol))
            {
                var selectedEntry = _entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                clip.VisibleRange.Start = selectedEntry.OriginalStartTime;
                clip.VisibleRange.End = selectedEntry.OriginalEndTime;
                clip.SourceRange.Start = selectedEntry.OriginalSourceStartTime;
                clip.SourceRange.End = selectedEntry.OriginalSourceEndTime;
            }
        }

        public void Do()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var animator = compositionUi.Symbol.Animator;

            foreach (var clip in NodeOperations.GetAllTimeClips(compositionUi.Symbol))
            {
                var selectedEntry = _entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                clip.VisibleRange.Start = selectedEntry.StartTime;
                clip.VisibleRange.End = selectedEntry.EndTime;
                clip.SourceRange.Start = selectedEntry.SourceStartTime;
                clip.SourceRange.End = selectedEntry.SourceEndTime;
            }        
        }
    }
}