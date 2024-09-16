using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib._3d.gizmo
{
	[Guid("296dddbd-250b-4801-a039-bcb3cd555774")]
    public class DrawLineGrid : Instance<DrawLineGrid>
    {
        [Output(Guid = "d542b5bf-5e9b-4beb-8cbf-f2fff294423f")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "5767a3be-9ac1-4ad5-9529-aea00d3143ea")]
        public readonly InputSlot<float> UniformScale = new();

        [Input(Guid = "0480e529-b790-4c6f-a993-2efdbfda35e4")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "a4ea3140-3397-4989-98ee-3cf02d11f242")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "0b8c7835-078c-4990-8db5-edccd26018c9")]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "5e7da35a-6537-4fc1-9f23-faab82c8eeaa")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> Segments = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

    }
}

