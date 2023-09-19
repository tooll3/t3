using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7a08d73e_1aea_479f_8d36_ecb119d75c3a
{
    public class DirectionalForce : Instance<DirectionalForce>
    {
        [Output(Guid = "de0ec6bf-6c23-46d2-9d8b-6f314037b2c8")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> Particles = new Slot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "1840e5b8-2aee-44d0-b826-d34395325506")]
        public readonly InputSlot<System.Numerics.Vector3> Direction = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "4f9ab443-3885-4a57-9116-6be5824bd95b")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "2fe652d7-92c7-4cd0-8190-78905be178f1")]
        public readonly InputSlot<float> RandomAmount = new InputSlot<float>();

        [Input(Guid = "1f561bbd-a272-4c06-bd56-a580a2022bc6", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "a6b0a4da-2f6f-4941-b097-ed5e3dd6af0b")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new InputSlot<T3.Core.Operator.GizmoVisibility>();

        [Input(Guid = "82746dde-ab65-4c49-a22f-cf9a50f4a3e9")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();
        
        
        private enum Modes {
            Legacy,
            EncodeInRotation,
        }
    }
}

