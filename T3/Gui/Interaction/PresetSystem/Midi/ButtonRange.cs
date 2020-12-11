using System.Collections.Generic;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
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

        public bool IsRange => _lastIndex > _index;

        private readonly int _index;
        private readonly int _lastIndex;
    }
}