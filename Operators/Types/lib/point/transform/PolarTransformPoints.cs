using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3d255f3e_d2e2_4f61_a03d_5af7043fabfc
{
    public class PolarTransformPoints : Instance<PolarTransformPoints>
,ITransformable
    {
        [Output(Guid = "62a9bc7b-4678-409a-8e26-7f6377b72cb0")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Output = new();

        public PolarTransformPoints()
        {
            Output.TransformableOp = this;
        }        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Scale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "83d00528-423a-43f9-8750-97d7a4909c49")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "eb1ba2fe-1bc5-41c0-8acb-875fb3faa8ae")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new();

        [Input(Guid = "c7f7e8d2-8694-4eab-9693-c3e6c1ec95e8")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new();

        [Input(Guid = "433a0c6d-fd59-49d6-8476-601a316f0b88")]
        public readonly InputSlot<System.Numerics.Vector3> Scale = new();

        [Input(Guid = "f929e486-a49f-445b-962f-e0f3fc7d52cc")]
        public readonly InputSlot<float> UniformScale = new();

        [Input(Guid = "8fa1db66-53aa-4737-983b-91deda39bb65", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();
        
        private enum Modes
        {
            CartesianToCylindrical,
            CartesianToSpherical,
        }
        
        
        
        
        
    }
}

