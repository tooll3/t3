using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_bb617a86_d771_480a_be94_c9cc460252a5
{
    public class __ImageQuadEmitterOBSOLETE : Instance<__ImageQuadEmitterOBSOLETE>
    {
        [Output(Guid = "23c5d230-4f31-44f8-9b65-5c8cf111bd45")]
        public readonly Slot<T3.Core.Command> Command = new Slot<T3.Core.Command>();

        [Input(Guid = "49a41a2a-39a4-46fd-ab70-5c8442a6fd61")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "d133b57d-4e2c-432a-8172-599e7f70e79e")]
        public readonly MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView> ShaderResources = new MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView>();

        [Input(Guid = "98ae0757-7e78-40e1-9e9d-b807540dc4d4")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "59213885-ebbb-41cf-a6bc-b0a4b657d22b")]
        public readonly InputSlot<float> EmitPosY = new InputSlot<float>();

        [Input(Guid = "f5fa3f1e-33c3-4df3-96d2-981f166772af")]
        public readonly InputSlot<float> EmitPosYScatter = new InputSlot<float>();

        [Input(Guid = "892c9fc8-817d-49d5-97d2-cb290c609aeb")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "241961f6-27f2-4d1d-85ee-51a819c3ae21")]
        public readonly InputSlot<float> Mass = new InputSlot<float>();

        [Input(Guid = "6ef0891c-b6cd-4eec-a570-11c38e889ad5")]
        public readonly InputSlot<float> LifeTime = new InputSlot<float>();
    }
}

