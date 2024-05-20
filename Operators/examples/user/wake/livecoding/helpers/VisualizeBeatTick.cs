using System.Runtime.InteropServices;
using System;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.wake.livecoding.helpers
{
    [Guid("beefc0da-79f1-4dd2-8002-668099818d5d")]
    public class VisualizeBeatTick : Instance<VisualizeBeatTick>
    {
        [Output(Guid = "aeb2a7d3-bca9-40a6-a2b6-0653aa99be24")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "c1267d60-7758-41e0-9daa-cbe221735b40")]
        public readonly InputSlot<float> TimeInBars = new InputSlot<float>();

    }
}

