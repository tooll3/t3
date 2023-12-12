using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c6dd42a7_d3a3_4405_b64a_159bcf3beab8
{
    public class MoveMeshToPointLine : Instance<MoveMeshToPointLine>
    {

        [Output(Guid = "cf032071-fedc-45aa-9dbc-7b70f61e14dc")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new Slot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "2d550543-d102-454e-b9f3-ff7d7832bba9")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "1016ebbc-5d8b-428d-8f78-6a3e11b7705c")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "c51d7f13-290c-48f9-b7ad-053d20e037b2")]
        public readonly InputSlot<float> Range = new InputSlot<float>();

        [Input(Guid = "62744fc9-00e0-40c1-881c-ea95367efd2f")]
        public readonly InputSlot<float> Offset = new InputSlot<float>();

        [Input(Guid = "413a2c30-586e-452c-8b67-b7268a32702f")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        
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

