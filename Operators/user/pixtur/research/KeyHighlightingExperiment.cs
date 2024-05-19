using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_14939a8d_8c42_4bd4_8271_8a083cfa4024
{
    public class KeyHighlightingExperiment : Instance<KeyHighlightingExperiment>
    {
        [Output(Guid = "02a88c21-0472-439a-8a1e-ec1aa027115a")]
        public readonly Slot<Texture2D> ColorBuffer = new();

        [Input(Guid = "ea3281aa-06f1-42c4-a832-2faffdf58e48")]
        public readonly InputSlot<int> SourceImage = new();


    }
}

