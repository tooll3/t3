using System.Collections.Generic;
using Editor.Gui.Interaction.LegacyVariations.Midi;

namespace T3.Editor.Gui.Interaction.LegacyVariations.Midi.CommandProcessing
{
    /// <summary>
    /// Describes either a single or range of buttons on a midi device  
    /// </summary>
    public readonly struct ButtonRange
    {
        public ButtonRange(int index)
        {
            _index = index;
            _lastIndex = index;
        }

        public ButtonRange(int index, int lastIndex)
        {
            _index = index;
            _lastIndex = lastIndex;
        }

        public bool IncludesButtonIndex(int index)
        {
            return index >= _index && index <= _lastIndex;
        }

        public int GetMappedIndex(int buttonIndex)
        {
            return buttonIndex - _index;
        }

        public IEnumerable<int> Indices()
        {
            for (int index = _index; index <= _lastIndex; index++)
            {
                yield return index;
            }
        }

        public IEnumerable<int> GetMatchingIndicesFromSignals(List<ButtonSignal> signals)
        {
            if(signals.Count == 0)
                return new List<int>();

            var matches = new List<int>();
            foreach (var s in signals)
            {
                if(IncludesButtonIndex(s.ButtonId))
                    matches.Add(GetMappedIndex(s.ButtonId));
            }

            return matches;
        }



        public bool IsRange => _lastIndex > _index;

        private readonly int _index;
        private readonly int _lastIndex;
    }
}