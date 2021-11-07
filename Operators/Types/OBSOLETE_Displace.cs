using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_26a34630_ad46_4bcc_8ff8_ed37fe021f6c
{
    public class OBSOLETE_Displace : Instance<OBSOLETE_Displace>
    {
        [Output(Guid = "0fd9e62e-e9f8-441d-95e4-84a77e368d84")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        [Input(Guid = "08cc84b0-9ede-49d1-bf3f-52b229b7ec55")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "a9ef5673-9a48-459d-b08c-b03f3c62cc6f")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DisplaceMap = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "3aefdcee-7d34-41eb-b999-9bae7b792941")]
        public readonly InputSlot<float> DisplaceAmount = new InputSlot<float>();

        [Input(Guid = "fbf48b39-135b-4e5d-bd90-5671d4070d52")]
        public readonly InputSlot<float> SampleRadius = new InputSlot<float>();

        [Input(Guid = "eee699c6-51e9-45e9-a1fb-0a7aea68130a")]
        public readonly InputSlot<float> Angle = new InputSlot<float>();

        [Input(Guid = "ca123b90-e609-4df4-a866-421c272c83fd")]
        public readonly InputSlot<float> DisplaceOffset = new InputSlot<float>();

        [Input(Guid = "e13050ab-fb47-4ee1-bded-76aacebc203f")]
        public readonly InputSlot<float> SampleCount = new InputSlot<float>();

        [Input(Guid = "e4df2330-d25f-4bd8-8ba2-104408316985")]
        public readonly InputSlot<float> ShiftX = new InputSlot<float>();

        [Input(Guid = "e859e00f-1afa-4925-8d7b-dd376f74a546")]
        public readonly InputSlot<float> ShiftY = new InputSlot<float>();
    }
}