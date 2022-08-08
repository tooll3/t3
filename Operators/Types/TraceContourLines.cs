using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_46749ae5_ef10_43e7_a712_5cbd7a1d4398
{
    public class TraceContourLines : Instance<TraceContourLines>
,ITransformable
    {
        [Output(Guid = "5c2dc61d-012f-478a-ae4d-583ef4696e2d")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews>();

        public TraceContourLines()
        {
            OutBuffer.TransformableOp = this;
        }
        
        IInputSlot ITransformable.TranslationInput => Center;
        IInputSlot ITransformable.RotationInput => TextureRotate;
        IInputSlot ITransformable.ScaleInput => TextureStretch;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "4ed5dad4-e9f6-49a2-8aa6-6062b4202012")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "b4e3f2c7-0996-4832-abcc-f0e89e5c56c2")]
        public readonly InputSlot<float> SampleRadius = new InputSlot<float>();

        [Input(Guid = "e0521d6a-07f2-4b61-9701-5a1f78e732a4")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "818ebbce-9174-4fd1-800e-be816870311e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "709050be-0873-4a37-96fb-c8fa98780c6f")]
        public readonly InputSlot<System.Numerics.Vector2> TextureStretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "07e800ec-b9cb-45f6-954f-f242b95e9a87")]
        public readonly InputSlot<System.Numerics.Vector3> TextureRotate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "0529bdf2-47bf-40bf-8ad9-d925545257e9")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> TextureMode = new InputSlot<SharpDX.Direct3D11.TextureAddressMode>();

        [Input(Guid = "1cab22cf-494c-4e5c-b968-8e876a28e2e4")]
        public readonly InputSlot<float> TextureScale = new InputSlot<float>();

        [Input(Guid = "de7eb925-58d9-4300-a0d1-90f7c41722b2")]
        public readonly InputSlot<float> AdjustmentSpeed = new InputSlot<float>();


        private enum Attributes
        {
            NotUsed =0,
            For_X = 1,
            For_Y =2,
            For_Z =3,
            For_W =4,
            Rotate_X =5,
            Rotate_Y =6 ,
            Rotate_Z =7,
        }
    }
}

