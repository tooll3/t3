using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib._3d.gizmo
{
    [Guid("523bf3f7-d8f6-459b-a7fb-f681dce9e697")]
    public class LocatorExample : Instance<LocatorExample>
    {
        [Output(Guid = "2bfc15d6-7ef1-443b-b1e4-2a55bb2a12ec")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

