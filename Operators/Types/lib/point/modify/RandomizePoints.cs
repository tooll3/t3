using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ec0675d7_6b72_4b15_b141_80bdd2367cd8
{
    public class RandomizePoints : Instance<RandomizePoints>
    {

        [Output(Guid = "172dcbd2-a475-4514-8620-38f07a0ea4aa")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "cb157c8e-98f1-46e9-b197-d17dea896e30")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "82384eba-1d7a-41c8-a703-9184c961d766")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "270bcf23-35ee-4c4f-aae5-192435b1aee3")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "2cf68ba3-0665-43c8-88ab-64a9fb668ecc")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "e5b7c7b0-a207-44c2-b5d4-ef393c5dccb2")]
        public readonly InputSlot<float> W = new InputSlot<float>();

        [Input(Guid = "DD46595E-01E5-4616-9682-3A4EB7F63016")]
        public readonly InputSlot<System.Numerics.Vector4> ColorHSB = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "1E2C9B94-B303-4454-BA08-5246C7336660")]
        public readonly InputSlot<System.Numerics.Vector3> Extend = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "4dffb439-da81-477c-8100-34a9ba59b0ee")]
        public readonly InputSlot<float> RandomSeed = new InputSlot<float>();

        [Input(Guid = "5282AD12-AACF-4A62-8FDE-DF0148AB0F1F")]
        public readonly InputSlot<System.Numerics.Vector2> BiasAndGain = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "362FCD1A-444A-417B-B49F-EE1BABAEE998")]
        public readonly MultiInputSlot<float> UseSelection = new MultiInputSlot<float>();

        [Input(Guid = "9b4cc2f7-97f0-4b70-9773-d33ab4b893d1", MappedType = typeof(OffsetModes))]
        public readonly InputSlot<int> OffsetMode = new InputSlot<int>();

        [Input(Guid = "f06e85cc-a9b7-44c6-9f77-28c422db9f41", MappedType = typeof(Spaces))]
        public readonly InputSlot<int> Space = new InputSlot<int>();

        [Input(Guid = "0ee3b20d-b11b-4009-b1e4-ba35c1050252", MappedType = typeof(Interpolations))]
        public readonly InputSlot<int> Interpolation = new InputSlot<int>();

        [Input(Guid = "e86074d8-f77f-4716-9eb9-bb3948c21e68")]
        public readonly InputSlot<int> Repeat = new InputSlot<int>();

        [Input(Guid = "002e8e48-2eb3-4450-aa37-e539f1157600")]
        public readonly InputSlot<bool> ClampColorsEtc = new InputSlot<bool>();


        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
        }

        private enum OffsetModes
        {
            Add,
            Scatter,
        }
        
        private enum Interpolations
        {
            None,
            Linear,
            Smooth,
        }
    }
}

