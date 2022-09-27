using T3.Core.DataTypes;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_804cd9ce_7e1b_4156_a372_268a99650a57
{
    public class LifetimeUpdate : Instance<LifetimeUpdate>
    {
        [Output(Guid = "f6f929b9-b3c4-4891-a910-b58d78c05708")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "f2030b01-03cb-44a3-b303-29380290b996")]
        public readonly InputSlot<ParticleSystem> ParticleSystem = new InputSlot<ParticleSystem>();

    }
}

