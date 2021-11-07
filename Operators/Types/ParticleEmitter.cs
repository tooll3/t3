using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_119f710b_e6be_4eef_af63_13cfc6ba86fd
{
    public class ParticleEmitter : Instance<ParticleEmitter>
    {
        [Output(Guid = "aa24848d-4ec6-4f1a-9a5a-c64587ee6f75")]
        public readonly Slot<T3.Core.Command> Command = new Slot<T3.Core.Command>();


        [Input(Guid = "b33b6b8b-37cd-475a-b67d-ac672478f439")]
        public readonly InputSlot<string> ShaderFilename = new InputSlot<string>();

        [Input(Guid = "7f5e613e-03f0-48a0-9844-54e78868f7e3")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "88ad8d03-0281-417f-8830-69ccad5345ad")]
        public readonly MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView> ShaderResources = new MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView>();

        [Input(Guid = "1468320d-e1f9-4bb9-bc8e-f7cfbc7a4ea6")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "64af7117-3423-4c1e-9856-0b06777985a3")]
        public readonly InputSlot<System.Numerics.Vector4> ColorScatter = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "488e4032-0261-41d0-966f-225cf00c7653")]
        public readonly InputSlot<float> LifeTime = new InputSlot<float>();

        [Input(Guid = "43d0b219-bc30-4e46-acb1-18848d8b04a9")]
        public readonly InputSlot<float> ScatterPosition = new InputSlot<float>();
    }
}

