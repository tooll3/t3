using System;
using System.Collections.Generic;

namespace T3.Editor.Gui.Interaction.Midi.CommandProcessing;

/// <summary>
/// Defines and invokes matching commands if input signals are matching. 
/// </summary>
/// <remarks>
/// The requirements turned out to be quite complicated. This sketch might give an overview:
/// - https://www.figma.com/file/lLlkKILVbLuv1mxuTq6jMb/Interaction?node-id=823%3A57
///
/// The general idea of the <see cref="CommandProcessing"/> namespace is to abstract as
/// much functionally as possible so new midi-devices can be quickly added without implementing
/// too much logic. Ideally 
/// 
/// </remarks>
public class CommandTriggerCombination
{
    public CommandTriggerCombination(Action<int> indexAction, AbstractMidiDevice.InputModes requiredInputMode, ButtonRange[] keyRanges,
                                     ExecutesAt executesAt)
    {
        _keyRanges = keyRanges;
        _requiredInputMode = requiredInputMode;
        _executesAt = executesAt;
        _indexAction = indexAction;
    }

    public CommandTriggerCombination(Action<int[]> action, AbstractMidiDevice.InputModes requiredInputMode, ButtonRange[] keyRanges, ExecutesAt executesAt)
    {
        _keyRanges = keyRanges;
        _requiredInputMode = requiredInputMode;
        _executesAt = executesAt;
        _indicesAction = action;
    }

    public CommandTriggerCombination(Action<int, float> action, AbstractMidiDevice.InputModes requiredInputMode, ButtonRange[] keyRanges,
                                     ExecutesAt executesAt)
    {
        _keyRanges = keyRanges;
        _requiredInputMode = requiredInputMode;
        _executesAt = executesAt;
        _controllerValueUpdateAction = action;
    }

    public CommandTriggerCombination(Action action, AbstractMidiDevice.InputModes requiredInputMode, ButtonRange[] keyRanges, ExecutesAt executesAt)
    {
        _keyRanges = keyRanges;
        _requiredInputMode = requiredInputMode;
        _executesAt = executesAt;
        _actionWithoutParameters = action;
    }
        
    public void InvokeMatchingButtonCommands(List<ButtonSignal> buttonSignals, AbstractMidiDevice.InputModes activeMode,
                                             AbstractMidiDevice.InputModes releasedMode)
    {
        UpdateMatchingRangeIndices(buttonSignals);

        if (_executesAt == ExecutesAt.ModeButtonReleased)
        {
            if (releasedMode != _requiredInputMode)
                return;

            if (_activatedIndices.Count <= 0)
                return;
                
            _indexAction?.Invoke(_activatedIndices[0]);
            _indicesAction?.Invoke(_activatedIndices.ToArray());
            return;
        }

        if (_requiredInputMode != AbstractMidiDevice.InputModes.None && activeMode != _requiredInputMode)
            return;

        switch (_executesAt)
        {
            case ExecutesAt.SingleRangeButtonPressed:
            {
                if (_holdIndices.Count != 0 || _justPressedIndices.Count <= 0)
                    return;
                    
                _indexAction?.Invoke(_justPressedIndices[0]);
                _indicesAction?.Invoke(_activatedIndices.ToArray());
                return;
            }

            case ExecutesAt.SingleActionButtonPressed:
            {
                if (buttonSignals.Count == 1
                    && _keyRanges[0].IncludesButtonIndex(buttonSignals[0].ButtonId)
                    && buttonSignals[0].State == ButtonSignal.States.JustPressed
                   )
                {
                    _actionWithoutParameters?.Invoke();
                }

                return;
            }
                
            case ExecutesAt.AllCombinedButtonsReleased:
            {
                if (_releasedIndices.Count > 1 && _justPressedIndices.Count == 0 && _holdIndices.Count == 0)
                {
                    _indicesAction?.Invoke(_activatedIndices.ToArray());
                }

                break;
            }
        }
    }

    public void InvokeMatchingControlCommands(IEnumerable<ControlChangeSignal> controlSignals, AbstractMidiDevice.InputModes activeMode)
    {
        if (_requiredInputMode != AbstractMidiDevice.InputModes.None && activeMode != _requiredInputMode)
            return;

        if (_executesAt != ExecutesAt.ControllerChange)
            return;

        foreach (var signal in controlSignals)
        {
            foreach (var range in _keyRanges)
            {
                if (!range.IncludesButtonIndex(signal.ControllerId))
                    continue;


                var mappedIndex = range.GetMappedIndex(signal.ControllerId);
                _controllerValueUpdateAction.Invoke(mappedIndex, signal.ControllerValue);
            }

            // if (_holdIndices.Count == 0 && _justPressedIndices.Count > 0)
            // {
            //     _indexAction?.Invoke(_justPressedIndices[0]);
            //     _indicesAction?.Invoke(_activatedIndices.ToArray());
            // }
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
                if (!range.IncludesButtonIndex(s.ButtonId))
                    continue;
                    
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

    public enum ExecutesAt
    {
        SingleRangeButtonPressed,
        AllCombinedButtonsReleased,
        ModeButtonReleased,
        SingleActionButtonPressed,
        ControllerChange
    }

    // ReSharper disable InconsistentNaming
    private static readonly List<int> _activatedIndices = new(10);
    private static readonly List<int> _releasedIndices = new(10);
    private static readonly List<int> _justPressedIndices = new(10);
    private static readonly List<int> _holdIndices = new(10);

    private readonly ButtonRange[] _keyRanges;
    private readonly AbstractMidiDevice.InputModes _requiredInputMode;
    private readonly ExecutesAt _executesAt;
    private readonly Action _actionWithoutParameters;
    private readonly Action<int> _indexAction;
    private readonly Action<int[]> _indicesAction;
    private readonly Action<int, float> _controllerValueUpdateAction;

}