using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace lib.point.modify
{
	[Guid("3d958f08-9c0f-45eb-a252-de880b5834f3")]
    public class CustomPointShader : Instance<CustomPointShader>,ITransformable
    {
        public CustomPointShader()
        {
            Output.TransformableOp = this;
        }
        
        public IInputSlot TranslationInput => Center;
        public IInputSlot RotationInput => null;
        public IInputSlot ScaleInput => null;
        
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }        
        
        [Output(Guid = "e0097148-4395-4441-83d2-c5cf5b76bb61")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "e77660e2-0fd0-45ea-8e0b-c607a757bb49")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "e9712b03-e7aa-4fe5-b5cf-f2c5d0c0b0df")]
        public readonly InputSlot<string> ShaderCode = new();

        [Input(Guid = "01898885-4140-4435-bb44-a7a6f6f32657")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new();

        [Input(Guid = "a5c7863e-9c26-4109-9851-3244086b0ccc")]
        public readonly InputSlot<float> A = new();

        [Input(Guid = "e5a7649f-684e-4938-8ae3-7289f5b9ff45")]
        public readonly InputSlot<float> B = new();

        [Input(Guid = "b909844f-cff7-4907-9bc8-e9c2281582bf")]
        public readonly InputSlot<float> C = new();

        [Input(Guid = "20226539-a481-4df6-8dc7-cc65de915ea9")]
        public readonly InputSlot<float> D = new();

        [Input(Guid = "dfbb9327-6cd2-41d3-8b2b-0abd7716471b")]
        public readonly InputSlot<bool> IgnoreTemplate = new();
    }
}

