using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3849f1f8_7d5e_4c15_9e25_6761817b5038
{
    public class __SurfaceForce : Instance<__SurfaceForce>
    {

        [Output(Guid = "b20c80ff-2cde-437c-b6a8-4da751432fdb")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "263fd506-1528-403d-991e-84aaf4c9d938")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "6b98e7d2-2d8a-48eb-bea7-c5e72289f41a")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "f0401f2d-d672-48c0-ba82-289f2f27114b")]
        public readonly InputSlot<float> MaxAcceleration = new InputSlot<float>();

        [Input(Guid = "a541039a-f23d-49c7-8158-81efc0be1533")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "cefd81ce-efcd-4269-a8a4-508343024bf4")]
        public readonly InputSlot<float> DecayExponent = new InputSlot<float>();

        [Input(Guid = "68105a09-628f-4639-ac1e-41fe0ce983a7")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new InputSlot<T3.Core.Operator.GizmoVisibility>();

        [Input(Guid = "525ce1b8-f1da-420b-8398-d81838649642")]
        public readonly InputSlot<int> VolumeType = new InputSlot<int>();

        [Input(Guid = "382e13a6-079e-40d9-a5f5-07bbf67785c9")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();
        
        private enum Modes {
            Legacy,
            EncodeInRotation,
        }
    }
}

