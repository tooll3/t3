using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_98d6089f_50f9_4b4c_ba86_18d19ae9dd17
{
    public class ParticleDraw : Instance<ParticleDraw>
    {
        [Output(Guid = "519c6615-7814-42e9-aa7d-5158bc02bb1e", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "ed0c2c1a-2a20-4d49-a812-80f8d19e447b")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "CD19CC42-4033-4B43-A73C-3E4E50D3095E")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "8D418AD8-8D65-491A-9CF3-14DD046A3A53")]
        public readonly InputSlot<float> Size = new InputSlot<float>();
    }
}

