using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3f8376f2_b89a_4ab4_b6dc_a3e8bf88c0a5
{
    public class TurbulenceForce : Instance<TurbulenceForce>
    {

        [Output(Guid = "6fbbeeb4-f9dc-4202-a2c6-2d688b81dff2")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "f92939bc-e1f6-4c0e-bdd6-935b97231bb9")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "e27a97ce-3d0f-41a9-93c3-a1691f4029aa")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "f0345217-29f4-48f8-babd-8aed134cb0d5")]
        public readonly InputSlot<float> Frequency = new InputSlot<float>();

        [Input(Guid = "419b5ec5-8f6d-4c2d-a633-37d125cfcf07")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "56144ddb-9d4b-4e08-9169-7853a767f794")]
        public readonly InputSlot<float> Variation = new InputSlot<float>();

        [Input(Guid = "dfa6e67f-140b-4f96-bfb7-a8897edce28f")]
        public readonly InputSlot<System.Numerics.Vector3> AmountDistribution = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "ebf8276f-2df8-4e70-ba57-30288fb184d1")]
        public readonly InputSlot<bool> UseCurlNoise = new InputSlot<bool>();

        [Input(Guid = "d1ebfcaa-ce47-4064-9169-7afa64f942f5")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new InputSlot<T3.Core.Operator.GizmoVisibility>();
    }
}

