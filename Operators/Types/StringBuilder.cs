using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fcdc3089_2df5_467b_841b_7745efaf13db 
{
    public class StringBuilder : Instance<StringBuilder>
    {
        [Output(Guid = "DEB52613-01FC-428D-B2D1-4BE7B6767FAD")]
        public readonly Slot<System.Text.StringBuilder> Builder = new Slot<System.Text.StringBuilder>();

        
        public StringBuilder()
        {
            _builder = new System.Text.StringBuilder();
            Builder.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Builder.Value = _builder;
            if (ClearTrigger.GetValue(context))
            {
                _builder.Clear();
            }
        }

        private System.Text.StringBuilder _builder;

        
        [Input(Guid = "F5C0B04E-8C4E-43BA-AE19-79F80C0F830D")]
        public readonly InputSlot<bool> ClearTrigger = new InputSlot<bool>();
    }
}
