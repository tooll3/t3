using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_86b61bcf_4eaa_4f77_a535_8a1dc876aada
{
    public class SetPointAttributes : Instance<SetPointAttributes>
    {

        [Output(Guid = "9bc53d1e-64bf-4373-9367-66ffa41447bd")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "29f16973-2bd7-4655-b32f-1a5b932010a1")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "cc54c0ab-28c1-4333-a016-1147b5aa44fb")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "b4c4414d-e24d-4456-8c7d-00eb9de89de9")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();


        private enum MappingModes
        {
            Normal,
            ForStart,
            PingPong,
            Repeat,
            UseOriginalW,
        }
        
        private enum Modes
        {
            Replace,
            Multiply,
            Add,
        }
    }
}

