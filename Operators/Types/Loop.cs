using System;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3631c727_36a0_4f26_ae76_ee9c100efc33
{
    public class Loop : Instance<Loop>
    {
        [Output(Guid = "5685cbc4-fe19-4f0e-95a3-147d1fbbad15")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public Loop()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var variableName = VariableName.GetValue(context);
            var normalizedVariableName = VariableName.GetValue(context) + "_normalized";
            var end = Count.GetValue(context);
            for (var i = 0; i < end; i ++)
            {
                context.FloatVariables[variableName] = i;
                if (end == 1)
                {
                    context.FloatVariables[normalizedVariableName] = 0;
                }
                else
                {
                    context.FloatVariables[normalizedVariableName] = i / ((float)(end - 1));
                }

                DirtyFlag.InvalidationRefFrame++;
                Command.Invalidate();
                Command.GetValue(context);
            }
        }
        
        [Input(Guid = "49552a0c-2060-4f03-ad39-388293bb6871")]
        public readonly InputSlot<Command> Command = new();

        [Input(Guid = "F9AEBE04-DD82-459F-8175-7139C7B2E468")]
        public readonly InputSlot<string> VariableName = new();


        [Input(Guid = "1F6E2ADB-CFF8-4DC4-9CB4-A26E3AD8B087")]
        public readonly InputSlot<int> Count = new();
    }
}

