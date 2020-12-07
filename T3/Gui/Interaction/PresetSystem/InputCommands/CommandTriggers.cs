using System;
using System.Collections.Generic;
using T3.Gui.Interaction.PresetSystem.Midi;

namespace T3.Gui.Interaction.PresetSystem.InputCommands
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

    public class ButtonSignal
    {
        public int ButtonIndex;
        public float PressTime;
        public float ControllerValue;
        public bool IsPressed;
        public int PressCount;
        public int Channel;
    }
    
    public class CommandTriggerCombination
    {
        public CommandTriggerCombination(ButtonRange[] keyRanges, Type commandType, AbstractMidiDevice midiDevice)
        {
            _keyRanges = keyRanges;
            _commandType = commandType;
            _midiDevice = midiDevice;
        }

        public InputCommand MatchesCommandPresses(List<ButtonSignal> buttonSignals)
        {
            var matchedSignalCount = 0;
            MatchingRangeIndices.Clear();

            foreach (var requiredControlRanges in _keyRanges)
            {
                var foundMatchingSignal = false;
                foreach (var givenSignal in buttonSignals)
                {
                    if (!requiredControlRanges.IncludesButtonIndex(givenSignal.ButtonIndex))
                        continue;

                    matchedSignalCount++;
                    foundMatchingSignal = true;
                    if (requiredControlRanges.IsRange)
                    {
                        var mappedIndex = requiredControlRanges.GetMappedIndex(givenSignal.ButtonIndex);
                        MatchingRangeIndices.Add(mappedIndex);
                    }
                }

                if (!foundMatchingSignal)
                    return null;
            }

            // All button pressed must be account for...
            if (matchedSignalCount != buttonSignals.Count)
                return null;

            var indices = MatchingRangeIndices.ToArray();
            var command = Activator.CreateInstance(_commandType, indices) as ButtonsPressCommand;
            return command;
        }

        private readonly ButtonRange[] _keyRanges;
        private readonly Type _commandType;
        private static readonly List<int> MatchingRangeIndices = new List<int>(10);
        private readonly AbstractMidiDevice _midiDevice;
    }
}