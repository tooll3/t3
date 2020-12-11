using System;
using System.Collections.Generic;
using T3.Gui.Interaction.PresetSystem.InputCommands;
using T3.Gui.Interaction.PresetSystem.Midi;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
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