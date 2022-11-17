using System;
using System.Diagnostics;
using T3.Core;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_c6d22dc3_a6ff_4a6f_aa14_8be6595da2b1
{
    public class SetPlaybackTime : Instance<SetPlaybackTime>
    {
        [Output(Guid = "0bd5fcb8-72fa-40d5-a922-63a2e7551a88")]
        public readonly Slot<Command> Commands = new();
        
        public SetPlaybackTime()
        {
            Commands.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {

            
            var newTime = TimeInBars.GetValue(context);
            var wasTriggered = MathUtils.WasTriggered(SetTrigger.GetValue(context), ref _setTrigger);
            
            if (float.IsNaN(newTime) || float.IsInfinity(newTime))
            {
                newTime = 0;
            }

            if (wasTriggered)
            {
                if (Playback.Current == null)
                {
                    Log.Warning("Can't set playback time without active Playback");
                    return;
                }
                
                Log.Debug($"Setting playback time to {newTime}");
                Playback.Current.TimeInBars = newTime;
            }
            
            SubGraph.GetValue(context);
        }

        private bool _setTrigger;
        
        [Input(Guid = "68c3bd6d-aebf-4a0c-bb73-04c9997ebae9")]
        public readonly InputSlot<Command> SubGraph = new();
        
        [Input(Guid = "24cf475d-a5b7-46b5-92cf-1ffcefb0693e")]
        public readonly InputSlot<float> TimeInBars = new();
        
        [Input(Guid = "BD88E545-743E-4CAA-8FD1-E1F7F3C78B21")]
        public readonly InputSlot<bool> SetTrigger = new();
    }
}