using System.Collections.Generic;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.Commands.Animation
{
    public class RemoveAnimationsCommand : ICommand
    {
        public string Name { get; set; }
        public bool IsUndoable => true;
        
        public RemoveAnimationsCommand(Animator animator, IInputSlot[] inputSlots)
        {
            _animator = animator;
            _inputSlots = inputSlots;
            if (inputSlots.Length == 1)
            {
                Name = $"Remove animation from {inputSlots[0].Parent.Symbol.Name}.{inputSlots[0].Input.Name}";
            }
            else
            {
                Name = $"Remove animation from {inputSlots.Length} parameters";
            }
        }
        
        public void Do()
        {
            foreach (var input in _inputSlots)
            {
                var curveSet = new List<Curve>();
                foreach (var curve in _animator.GetCurvesForInput(input))
                {
                    curveSet.Add(curve.TypedClone());
                }
                _curveSets.Add(curveSet);
                _animator.RemoveAnimationFrom(input);
                
            }
            _inputSlots[0].Parent.Parent.Symbol.CreateOrUpdateActionsForAnimatedChildren();
        }
        
        public void Undo()
        {
            for (var index = 0; index < _inputSlots.Length; index++)
            {
                var input = _inputSlots[index];
                var curves = _curveSets[index];
                _animator.AddCurvesToInput(curves, input);
            }
        }

        private readonly Animator _animator;
        private readonly IInputSlot[] _inputSlots;
        private readonly List<List<Curve>> _curveSets = new();
    }
}