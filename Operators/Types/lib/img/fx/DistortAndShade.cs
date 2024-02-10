using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_8bede700_4e3e_42d8_8097_9744abdb8ad3
{
    public class DistortAndShade : Instance<DistortAndShade>
    {
        [Output(Guid = "5a639fac-b8e1-495b-a82f-e4877133b06f")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Input(Guid = "824e1ad4-0d33-458f-aefe-f3780ab06529")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageA = new();

        [Input(Guid = "2596e8fb-00aa-4704-9978-a880c1016c18")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageB = new();

        [Input(Guid = "17c45c4e-590e-4de6-90c2-befe2f89831d")]
        public readonly InputSlot<System.Numerics.Vector4> ShadeColor = new();

        [Input(Guid = "84cea304-79d9-4f8c-a171-584897b9b468")]
        public readonly InputSlot<float> Displacement = new();

        [Input(Guid = "f942fc0d-ed5f-4594-8690-d464c5b12ed8")]
        public readonly InputSlot<float> Shading = new();

        [Input(Guid = "3a3acfbd-dca7-4f8a-b862-90eae2bc41ca")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new();

    }
}

