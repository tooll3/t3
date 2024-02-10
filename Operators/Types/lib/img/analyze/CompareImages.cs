using System;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0e13e34f_c07b_4ada_8c87_6b89f4ed8b41
{
    public class CompareImages : Instance<CompareImages>
,ITransformable
    {
        [Output(Guid = "2d59fec4-af4e-4db2-bc11-3685f31e9de5")]
        public readonly TransformCallbackSlot<Texture2D> TextureOutput = new();

        public CompareImages()
        {
            TextureOutput.TransformableOp = this;
        }
        
        IInputSlot ITransformable.TranslationInput => Center;
        IInputSlot ITransformable.RotationInput => null;
        IInputSlot ITransformable.ScaleInput => null;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "5537a990-0d27-4e91-912e-8f913a734722")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "78c3486a-3a82-4e61-81fd-3da904fd7aed")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d2 = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "f46ed94a-7eb2-44d6-9cbb-f8eab586f7c5")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "beada8f0-cece-4526-aa29-3546bebd276a")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "99171ae6-fb07-4121-bf23-e97f30b33be5")]
        public readonly InputSlot<float> IntensityRange = new InputSlot<float>();

    }
}

