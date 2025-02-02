using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_97ffb173_f4cc_4143_a479_80cf3465cc7e
{
    public class MeshProjectUV : Instance<MeshProjectUV>
,ITransformable
    {
        [Output(Guid = "84C6619E-4264-4D16-B870-5413D986F08A")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.MeshBuffers> OutBuffer = new();

        public MeshProjectUV()
        {
            OutBuffer.TransformableOp = this;
        }
        
        IInputSlot ITransformable.TranslationInput => Translate;
        IInputSlot ITransformable.RotationInput => Rotate;
        IInputSlot ITransformable.ScaleInput => Stretch;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "10bc1ef8-e036-4da0-9bc8-65da0ddff7f0")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "8a991e5f-361c-4ab9-9660-bdf759c87594")]
        public readonly InputSlot<System.Numerics.Vector3> Translate = new();

        [Input(Guid = "4da35d2e-4e12-44ee-8b06-6dfe54be104f")]
        public readonly InputSlot<System.Numerics.Vector3> Rotate = new();

        [Input(Guid = "432c3388-907b-4b7f-8c6e-73a5100bc43a")]
        public readonly InputSlot<System.Numerics.Vector3> Stretch = new();
        

        [Input(Guid = "2ce29895-0bfd-4b73-ad57-50aca7fd1a96")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "14e8cc1a-0fee-4162-9192-45b635e154a8")]
        public readonly InputSlot<bool> TexCoord2 = new InputSlot<bool>();
    }
}

