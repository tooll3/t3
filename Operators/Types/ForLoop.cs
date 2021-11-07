using System;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4a6fcc72_c703_4290_8e4a_2f90805cefa8
{
    public class ForLoop : Instance<ForLoop>
    {
        [Output(Guid = "c4ed1524-4175-42ce-8590-10ec63318c16")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public ForLoop()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            string variableName = Variable.GetValue(context);
            float end = End.GetValue(context);
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
        public readonly InputSlot<string> Variable = new InputSlot<string>();
        
        [Input(Guid = "726914f5-6ab8-4910-b225-323258aa9879")]
        public readonly InputSlot<float> End = new InputSlot<float>();
    }
}

