using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dc3d1571_ad9f_46aa_bed9_df2f4e1c7040
{
    public class CollectSpawnPoints : Instance<CollectSpawnPoints>
    {

        [Output(Guid = "fd2f84af-0925-418e-b3fa-edec6fa19df3")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "088f9a81-7170-4f9d-bbfa-f08b0bf32317")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "18903940-ff20-4b64-a4f0-6078977edd7a")]
        public readonly InputSlot<int> BufferSize = new InputSlot<int>();

        [Input(Guid = "5525b00a-eea5-46ed-b4b4-cbcadcee3820")]
        public readonly InputSlot<bool> CollectPoints = new InputSlot<bool>();

        [Input(Guid = "e801acdb-7750-4cd4-9366-ea2d17dbb649")]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "a03ffef9-11e3-41f9-9f13-71f107b484df")]
        public readonly InputSlot<float> GrowRate = new InputSlot<float>();

        [Input(Guid = "0f84199d-76f0-4155-b5b0-f6d05260423a")]
        public readonly InputSlot<float> MaxAge = new InputSlot<float>();

        [Input(Guid = "267b6cae-2c3d-4874-9532-ca3da138fde6")]
        public readonly InputSlot<bool> Reset = new InputSlot<bool>();

        [Input(Guid = "ae7aa205-faa0-454b-9a82-0067410275a0")]
        public readonly InputSlot<bool> Freeze = new InputSlot<bool>();
    }
}

