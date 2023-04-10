using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.Commands.Animation
{
    public class AddAnimationCommand : ICommand
    {
        public string Name => "Add Animation for parameter";
        public bool IsUndoable => true;
        
        // private readonly Curve _curve;
        // private readonly VDefinition _originalKey;
        // private readonly VDefinition _newKey;

        private Animator _animator;
        private IInputSlot _inputSlot;
        private bool _wasDefault;

        public AddAnimationCommand(Animator animator, IInputSlot inputSlot)
        {
            _animator = animator;
            _inputSlot = inputSlot;
        }
        
        public void Do()
        {
            _wasDefault = _inputSlot.Input.IsDefault; 
            _animator.CreateInputUpdateAction(_inputSlot); // todo: create command
            _inputSlot.Parent.Parent.Symbol.CreateOrUpdateActionsForAnimatedChildren();
        }
        
        public void Undo()
        {
            _animator.RemoveAnimationFrom(_inputSlot);
            if (_wasDefault)
                _inputSlot.Input.IsDefault = true;
            
            _inputSlot.Parent.Parent.Symbol.CreateOrUpdateActionsForAnimatedChildren();
        }

    }
}