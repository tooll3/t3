using System;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4a6fcc72_c703_4290_8e4a_2f90805cefa8
{
    public class __ObsoleteLoop : Instance<__ObsoleteLoop>
    {
        [Output(Guid = "c4ed1524-4175-42ce-8590-10ec63318c16")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public __ObsoleteLoop()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            string variableName = VariableName.GetValue(context);
            float end = Count.GetValue(context);
            for (float i = 0f; i < end; i += 1.0f)
            {
                context.FloatVariables[variableName] = i;

                DirtyFlag.InvalidationRefFrame++;
                Command.Invalidate();
                Command.GetValue(context);
            }
        }
        
        [Input(Guid = "51d969ee-c373-4bfd-9290-b60070b8872c")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "3d488da4-a984-4588-af58-146bf32cbc88")]
        public readonly InputSlot<string> VariableName = new InputSlot<string>();

        [Input(Guid = "CF64344B-0D61-4969-A600-6C7B4F9B43FE")]
        public readonly InputSlot<float> Start = new InputSlot<float>();

        [Input(Guid = "726914f5-6ab8-4910-b225-323258aa9879")]
        public readonly InputSlot<float> Count = new InputSlot<float>();
    }
}

