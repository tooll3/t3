using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_191e5057_4da4_447e_b7cf_e9e0ed8c5dd8
{
    public class SetPointsW : Instance<SetPointsW>
    {

        [Output(Guid = "39c62e1e-7c63-4a88-9923-3f7f5fffbfbf")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "d5a5862b-d3a2-4e6e-ad54-32cff7ced0fd")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "840aa616-6983-4840-a58b-d5396a91c2f9", MappedType = typeof(MappingModes))]
        public readonly InputSlot<int> Mapping = new InputSlot<int>();

        [Input(Guid = "ba00fa7a-fcda-48da-a4c7-f2fe97997e50", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "38046ede-e786-4cb1-ac17-de6cb7b91c32")]
        public readonly InputSlot<float> Range = new InputSlot<float>();

        [Input(Guid = "82a1a932-f5c3-41d5-9539-9b21663aee1b")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "27bf737b-966e-4203-b8fd-2d9c7b19dcad")]
        public readonly InputSlot<T3.Core.Animation.Curve> WCurve = new InputSlot<T3.Core.Animation.Curve>();

        [Input(Guid = "d504c3f9-290f-4a73-bf9d-f9266ea955f6")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();


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

