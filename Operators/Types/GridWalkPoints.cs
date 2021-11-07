using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_32d3b742_3ca8_406c_91e4_3067d6b4d5d6
{
    public class GridWalkPoints : Instance<GridWalkPoints>
    {

        [Output(Guid = "f1ff2b04-87a9-47c7-8cc8-5edd525a83a3")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "1ac63dff-a6ab-4dab-8c3d-fb1df0b65e0d")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "bae47166-dbed-4ebf-8139-f14e45d83f73")]
        public readonly InputSlot<float> Speed = new InputSlot<float>();

        [Input(Guid = "f93734ed-14dc-45ce-9043-5640ff750ce6")]
        public readonly InputSlot<float> SpeedVariation = new InputSlot<float>();

        [Input(Guid = "fbb42c5a-5626-4c0b-b4f0-774e7d1321b0")]
        public readonly InputSlot<System.Numerics.Vector3> GridSize = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "1f3d8cbe-2dc4-487a-9071-db428f5c8da8")]
        public readonly InputSlot<System.Numerics.Vector3> GridOffset = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "70ed99ef-7ffc-4dff-8622-d1cb5cd3d072")]
        public readonly InputSlot<bool> TriggerTurn = new InputSlot<bool>();
    }
}

