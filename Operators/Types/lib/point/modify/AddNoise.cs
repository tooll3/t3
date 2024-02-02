using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dd586355_64b3_4e96_af6d_b4927595dee7
{
    public class AddNoise : Instance<AddNoise>
    {

        [Output(Guid = "bea6aa18-e751-4ce7-b7d7-b7a026c8e019")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "3f5abde2-66e1-4b04-9bff-5a19a58aab86")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "5894156a-cc31-4236-908c-de0e5385fd84")]
        public readonly InputSlot<float> Amount = new();

        [Input(Guid = "929db7b2-f19c-4a28-b4c2-187365b99760")]
        public readonly InputSlot<float> Frequency = new();

        [Input(Guid = "aaba1602-e7a1-4b48-81d4-9d7b2b3aa8b1")]
        public readonly InputSlot<float> Phase = new();

        [Input(Guid = "1dfb45ae-b376-41ea-a1d2-97b170645b50")]
        public readonly InputSlot<float> Variation = new();

        [Input(Guid = "c2df1fa3-88e1-4be2-954e-8c44edd9d421")]
        public readonly InputSlot<System.Numerics.Vector3> AmountDistribution = new();

        [Input(Guid = "97c25ec6-ef71-42f8-9352-52baf2ce41a4")]
        public readonly InputSlot<float> RotationLookupDistance = new();

        [Input(Guid = "6c2ab161-da81-47c2-8008-222cf994179c")]
        public readonly InputSlot<float> UseWAsWeight = new();

        [Input(Guid = "2d229dc7-a7ec-4fae-b84c-5eea6b91e18a")]
        public readonly InputSlot<System.Numerics.Vector3> NoiseOffset = new();

        [Input(Guid = "30ceafce-455a-449c-9ffd-928280d7a862")]
        public readonly InputSlot<System.Numerics.Vector2> BiasAndGain = new InputSlot<System.Numerics.Vector2>();
    }
}

