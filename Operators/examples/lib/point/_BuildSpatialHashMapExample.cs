using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.point
{
	[Guid("69776b58-e05d-47c8-b19f-2f44f7b9c915")]
    public class _BuildSpatialHashMapExample : Instance<_BuildSpatialHashMapExample>
    {
        [Output(Guid = "0af851d9-0753-472b-8448-77562b33cf07")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

