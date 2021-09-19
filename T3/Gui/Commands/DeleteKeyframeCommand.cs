using System.Collections.Generic;
using T3.Core.Animation;

namespace T3.Gui.Commands
{
    public class DeleteKeyframesCommand : ICommand
    {
        public string Name => "Add keyframe";
        public bool IsUndoable => true;
        
        private Curve _curve;
        private VDefinition _originalKey;
        
        private readonly Dictionary<VDefinition, VDefinition> _originalDefForReferences = new Dictionary<VDefinition, VDefinition>();

        public DeleteKeyframesCommand(Curve curve, VDefinition originalKey)
        {
            _curve = curve;
            _originalKey = originalKey;
        }
        
        public void Undo()
        {
            _curve.AddOrUpdateV(_originalKey.U, _originalKey);                
            _curve.UpdateTangents();
        }

        public void Do()
        {
            _curve.RemoveKeyframeAt(_originalKey.U);
            _curve.UpdateTangents();
        }
    }
}