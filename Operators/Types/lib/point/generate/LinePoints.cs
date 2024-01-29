using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4ae9e2f5_7cb3_40b0_a662_0662e8cb7c68
{
    public class LinePoints : Instance<LinePoints>
    {

        [Output(Guid = "68514ced-4368-459a-80e9-463a808bff0b")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        [Input(Guid = "951a1792-e607-4595-b211-97be7d27694c")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "178c1df1-5326-42d1-9251-8e2c8ad7965b")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "8f439130-529c-42ff-a5c0-255476120f03")]
        public readonly InputSlot<System.Numerics.Vector3> Direction = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "6fa2fddb-3b8d-4fda-9ac4-796618aa88d0")]
        public readonly InputSlot<float> LengthFactor = new InputSlot<float>();

        [Input(Guid = "af75835a-04c9-4721-8c7a-a31ef8012f06")]
        public readonly InputSlot<float> Pivot = new InputSlot<float>();

        [Input(Guid = "28081e5f-da01-46dc-81ad-699df29a49a4")]
        public readonly InputSlot<System.Numerics.Vector2> GainBias = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "d120d8f7-aff6-4e30-b0d2-c45e3e477fde")]
        public readonly InputSlot<float> W = new InputSlot<float>();

        [Input(Guid = "41ed1339-c762-4979-9c89-b1a347eb3d06")]
        public readonly InputSlot<float> WOffset = new InputSlot<float>();

        [Input(Guid = "6f46bd61-422f-4715-9219-3d2e1dff1d90")]
        public readonly InputSlot<System.Numerics.Vector4> ColorA = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "4d45a633-ac00-4cbe-83a3-43c419c3da97")]
        public readonly InputSlot<System.Numerics.Vector4> ColorB = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "83986e05-af3e-469f-a656-9956d37d12ba", MappedType = typeof(OrientationModes))]
        public readonly InputSlot<int> Orientation = new InputSlot<int>();

        [Input(Guid = "a8dfe0e7-ad33-47cf-ab78-726385e38434")]
        public readonly InputSlot<System.Numerics.Vector3> OrientationAxis = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "208b724d-c5d7-4eaa-94a1-e1f045f14969")]
        public readonly InputSlot<float> OrientationAngle = new InputSlot<float>();

        [Input(Guid = "8f7206d1-5f78-4a9c-bba9-1ef8277b6d5f")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "ddc2ea6a-d356-46c9-b333-4cce69c02570")]
        public readonly InputSlot<bool> AddSeparator = new InputSlot<bool>();


        private enum OrientationModes
        {
            UsingUpVector,
            Simple,
        }
    }
}

