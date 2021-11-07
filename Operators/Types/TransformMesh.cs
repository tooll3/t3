using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_026e6917_6e6f_4ee3_b2d4_58f4f1de74c9
{
    public class TransformMesh : Instance<TransformMesh>, ITransformable
    {
        [Output(Guid = "9ff1bfed-4554-4c55-9557-8b318ac47afe")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.MeshBuffers> Result = new TransformCallbackSlot<T3.Core.DataTypes.MeshBuffers>();
        
        public TransformMesh()
        {
            Result.TransformableOp = this;
        }        
        
        // implementation of ITransformable
        System.Numerics.Vector3 ITransformable.Translation { get => Translation.Value; set => Translation.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Rotation { get => Rotation.Value; set => Rotation.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Scale { get => Scale.Value; set => Scale.SetTypedInputValue(value); }
        //public Action<ITransformable, EvaluationContext> TransformCallback { get; set; }

        public Action<ITransformable, EvaluationContext> TransformCallback { get => Result.TransformCallback; set => Result.TransformCallback = value; }
        
        [Input(Guid = "c2c9afc7-3474-40c3-be82-b9f48c92a2c5")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "da607ebd-6fec-4ae8-bf91-b70dcb794557")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "1168094f-1eee-4ed7-95e2-9459e6171e08")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "f37c11a5-b210-4e83-8ebd-64ea49ee9b96")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "86791d0a-97c3-413a-89d9-aa2ddd40ce4a")]
        public readonly InputSlot<float> UniformScale = new InputSlot<float>();

        [Input(Guid = "71531810-78ab-449e-bb13-bfe5fe3d2a69")]
        public readonly InputSlot<bool> UseVertexSelection = new InputSlot<bool>();
        
        
        
        
        
        
        
        
        
    }
}

