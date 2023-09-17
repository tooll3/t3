using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_93f47afb_edf3_4ca3_a163_913278e7bead
{
    public class DirectionalForce2 : Instance<DirectionalForce2>
    {

        [Output(Guid = "cb6368db-95f5-41b9-b162-2741d7e8ed7b")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "53bef7a0-45bf-4abb-95d0-185beda78c38")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "b5f98295-7b3c-472c-8447-74ba9ca1e9c8")]
        public readonly InputSlot<System.Numerics.Vector3> Direction = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "24aaed70-77eb-41b6-8081-cc98a94f1f80")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "6a780f4d-8053-4472-b479-95b9594654ad")]
        public readonly InputSlot<float> RandomAmount = new InputSlot<float>();

        [Input(Guid = "0719ff34-6f6d-4d11-a37e-3e4a2bc575a3")]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "396d9465-599f-41a7-8503-46def8b99ed2")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new InputSlot<T3.Core.Operator.GizmoVisibility>();
        
        
        private enum Modes {
            Legacy,
            EncodeInRotation,
        }
    }
}

