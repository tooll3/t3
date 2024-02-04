using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.examples
{
	[Guid("446d1664-5fcf-4792-874c-926450a900d7")]
    public class SamplePointAttributesExample : Instance<SamplePointAttributesExample>
    {
        [Output(Guid = "346e5f1b-d880-4d5c-99b9-e7c528c0a2cb")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

