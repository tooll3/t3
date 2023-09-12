using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fbe1a703_f372_4236_9f20_5d0b69183843
{
    public class SurfaceForce : Instance<SurfaceForce>
,ITransformable
    {
        [Output(Guid = "467cad1e-d5d4-493e-9003-c450d48ddf6c")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> Result2 = new TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews>();

        public SurfaceForce()
        {
            Result2.TransformableOp = this;
        }

        IInputSlot ITransformable.TranslationInput => VolumeCenter;
        IInputSlot ITransformable.RotationInput => VolumeRotate;
        IInputSlot ITransformable.ScaleInput => VolumeScale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "a5931f67-3724-4bfc-bed2-c2f490b23c74")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "e3485fea-3a75-47f9-9a7d-ea69f4feb5f6")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeCenter = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "a75686be-909f-42fd-88f1-005e2fcd9f70")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeStretch = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "60eb8c10-93fa-487b-9eda-1767b485bb21")]
        public readonly InputSlot<float> VolumeScale = new InputSlot<float>();

        [Input(Guid = "721c500b-ae7c-4249-a374-1bcf6ae13abd")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeRotate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "aa742901-75bd-4d30-b3cf-154dfd4ab9b0")]
        public readonly InputSlot<float> FallOff = new InputSlot<float>();

        [Input(Guid = "3f449bf0-7136-4b57-af51-0928b025c697")]
        public readonly InputSlot<float> Bias = new InputSlot<float>();

        [Input(Guid = "86c81e40-79e8-4699-a7fe-581f0b09d266")]
        public readonly InputSlot<int> VolumeShape = new InputSlot<int>();

        [Input(Guid = "8387dc9d-e536-4e0a-a650-46feddbea91b")]
        public readonly InputSlot<bool> ClampResult = new InputSlot<bool>();

        [Input(Guid = "5264a72d-679e-4921-a086-42ef5b88469e")]
        public readonly InputSlot<float> MaxAcceleration = new InputSlot<float>();

        [Input(Guid = "09a5476c-78e3-45dc-a9a3-f3bd170bbf05")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "f6efb038-fdea-439c-b25c-5cdf76f15b2b")]
        public readonly InputSlot<float> Threshold = new InputSlot<float>();

        [Input(Guid = "48d1a0b2-c468-412c-a798-0b331cf008cb")]
        public readonly InputSlot<bool> DiscardNonSelected = new InputSlot<bool>();

        [Input(Guid = "cf741b95-e3d1-4f43-99e9-15fcdaa6b648")]
        public readonly InputSlot<float> Strength = new InputSlot<float>();

        [Input(Guid = "eb33f5a6-df40-47fd-aab9-c1eb03b49bbd")]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "f7266e64-172c-42c2-8ca3-5a8112852245")]
        public readonly InputSlot<float> SoftEdge = new InputSlot<float>();

        [Input(Guid = "0db5c531-721d-403e-9154-e31f6be20ec6")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> GizmoVisibility = new InputSlot<T3.Core.Operator.GizmoVisibility>();


        
        private enum Shapes
        {
            Sphere,
            Box,
            Plane,
            Zebra,
            Noise,
        }
        
        private enum Modes
        {
            Override,
            Add,
            Sub,
            Multiply,
            Invert,
        }
    }
}

