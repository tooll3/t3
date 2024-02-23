using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Graph
{
    public class ChangeInputValueCommand : ICommand
    {
        public string Name => "Change Input Value";
        public bool IsUndoable => true;

        public ChangeInputValueCommand(Symbol inputParentSymbol, Guid symbolChildId, SymbolChild.Input input, InputValue newValue)
        {
            _inputParentSymbolId = inputParentSymbol.Id;
            _childId = symbolChildId;
            _inputId = input.InputDefinition.Id;
            _isAnimated = inputParentSymbol.Animator.IsAnimated(_childId, _inputId);
            _wasDefault = input.IsDefault;
            _animationTime = Playback.Current.TimeInBars;

            OriginalValue = input.Value.Clone();
            _newValue = newValue == null ? input.Value.Clone() : newValue.Clone();

            //Log.Debug($"New command {OriginalValue} -> {newValue}");
            if (_isAnimated)
            {
                var animator = inputParentSymbol.Animator;
                _originalKeyframes = animator.GetTimeKeys(symbolChildId, _inputId, _animationTime).ToList();
            }
        }

        public void Undo()
        {
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            if (_isAnimated)
            {
                var wasNewKeyframe = false;
                foreach (var v in _originalKeyframes)
                {
                    if (v == null)
                    {
                        wasNewKeyframe = true;
                        break;
                    }
                }

                var animator = inputParentSymbol.Animator;
                if (wasNewKeyframe)
                {
                    //Log.Debug("  was new keyframe...");
                    animator.SetTimeKeys(_childId, _inputId,_animationTime, _originalKeyframes); // Remove keyframes
                    var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);
                    InvalidateInstances(inputParentSymbol, symbolChild);
                }
                else
                {
                    //Log.Debug("  restore original keyframes...");
                    animator.SetTimeKeys(_childId, _inputId,_animationTime, _originalKeyframes);
                    
                    var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);
                    InvalidateInstances(inputParentSymbol, symbolChild);
                }
            }
            else
            {
                if (_wasDefault)
                {
                    var symbolChild = inputParentSymbol.Children.SingleOrDefault(child => child.Id == _childId);
                    if (symbolChild == null)
                        return;
                    
                    var input = symbolChild.Inputs[_inputId];
                    input.ResetToDefault();
                    InvalidateInstances(inputParentSymbol, symbolChild);
                }
                else
                {
                    AssignValue(OriginalValue);
                }
            }
        }

        public void Do()
        {
            AssignValue(_newValue);
        }
        
        public void AssignNewValue(InputValue valueToSet)
        {
            _newValue.Assign(valueToSet);
            AssignValue(valueToSet);
        }

        private void AssignValue(InputValue valueToSet)
        {
            //Log.Debug($"assigning value {valueToSet}");
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            var symbolChild = inputParentSymbol.Children.SingleOrDefault(child => child.Id == _childId);
            if (symbolChild == null)
            {
                Log.Error($"Can't assign value to missing symbolChild {_childId}");
                return;
            }
            var input = symbolChild.Inputs[_inputId];
            
            if (_isAnimated)
            {
                var symbolUi = SymbolUiRegistry.Entries[symbolChild.Symbol.Id];
                var inputUi = symbolUi.InputUis[_inputId];
                var animator = inputParentSymbol.Animator;

                foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
                {
                    var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                    var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                    inputUi.ApplyValueToAnimation(inputSlot, valueToSet, animator, _animationTime);
                    inputSlot.DirtyFlag.ForceInvalidate();
                }
            }
            else
            {
                input.IsDefault = false;
                input.Value.Assign(valueToSet);
                InvalidateInstances(inputParentSymbol, symbolChild);
            }
        }

        private void InvalidateInstances(Symbol inputParentSymbol, SymbolChild symbolChild)
        {
            foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
            {
                var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                inputSlot.DirtyFlag.ForceInvalidate();
            }
        }

        private InputValue OriginalValue { get; set; }
        private readonly InputValue _newValue;
        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
        private readonly Guid _inputId;
        private readonly bool _wasDefault;
        private readonly bool _isAnimated;
        private readonly double _animationTime;
        private readonly List<VDefinition> _originalKeyframes;
    }
}