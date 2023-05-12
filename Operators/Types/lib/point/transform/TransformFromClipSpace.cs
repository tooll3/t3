using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_81377edc_0a42_4bb1_9440_2f2433d5757f
{
    public class TransformFromClipSpace : Instance<TransformFromClipSpace>
,ITransformable
    {
        [Output(Guid = "fa70200b-cfcb-4efe-afbd-48cefea1ca39")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Output = new TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews>();

        public TransformFromClipSpace()
        {
            Output.TransformableOp = this;
        }        
        IInputSlot ITransformable.TranslationInput => Translation;
        IInputSlot ITransformable.RotationInput => Rotation;
        IInputSlot ITransformable.ScaleInput => Stretch;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "a761a9d9-807c-4fd8-a5b6-0a565d18616b")]
        public readonly InputSlot<System.Numerics.Vector3> Translation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "f57286ab-3a1b-44ae-a9d4-b5660d4828f5")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "92c257c7-8672-4467-915a-06ef56e73209")]
        public readonly InputSlot<System.Numerics.Vector3> Stretch = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "506e1c3f-4091-443f-8481-e6dd0e7a9ae6")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "22071412-28b4-4139-b467-c2bd87411e52")]
        public readonly InputSlot<bool> UpdateRotation = new InputSlot<bool>();

        [Input(Guid = "4f3342cd-4961-491b-b6da-69c4f40199f5")]
        public readonly InputSlot<System.Numerics.Vector3> Shearing = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "7618e126-702a-4be8-b168-b807d65d71a1")]
        public readonly InputSlot<float> ScaleW = new InputSlot<float>();

        [Input(Guid = "3eee5c58-d591-4aed-b00a-31049581d410")]
        public readonly InputSlot<float> OffsetW = new InputSlot<float>();

        [Input(Guid = "9f35477d-985d-4466-85fa-01244365576e")]
        public readonly InputSlot<int> Space = new InputSlot<int>();

        [Input(Guid = "d0b0c07f-8081-424f-876f-0d48a315b536")]
        public readonly InputSlot<bool> WIsWeight = new InputSlot<bool>();

        [Input(Guid = "e02d3e37-4da6-4528-b06f-6f26c818d1d8")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "44f670ca-3b10-4855-b7c4-9a645045d334")]
        public readonly InputSlot<System.Numerics.Vector3> Pivot = new InputSlot<System.Numerics.Vector3>();
        
        
        
        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
        }
        
        
        
        
        
    }
}

