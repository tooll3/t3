using T3.Core.Animation;
using T3.Core.DataTypes;

namespace T3.Editor.UiModel.Commands.Animation;

public sealed class DeleteKeyframeCommand : ICommand
{
    public string Name => "Delete keyframe";
    public bool IsUndoable => true;
        
    public DeleteKeyframeCommand(Curve curve, VDefinition keyframe)
    {
        _curve = curve;
        _keyframe = keyframe;
    }
    public void Do()
    {
        _curve.RemoveKeyframeAt(_keyframe.U);
        _curve.UpdateTangents();
    }
        
    public void Undo()
    {
        _curve.AddOrUpdateV(_keyframe.U, _keyframe);                
        _curve.UpdateTangents();
    }

    private readonly Curve _curve;
    private readonly VDefinition _keyframe;
}