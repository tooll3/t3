using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;

namespace T3.Gui.Commands
{
    public class ChangeKeyframesCommand : ICommand
    {
        public string Name => "Move keyframes";
        public bool IsUndoable => true;

        private readonly Guid _compositionSymbolId;

        private readonly Dictionary<VDefinition, VDefinition> _originalDefForReferences = new Dictionary<VDefinition, VDefinition>();
        private readonly Dictionary<VDefinition, VDefinition> _newDefForReferences = new Dictionary<VDefinition, VDefinition>();

        public ChangeKeyframesCommand(Guid compositionSymbolId, IEnumerable<VDefinition> vDefinitions)
        {
            _compositionSymbolId = compositionSymbolId;
            foreach (var def in vDefinitions)
            {
                _originalDefForReferences[def] = def.Clone();
            }
        }

        public void StoreCurrentValues()
        {
            foreach (var referencedDefinition in _originalDefForReferences.Keys)
            {
                _newDefForReferences[referencedDefinition] = referencedDefinition.Clone();
            }
        }

        public void Undo()
        {
            foreach (var (referencedDefinition, orgDef) in _originalDefForReferences)
            {
                referencedDefinition.CopyValuesFrom(orgDef);
            }
        }

        public void Do()
        {
            foreach (var (referencedDefinition, newDef) in _newDefForReferences)
            {
                referencedDefinition.CopyValuesFrom(newDef);
            }
        }
    }
}