using System;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_296dddbd_250b_4801_a039_bcb3cd555774
{
    public class DrawLineGrid : Instance<DrawLineGrid>
    {
        [Output(Guid = "d542b5bf-5e9b-4beb-8cbf-f2fff294423f")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "5767a3be-9ac1-4ad5-9529-aea00d3143ea")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();

    }
}

