using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c6fce5e7_391e_4d08_bdc2_4c4960a2296e
{
    public class CurlNoiseWithGizmo : Instance<CurlNoiseWithGizmo>
    {
        [Output(Guid = "A2FC8C88-2DD2-4099-A76E-B252032F41CC")]
        public readonly Slot<Command> Command = new Slot<Command>();

        [Input(Guid = "53a2c70b-2523-4f9f-abf8-2e5b16ddfe00")]
        public readonly MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView> ShaderResources = new MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView>();

        [Input(Guid = "39261664-c9e9-40ca-85d0-726dc8801bf2")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "9cbd5aed-e175-4001-aadc-19817cd25c33")]
        public readonly InputSlot<int> FilterEmitter = new InputSlot<int>();

        [Input(Guid = "ec44a127-17fe-46f8-ba07-b07c4f18c5b2")]
        public readonly InputSlot<float> Frequency = new InputSlot<float>();

        [Input(Guid = "13d679fa-cd25-44d5-b617-c9911f73c276")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "d277f1ca-2892-45e5-b154-11077df1af15")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "60549314-65ab-4bfd-a12e-0818d22a43f7")]
        public readonly InputSlot<float> ParticleFriction = new InputSlot<float>();

        [Input(Guid = "45d61d8d-5441-4633-9f97-d57f8e583e08")]
        public readonly InputSlot<float> Variation = new InputSlot<float>();
    }
}