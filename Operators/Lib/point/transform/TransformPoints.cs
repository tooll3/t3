using System.Runtime.InteropServices;
using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace lib.point.transform
{
	[Guid("7f6c64fe-ca2e-445e-a9b4-c70291ce354e")]
    public class TransformPoints : Instance<TransformPoints>, ITransformable
    {
        [Output(Guid = "ba17981e-ef9f-46f1-a653-6d50affa8838")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Output = new();

        public TransformPoints()
        {
            Output.TransformableOp = this;
        }        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Stretch;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "565ff364-c3d9-4c60-a9a0-79fdd36d3477")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "9e803bd1-c5a3-4f6f-926d-d19f32dcbae5")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new();

        [Input(Guid = "454d0150-dac4-41b2-83f8-d1ecc3ef76d4")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new();

        [Input(Guid = "523b7acd-d8e7-4473-9ec7-15eec1d795df")]
        public readonly InputSlot<System.Numerics.Vector3> Stretch = new();

        [Input(Guid = "a6e5770b-39dc-4d7b-b92e-53302dc89395")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "0192b746-ff90-4c26-a7d4-754b6ec8006b")]
        public readonly InputSlot<bool> UpdateRotation = new();

        [Input(Guid = "319d71a9-b8dd-406f-a3a2-1c7508ba2ca7")]
        public readonly InputSlot<System.Numerics.Vector3> Shearing = new();

        [Input(Guid = "4af2dbdd-1005-465e-a193-756ed2b29a33")]
        public readonly InputSlot<float> ScaleW = new();

        [Input(Guid = "af0cff8a-126e-47bd-bb60-9198567f85e0")]
        public readonly InputSlot<float> OffsetW = new();

        [Input(Guid = "1ab4671f-7977-4e7e-bb06-f828ae32e3af", MappedType = typeof(Spaces))]
        public readonly InputSlot<int> Space = new();

        [Input(Guid = "56cd97c5-f4f1-4eb4-a53c-312373ee7706")]
        public readonly InputSlot<bool> WIsWeight = new();

        [Input(Guid = "0ef7556a-950f-406c-8e1d-511d17b4ea10")]
        public readonly InputSlot<System.Numerics.Vector3> Pivot = new();
        
        
        
        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
        }
        
        
        
        
        
    }
}

