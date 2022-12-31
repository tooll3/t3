using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_99435f7a_4969_4a0e_83f5_404ee6a0cfa2
{
    public class Warp2dMesh : Instance<Warp2dMesh>
    {

        [Output(Guid = "ee5ce4f1-a518-4545-b2ce-64005fade7a8")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new Slot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "8cf1ef77-4b59-412b-992b-74f88bf857f0")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "0fc8009e-8555-475e-a87c-5af5093a1cb9")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "9d00b18c-46f4-446c-8953-2972d90d9685")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> TargetPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        
        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
            WorldSpace,
        }
        
        private enum Directions
        {
            WorldSpace,
            SurfaceNormal,
        }
    }
}

