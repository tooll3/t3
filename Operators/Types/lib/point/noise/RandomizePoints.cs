using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_15f056a3_ee8b_41a2_92c9_eb85153f8200
{
    public class RandomizePoints : Instance<RandomizePoints>
    {

        [Output(Guid = "92864bf1-5cc9-4e42-a136-e4f79282297a")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "06332b65-36c9-4dc8-a3db-64b2ee116148")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "5fc7dd7c-6298-4f7e-9e4e-cec3506c19aa")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "72308bda-fe03-429e-8f42-a975b11ca8a4")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "dedd7211-21c2-4fa0-88d3-14e7a062e7ab")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "3d045aa7-9f4f-4a42-8f12-a9dc30c30410")]
        public readonly InputSlot<float> RandomizeW = new InputSlot<float>();

        [Input(Guid = "8f8c14b3-e87e-4295-a591-9fa7ebcce8f3", MappedType = typeof(Spaces))]
        public readonly InputSlot<int> Space = new InputSlot<int>();

        [Input(Guid = "1503C058-E270-40CD-99E6-40325B9233A7")]
        public readonly InputSlot<int> Seed = new InputSlot<int>();

        [Input(Guid = "07c73fd8-6126-4e03-8f10-05d28a3c7ef4")]
        public readonly InputSlot<float> Gain = new InputSlot<float>();

        [Input(Guid = "36e69f1c-412b-4a5c-8b17-d0f5d909f1cb")]
        public readonly InputSlot<float> Offset = new InputSlot<float>();


        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
            WorldSpace,
        }
    }
}

