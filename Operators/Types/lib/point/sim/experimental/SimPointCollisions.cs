using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ed0bc47a_31ef_400b_b4e4_5552a859b309
{
    public class SimPointCollisions : Instance<SimPointCollisions>
    {

        [Output(Guid = "8c9461b8-9152-4389-938e-2ff67a6451ed")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> Particles = new Slot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "0724bf0c-8f97-44de-bf42-6a89b89f1632")]
        public readonly InputSlot<float> CellSize = new InputSlot<float>();

        [Input(Guid = "d0036a34-d7e4-4542-b231-5d1b687b0028")]
        public readonly InputSlot<float> Bounciness = new InputSlot<float>();

        [Input(Guid = "91966b2c-342a-420a-bad1-cbcf4d6af1ae")]
        public readonly InputSlot<float> Attraction = new InputSlot<float>();

        [Input(Guid = "78566405-8dee-4661-9cb7-489c8d322f64")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

        [Input(Guid = "5b3fcf3c-7155-401f-b3ac-3a4ac9d921df")]
        public readonly InputSlot<float> AttractionDecay = new InputSlot<float>();
    }
}

