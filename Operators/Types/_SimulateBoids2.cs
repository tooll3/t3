using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9c3f142f_76d2_4395_9796_3857413084e2
{
    public class _SimulateBoids2 : Instance<_SimulateBoids2>
    {
        [Output(Guid = "92e9be89-cdc8-40e0-9041-308bd6fbcefb")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();

        [Input(Guid = "56860a7b-c935-4d21-a511-2d8bc9f67480")]
        public readonly InputSlot<int> ComputeSteps = new InputSlot<int>();

        [Input(Guid = "ff106c07-ee17-4788-98c7-401c875e8f7d")]
        public readonly InputSlot<int> AgentCount = new InputSlot<int>();

        [Input(Guid = "ae1b8419-5d79-4085-aa6e-3f7f9e8094f5")]
        public readonly InputSlot<bool> RestoreLayoutEnabled = new InputSlot<bool>();

        [Input(Guid = "9ef3b1d4-f91f-49fa-b50c-d8acbda9cad1")]
        public readonly InputSlot<float> RestoreLayout = new InputSlot<float>();

        [Input(Guid = "75ee8f92-f110-40d0-9bdc-ccf9413dc266")]
        public readonly InputSlot<float> EffectLayer = new InputSlot<float>();

        [Input(Guid = "a2ea7dcb-f30d-4f13-8e7d-2faa38518525")]
        public readonly InputSlot<float> EffectTwist = new InputSlot<float>();

        [Input(Guid = "fce39d1d-74cf-4769-8a85-3ba5192ac26c")]
        public readonly InputSlot<float> CellSize = new InputSlot<float>();

        [Input(Guid = "5e8c41a5-5389-4ab7-9060-3b0a6ca98267")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> EffectTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "8a70798c-6448-44ef-a0cb-c9bf22bcc3e1")]
        public readonly InputSlot<T3.Core.DataTypes.StructuredList> BoidDefinitions = new InputSlot<T3.Core.DataTypes.StructuredList>();


    }
}

