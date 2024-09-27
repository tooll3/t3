using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Editor.Gui.Commands.Animation;

public class AddAnimationCommand : ICommand
{
    public string Name { get; set; }
    public bool IsUndoable => true;
        
    public AddAnimationCommand(Animator animator, IInputSlot inputSlot)
    {
        _animator = animator;
        _inputSlot = inputSlot;
        Name = $"Add animation to {inputSlot.Parent.Symbol.Name}.{inputSlot.Input.Name}";
    }
        
    public void Do()
    {
        _wasDefault = _inputSlot.Input.IsDefault; 
        _keepCurves = _animator.AddOrRestoreCurvesToInput(_inputSlot, _keepCurves); 
        _inputSlot.Parent.Parent.Symbol.CreateOrUpdateActionsForAnimatedChildren();
    }
        
    public void Undo()
    {
        _animator.RemoveAnimationFrom(_inputSlot);
        if (_wasDefault)
            _inputSlot.Input.IsDefault = true;
            
        _inputSlot.Parent.Parent.Symbol.CreateOrUpdateActionsForAnimatedChildren();
    }

    private readonly Animator _animator;
    private readonly IInputSlot _inputSlot;
    private bool _wasDefault;
    private Curve[] _keepCurves;
}