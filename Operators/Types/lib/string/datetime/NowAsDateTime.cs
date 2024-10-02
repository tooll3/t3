using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_bd8d684c_96ae_4864_84fd_ca87f98ce1a4
{
    public class NowAsDateTime : Instance<NowAsDateTime>
    {
        [Output(Guid = "99f94d1c-7d79-497d-9d42-dff8b749e493")]
        public readonly Slot<DateTime> Output = new();
        

        public NowAsDateTime()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Output.Value = DateTime.Now;
        }
    }
}