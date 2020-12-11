using System;
using System.Collections.Generic;
using T3.Gui.Interaction.PresetSystem.InputCommands;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    /// <summary>
    /// Creates matching commands if input signals are matching. 
    /// </summary>
    /// <remarks>
    /// The requirements turned out to be quite complicated. This sketch might give an overview:
    /// - https://www.figma.com/file/lLlkKILVbLuv1mxuTq6jMb/Interaction?node-id=823%3A57
    ///
    ///  
    /// </remarks>
    public class CommandTriggerCombination
    {
        public CommandTriggerCombination(Type commandType, AbstractMidiDevice.InputModes requiredInputMode, ButtonRange[] keyRanges, ExecutesAt executesAt)
        {
            _keyRanges = keyRanges;
            _commandType = commandType;
            _requiredInputMode = requiredInputMode;
            _executesAt = executesAt;
        }

        public InputCommand GetMatchingCommand(List<ButtonSignal> buttonSignals, AbstractMidiDevice.InputModes activeMode,
                                               AbstractMidiDevice.InputModes releasedMode)
        {
            UpdateMatchingRangeIndices(buttonSignals);
            
            if (_executesAt == ExecutesAt.ModeButtonReleased)
            {
                if (releasedMode != _requiredInputMode)
                    return null;
                
                if(_activatedIndices.Count > 0)
                    return Activator.CreateInstance(_commandType, _activatedIndices.ToArray()) as ButtonsPressCommand;
                
                return null;
            }

            if (_requiredInputMode != AbstractMidiDevice.InputModes.None && activeMode != _requiredInputMode)
                return null;

            
            if (_executesAt == ExecutesAt.SingleRangeButtonPressed)
            {
                if (_releasedIndices.Count == 0 && _justPressedIndices.Count > 0)
                {
                    return Activator.CreateInstance(_commandType, _justPressedIndices.ToArray()) as ButtonsPressCommand;
                }

                return null;
            }

            if (_executesAt == ExecutesAt.AllCombinedButtonsReleased)
            {
                if (_releasedIndices.Count > 0 && _justPressedIndices.Count == 0 && _holdIndices.Count == 0)
                {
                    return Activator.CreateInstance(_commandType, _releasedIndices.ToArray()) as ButtonsPressCommand;
                }

                // var matchedSignalCount = 0;
                // _matchingMappedIndices.Clear();
                // foreach (var requiredControlRanges in _keyRanges)
                // {
                //     var foundMatchingSignal = false;
                //     foreach (var givenSignal in buttonSignals)
                //     {
                //         if (!requiredControlRanges.IncludesButtonIndex(givenSignal.ButtonId))
                //             continue;
                //
                //         matchedSignalCount++;
                //         foundMatchingSignal = true;
                //         if (requiredControlRanges.IsRange)
                //         {
                //             var mappedIndex = requiredControlRanges.GetMappedIndex(givenSignal.ButtonId);
                //             _matchingMappedIndices.Add(mappedIndex);
                //         }
                //     }
                //
                //     if (!foundMatchingSignal)
                //         return null;
                // }
            }
            

            // All button pressed must be account for...
            // if (matchedSignalCount != buttonSignals.Count)
            //     return null;

            //var indices = _matchingMappedIndices.ToArray();
            //var command = Activator.CreateInstance(_commandType, indices) as ButtonsPressCommand;
            return null;
        }

        private void UpdateMatchingRangeIndices(List<ButtonSignal> buttonSignals)
        {
            //_matchingMappedIndices.Clear();
            _releasedIndices.Clear();
            _justPressedIndices.Clear();
            _holdIndices.Clear();
            _activatedIndices.Clear();

            if (buttonSignals.Count == 0)
                return;

            foreach (var range in _keyRanges)
            {
                if (!range.IsRange)
                    continue;

                foreach (var s in buttonSignals)
                {
                    if (range.IncludesButtonIndex(s.ButtonId))
                    {
                        var mappedIndex = range.GetMappedIndex(s.ButtonId);

                        switch (s.State)
                        {
                            case ButtonSignal.States.JustPressed:
                                _justPressedIndices.Add(mappedIndex);
                                break;
                            case ButtonSignal.States.Released:
                                _releasedIndices.Add(mappedIndex);
                                break;
                            case ButtonSignal.States.Hold:
                                _holdIndices.Add(mappedIndex);
                                break;
                        }
                        _activatedIndices.Add(mappedIndex);
                    }
                }
            }
        }
        
        public enum ExecutesAt
        {
            SingleRangeButtonPressed,
            AllCombinedButtonsReleased,
            ModeButtonReleased,
        }

        // ReSharper disable InconsistentNaming
        private static readonly List<int> _activatedIndices = new List<int>(10);
        private static readonly List<int> _releasedIndices = new List<int>(10);
        private static readonly List<int> _justPressedIndices = new List<int>(10);
        private static readonly List<int> _holdIndices = new List<int>(10);

        private readonly ButtonRange[] _keyRanges;
        private readonly Type _commandType;
        private readonly AbstractMidiDevice.InputModes _requiredInputMode;
        private readonly ExecutesAt _executesAt;
    }
}