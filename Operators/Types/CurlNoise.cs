using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0e553db8_9621_45e2_b871_736b7630bf1f
{
    public class CurlNoise : Instance<CurlNoise>
    {
        [Output(Guid = "a507e4cd-1b47-4cf5-924c-29ec6374f34e")]
        public readonly Slot<T3.Core.Command> Command = new Slot<T3.Core.Command>();


        [Input(Guid = "d8f26ecf-f7eb-44c9-9911-bc177c771c0e")]
        public readonly InputSlot<string> ShaderFilename = new InputSlot<string>();

        [Input(Guid = "18d7ff8e-4118-4e65-91eb-917d085f5384")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "ffe9dbb2-97ef-46e9-b365-490341624daf")]
        public readonly MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView> ShaderResources = new MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView>();

        [Input(Guid = "49dfc07f-6ad0-46a4-aa2a-a9fe547baf60")]
        public readonly InputSlot<float> Frequency = new InputSlot<float>();

        [Input(Guid = "4a4191b6-63f3-4f59-ad63-b83bd581800a")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "2849511f-dc52-4a95-924d-1eb2e6c53f7e")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "eb15afd2-a3f8-427c-b4cd-e2a803e93f5c")]
        public readonly InputSlot<float> ParticleFriction = new InputSlot<float>();
    }
}

