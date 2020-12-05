using System;
using System.Collections.Generic;
using T3.Gui.Interaction.PresetSystem.Midi;

namespace T3.Gui.Interaction.PresetSystem.InputCommands
{
    public readonly struct ControllerRange
    {
        public ControllerRange(int index)
        {
            _index = index;
            _lastIndex = index;
        }

        public ControllerRange(int index, int lastIndex)
        {
            _index = index;
            _lastIndex = lastIndex;
        }

        public bool IncludesIndex(int index)
        {
            return index >= _index && index <= _lastIndex;
        }

        public bool IsRange => _lastIndex > _index;

        private readonly int _index;
        private readonly int _lastIndex;
    }

    public class ButtonSignal
    {
        public int ControllerId;
        public float PressTime;
        public float ControllerValue;
        public bool IsPressed;
        public int PressCount;
        public int Channel;
    }
    
    public class CommandTriggerCombination
    {
        public CommandTriggerCombination(ControllerRange[] keyRanges, Type commandType, MidiDevice midiDevice)
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
                    if (!requiredControlRanges.IncludesIndex(givenSignal.ControllerId))
                        continue;

                    matchedSignalCount++;
                    foundMatchingSignal = true;
                    if (requiredControlRanges.IsRange)
                        MatchingRangeIndices.Add(givenSignal.ControllerId);
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

        private readonly ControllerRange[] _keyRanges;
        private readonly Type _commandType;
        private static readonly List<int> MatchingRangeIndices = new List<int>(10);
        private readonly MidiDevice _midiDevice;
    }
}