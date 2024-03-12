using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a8fd7522_7874_4411_ad8d_b2e7a20bc4ac
{
    public class DeformMesh : Instance<DeformMesh>
,ITransformable
    {
        [Output(Guid = "233d4a02-5e7c-40d1-9a89-4b5e2414900b")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.MeshBuffers> Result = new();
        
        public DeformMesh()
        {
            Result.TransformableOp = this;
        }        
        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Scale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "b3593825-1ff5-4a5f-86cb-379a23471a4d")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "f8c517a7-e07d-4169-81cd-328a9ba90653")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new();

        [Input(Guid = "9eac21ff-c02d-4b27-88a1-4d46e8a0bdff")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new();

        [Input(Guid = "d697b4bc-1a20-495d-9a73-5defe1c4b3a8")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new();

        [Input(Guid = "27900ad7-1dd3-42c3-b236-cf26683c9e9a")]
        public readonly InputSlot<float> UniformScale = new();

        [Input(Guid = "3af8bfb8-a2d8-4919-98f5-5431798d927a")]
        public readonly InputSlot<bool> UseVertexSelection = new();

        [Input(Guid = "cf8e1065-164d-4ba9-8c60-9ab545aaaee2")]
        public readonly InputSlot<System.Numerics.Vector3> Pivot = new();

        [Input(Guid = "f6efc4a6-5267-40aa-82d3-e1b67d852fa8")]
        public readonly InputSlot<float> Spherize = new InputSlot<float>();

        [Input(Guid = "161f293c-1d7f-4543-befe-0b4bd676483a")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "10d0502e-1a9d-4d8f-a516-2b2b465849bf")]
        public readonly InputSlot<float> Taper = new InputSlot<float>();

        [Input(Guid = "c67fa5cb-97b2-4146-a6c1-0ea84a03f703")]
        public readonly InputSlot<float> Twist = new InputSlot<float>();

        [Input(Guid = "a53aad5d-7bc5-4cbb-8a59-90cf8c346992")]
        public readonly InputSlot<int> TaperAxis = new InputSlot<int>();

        [Input(Guid = "ebf1167b-1519-4ca9-bfa6-87472889966b")]
        public readonly InputSlot<int> TwistAxis = new InputSlot<int>();
        
        
        
        
        
        
        
        
        
    }
}

