using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.examples
{
	[Guid("1a8acd4a-e6a9-45ad-b5be-d564e0fd3f18")]
    public class VennDiagram2 : Instance<VennDiagram2>
    {
        [Output(Guid = "9834a3a3-fba0-475e-9f0f-4014f90fccf6")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

