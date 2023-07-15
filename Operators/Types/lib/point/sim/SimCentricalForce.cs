using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_87915d7b_f2aa_45da_80f9_bd1f6033d387
{
    public class SimCentricalForce : Instance<SimCentricalForce>
    {

        [Output(Guid = "22ac99e2-182d-4a14-b64d-2a27f39be88b")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "9180eb49-efae-4305-b269-04314210e1f2")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "129f21fb-3206-4b0a-b455-485f1c11bd83")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "cbcfcceb-309b-4a25-856a-cb4eb3798cf4")]
        public readonly InputSlot<float> MaxAcceleration = new InputSlot<float>();

        [Input(Guid = "02f34a62-bd83-47ac-962a-f1af6a92f0b8")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "77aa9e9a-b73c-4cf4-8b00-c5a9860cdeed")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

        [Input(Guid = "ac4bf799-f2e8-4bb8-ab57-e1260ba0421e")]
        public readonly InputSlot<float> ApplyMovement = new InputSlot<float>();

        [Input(Guid = "c399f493-9df4-4fb8-9840-a749c3fe33a6", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();
        
        private enum Modes {
            Legacy,
            EncodeInRotation,
        }
    }
}

