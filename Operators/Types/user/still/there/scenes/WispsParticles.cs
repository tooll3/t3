using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f56a981c_2080_4617_b9f1_d7a625a44b57
{
    public class WispsParticles : Instance<WispsParticles>
    {

        [Output(Guid = "5eaa9c88-6e90-4583-a7bb-457ff99f0a1b")]
        public readonly TimeClipSlot<Command> Output2 = new();


    }
}

