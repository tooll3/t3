using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Operator;

namespace T3.Gui.Commands
{
    public class ChangeKeyframesCommand : ICommand
    {
        public string Name => "Move keyframes";
        public bool IsUndoable => true;

        private readonly Guid _compositionSymbolId;

        public ChangeKeyframesCommand(Guid compositionSymbolId, IEnumerable<VDefinition> vDefinitions)
        {
            _compositionSymbolId = compositionSymbolId;
            // TODO: Implement
        }


        public void StoreCurrentValues()
        {
            // TODO: Implement
        }


        public void Undo()
        {
            // TODO: Implement
        }

        public void Do()
        {
            // TODO: Implement
        }
    }
}