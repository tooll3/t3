using System;
using System.Collections.Generic;
using T3.Gui.Graph;
//using T3.Gui.Interaction.PresetSystem.InputCommands;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    /// <summary>
    /// Creates matching commands if input signals are matching. 
    /// </summary>
    /// <remarks>
    /// The requirements turned out to be quite complicated. This sketch might give an overview:
    /// - https://www.figma.com/file/lLlkKILVbLuv1mxuTq6jMb/Interaction?node-id=823%3A57
    ///  
    /// </remarks>
    public class CommandTriggerCombination
    {
        public CommandTriggerCombination(Action<int> singleIndexAction, AbstractMidiDevice.InputModes requiredInputMode, ButtonRange[] keyRanges, ExecutesAt executesAt)
        {
            _keyRanges = keyRanges;
            _requiredInputMode = requiredInputMode;
            _executesAt = executesAt;
            _singleIndexAction = singleIndexAction;
        }

        
        
        public void InvokeMatchingCommands(List<ButtonSignal> buttonSignals, AbstractMidiDevice.InputModes activeMode,
                                               AbstractMidiDevice.InputModes releasedMode)
        {
            UpdateMatchingRangeIndices(buttonSignals);
            
            if (_executesAt == ExecutesAt.ModeButtonReleased)
            {
                if (releasedMode != _requiredInputMode)
                    return;

                if (_activatedIndices.Count > 0)
                {
                    _singleIndexAction?.Invoke(_activatedIndices[0]);
                }
                
                return;
            }

            if (_requiredInputMode != AbstractMidiDevice.InputModes.None && activeMode != _requiredInputMode)
                return;

            
            if (_executesAt == ExecutesAt.SingleRangeButtonPressed)
            {
                if (_holdIndices.Count == 0 && _justPressedIndices.Count > 0)
                {
                    _singleIndexAction?.Invoke(_justPressedIndices[0]);
                }

                return;
            }

            if (_executesAt == ExecutesAt.AllCombinedButtonsReleased)
            {
                if (_releasedIndices.Count > 0 && _justPressedIndices.Count == 0 && _holdIndices.Count == 0)
                {
                    _singleIndexAction?.Invoke(_releasedIndices[0]);
                }
            }
        }

        private void UpdateMatchingRangeIndices(List<ButtonSignal> buttonSignals)
        {
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
        private readonly AbstractMidiDevice.InputModes _requiredInputMode;
        private readonly ExecutesAt _executesAt;        
        private readonly Action<int> _singleIndexAction;

    }
}