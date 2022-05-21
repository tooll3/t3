using System;
using System.Diagnostics;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f5158500_39e4_481e_aa4f_f7dbe8cbe0fa
{
    public class SetBpm : Instance<SetBpm>
    {
        [Output(Guid = "05C17586-CF93-4244-9979-47E310ABAF31")]
        public readonly Slot<Command> Commands = new();
        
        // [Output(Guid = "ed186e34-7644-4e46-8dd1-6d550733e3c3")]
        // public readonly Slot<float> Delta = new();
        
        // [Output(Guid = "2a289ad7-153b-44fc-92b3-e800d7ca8005")]
        // public readonly Slot<float> LastDuration = new();
        
        public SetBpm()
        {
            Commands.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (Playback.Current == null)
            {
                Log.Warning("Can't set BPM-Rate without active Playback");
                return;
            }

            var bpm = BpmRate.GetValue(context);
            
            var clamped = bpm.Clamp(54, 240);
            if (bpm > 0 && Math.Abs(clamped - Playback.Current.Bpm) > 0.1f)
            {
                Log.Debug($"Setting BPM rate to {clamped}");
                Playback.Current.Bpm = clamped;
                return;
            }
            
            SubGraph.GetValue(context);
        }

        
        [Input(Guid = "721C34B5-BB06-49E0-A71E-2AEBBF2557E0")]
        public readonly InputSlot<float> BpmRate = new();
        
                
        [Input(Guid = "9CC32DA8-F939-4AD3-B381-6DF8338A371B")]
        public readonly InputSlot<Command> SubGraph = new();
    }
}