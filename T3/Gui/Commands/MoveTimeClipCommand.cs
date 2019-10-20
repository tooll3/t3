using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;
using T3.Gui.Selection;

namespace T3.Gui.Commands
{
    public class MoveTimeClipCommand : ICommand
    {
        public string Name => "Move Time Clip";
        public bool IsUndoable => true;

        public class Entry
        {
            public Guid Id;
            public double OriginalStartTime { get; set; }
            public double OriginalEndTime { get; set; }
            public double OriginalSourceStartTime { get; set; }
            public double OriginalSourceEndTime { get; set; }
            public double StartTime { get; set; }
            public double EndTime { get; set; }
            public double SourceStartTime { get; set; }
            public double SourceEndTime { get; set; }
        }

        public readonly Entry[] Entries;
        private readonly Guid _compositionSymbolId;

        public MoveTimeClipCommand(Guid compositionSymbolId, List<Animator.Clip> clips)
        {
            _compositionSymbolId = compositionSymbolId;
            Entries = new Entry[clips.Count()];
            for (var i = 0; i < Entries.Length; i++)
            {
                var clip = clips[i];
                var entry = new Entry
                            {
                                Id = clip.Id,
                                 OriginalStartTime= clip.StartTime,
                                 OriginalEndTime= clip.EndTime,
                                 OriginalSourceStartTime= clip.SourceStartTime,
                                 OriginalSourceEndTime= clip.SourceEndTime,
                            };
                Entries[i] = entry;
            }
        }
        

        public void StoreCurrentValues()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var animator = compositionUi.Symbol.Animator;

            foreach (var clip in animator.GetAllTimeClips())
            {
                var selectedEntry = Entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                selectedEntry.StartTime = clip.StartTime;
                selectedEntry.EndTime = clip.EndTime;
                selectedEntry.SourceStartTime = clip.SourceStartTime;
                selectedEntry.SourceEndTime = clip.SourceEndTime;
            }            
        }


        public void Undo()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var animator = compositionUi.Symbol.Animator;

            foreach (var clip in animator.GetAllTimeClips())
            {
                var selectedEntry = Entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                clip.StartTime = selectedEntry.OriginalStartTime;
                clip.EndTime = selectedEntry.OriginalEndTime;
                clip.SourceStartTime = selectedEntry.OriginalSourceStartTime;
                clip.SourceEndTime = selectedEntry.OriginalSourceEndTime;
            }
        }

        public void Do()
        {
            var compositionUi = SymbolUiRegistry.Entries[_compositionSymbolId];
            var animator = compositionUi.Symbol.Animator;

            foreach (var clip in animator.GetAllTimeClips())
            {
                var selectedEntry = Entries.SingleOrDefault(entry => entry.Id == clip.Id);
                if (selectedEntry == null)
                    continue;

                clip.StartTime = selectedEntry.StartTime;
                clip.EndTime = selectedEntry.EndTime;
                clip.SourceStartTime = selectedEntry.SourceStartTime;
                clip.SourceEndTime = selectedEntry.SourceEndTime;
            }        
        }
    }
}