using System.Collections.Generic;

namespace T3.Gui.Interaction.Variations.Midi
{
    /// <summary>
    /// Describes either a single or range of buttons on a midi device  
    /// </summary>
    public readonly struct ButtonRange
    {
        public ButtonRange(int startIndex)
        {
            _startIndex = startIndex;
            _lastStartIndex = startIndex;
            _reversed = true;
        }

        public ButtonRange(int startIndex, int lastStartIndex, bool reversed = false)
        {
            _startIndex = startIndex;
            _lastStartIndex = lastStartIndex;
            _reversed = reversed;
        }

        public bool IncludesButtonIndex(int index)
        {
            return index >= _startIndex && index <= _lastStartIndex;
        }

        public int GetMappedIndex(int buttonIndex)
        {
            return _reversed 
                       ? _lastStartIndex - (buttonIndex - _startIndex)
                        :buttonIndex - _startIndex;
        }

        public IEnumerable<int> Indices()
        {
            for (var index = _startIndex; index <= _lastStartIndex; index++)
            {
                yield return _reversed 
                                 ? (_lastStartIndex-_startIndex) 
                                 :   index;
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



        public bool IsRange => _lastStartIndex > _startIndex;

        private readonly int _startIndex;
        private readonly int _lastStartIndex;
        private readonly bool _reversed;
    }
}