using System;
using SharpDX;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e64f95e4_c045_400f_98ca_7c020ad46174
{
    public class Switch : Instance<Switch>
    {
        [Output(Guid = "9300b07e-977d-47b0-908e-c4b1e5e53a64", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        public Switch()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var commands = Commands.GetCollectedTypedInputs();
            var index = Index.GetValue(context);

            if (commands.Count == 0 || index == -1)
            {
                return;
            }
            
            // Do all
            if (index == -2)
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    commands[i].GetValue(context); 
                }

                for (int i = 0; i < commands.Count; i++)
                {
                    commands[i].Value?.RestoreAction?.Invoke(context);
                }
                
                return;
            }
            
            index %= commands.Count;
            if (index < 0)
            {
                index += commands.Count;
            }
                
            commands[index].GetValue(context); 
            commands[index].Value?.RestoreAction?.Invoke(context);
        }

        [Input(Guid = "988DD1B5-636D-4A78-9592-2C6601401CC1")]
        public readonly MultiInputSlot<Command> Commands = new MultiInputSlot<Command>();
        
        [Input(Guid = "00FD2794-567A-4F9B-A900-C2EBF9760764")]
        public readonly InputSlot<int> Index = new InputSlot<int>();
    }
}