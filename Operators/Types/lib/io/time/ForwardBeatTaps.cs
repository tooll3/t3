using System;
using System.Collections.Generic;
using System.Diagnostics;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_79db48d8_38d3_47ca_9c9b_85dde2fa660d
{
    public class ForwardBeatTaps : Instance<ForwardBeatTaps>
    {
        [Output(Guid = "71d05d91-d18b-44b3-a469-392739fd6941")]
        public readonly Slot<Command> Result = new Slot<Command>();

        public ForwardBeatTaps()
        {
            Result.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            
            BeatTapTriggered = false;
            var triggerTap = TriggerBeatTap.GetValue(context);
            if (triggerTap != _wasBeatTriggered)
            {
                _wasBeatTriggered = triggerTap;
                BeatTapTriggered = triggerTap;
            }
            
            ResyncTriggered = false;
            var triggerResync = TriggerResync.GetValue(context);
            if (triggerResync != _wasResyncTriggered)
            {
                _wasResyncTriggered = triggerResync;
                ResyncTriggered = triggerResync;
            }
             
            // Evaluate subtree
            SubTree.GetValue(context);
        }

        private bool _wasBeatTriggered;
        private bool _wasResyncTriggered;

        public static bool BeatTapTriggered;
        public static bool ResyncTriggered;
        
        [Input(Guid = "89576f05-3f3d-48d1-ab63-f3c16c85db63")]
        public readonly InputSlot<Command> SubTree = new InputSlot<Command>();
        
        [Input(Guid = "37DA48AC-A7C5-47C8-9FB3-82D4403B2BA0")]
        public readonly InputSlot<bool> TriggerBeatTap = new();

        [Input(Guid = "58B6DF86-B02E-4183-9B63-1033C9DFF25F")]
        public readonly InputSlot<bool> TriggerResync = new();
        

    }
}