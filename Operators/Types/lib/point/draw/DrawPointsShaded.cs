using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;


namespace T3.Operators.Types.Id_84a93ea9_98fb_4088_a1e9_87454f7292f1
{
    public class DrawPointsShaded : Instance<DrawPointsShaded>
    {
        [Output(Guid = "037f6608-c1c8-417c-8a35-04e1357110fe")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "92e069cf-d136-4a13-8bcd-243731f77a54")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "f5b4e08b-a49a-47ac-838d-577b621005b2")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "68ebd395-5e00-4aeb-acfb-312899c4907c")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "9435c9b2-5237-4199-ac9c-93e9ddc5f8b3")]
        public readonly InputSlot<bool> EnableZWrite = new InputSlot<bool>();

        [Input(Guid = "2154f238-02d7-4635-aa26-17421352dca5")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "bb8e9be9-1371-45fd-be67-93d183b1355e", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "247d1fed-9897-486b-bcf0-39e6c98ccbd1")]
        public readonly InputSlot<float> FadeNearest = new InputSlot<float>();

        [Input(Guid = "b575d027-fd84-4a51-bf8a-6f3437e347f4")]
        public readonly MultiInputSlot<bool> UseWForSize = new MultiInputSlot<bool>();

        [Input(Guid = "ef476073-0796-4e6d-bf8b-d0ab92e92296")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Colors = new InputSlot<T3.Core.DataTypes.BufferWithViews>();
    }
}

