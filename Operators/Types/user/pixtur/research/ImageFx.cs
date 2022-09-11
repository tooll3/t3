using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dc066031_ccf6_4a23_8412_a266e5f2cb16
{
    public class ImageFx : Instance<ImageFx>
    {
        [Output(Guid = "7d5358d4-f713-44ff-b1a6-8c169a7b6dec")]
        public readonly Slot<T3.Core.Command> Output = new Slot<T3.Core.Command>();


        [Input(Guid = "72de7eea-2c6c-41a8-a499-188ad20d80ba")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "a8cc6506-e44e-411f-bd72-69969f8ecd8d")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "dae9f4a2-acc6-4273-8724-e803c974544c")]
        public readonly InputSlot<float> Height = new InputSlot<float>();

        [Input(Guid = "899bf2f3-3cc8-4980-91a2-2267fdde18e6")]
        public readonly InputSlot<string> ShaderPath = new InputSlot<string>();

        [Input(Guid = "710e09fc-7592-4573-a781-8abf01666488")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();
    }
}

