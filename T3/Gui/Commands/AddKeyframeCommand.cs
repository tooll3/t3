using System.Collections.Generic;
using T3.Core.Animation;

namespace T3.Gui.Commands
{
    public class AddKeyframesCommand : ICommand
    {
        public string Name => "Add keyframe";
        public bool IsUndoable => true;
        
        private Curve _curve;
        private VDefinition _originalKey;
        private VDefinition _newKey;
        
        private readonly Dictionary<VDefinition, VDefinition> _originalDefForReferences = new Dictionary<VDefinition, VDefinition>();
        private readonly Dictionary<VDefinition, VDefinition> _newDefForReferences = new Dictionary<VDefinition, VDefinition>();

        public AddKeyframesCommand(Curve curve, VDefinition newKey)
        {
            _curve = curve;
            _originalKey = curve.GetV(newKey.U);
            _newKey = newKey;
        }
        
        public void Undo()
        {
            if (_originalKey != null)
            {
                _curve.AddOrUpdateV(_originalKey.U, _originalKey);                
            }
            else
            {
                _curve.RemoveKeyframeAt(_newKey.U);
            }
            _curve.UpdateTangents();
        }

        public void Do()
        {
            _curve.AddOrUpdateV(_newKey.U, _newKey);
            _curve.UpdateTangents();
        }
    }
}