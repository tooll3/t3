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
        [Output(Guid = "13B7DBBC-D418-4BD5-A8A1-182DF2071A25")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new();

        // public SurfaceForce()
        // {
        //     ParticleSystem.TransformableOp = this;
        // }

        IInputSlot ITransformable.TranslationInput => VolumeCenter;
        IInputSlot ITransformable.RotationInput => VolumeRotate;
        IInputSlot ITransformable.ScaleInput => VolumeScale;
        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        [Input(Guid = "e3485fea-3a75-47f9-9a7d-ea69f4feb5f6")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeCenter = new();

        [Input(Guid = "a75686be-909f-42fd-88f1-005e2fcd9f70")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeStretch = new();

        [Input(Guid = "60eb8c10-93fa-487b-9eda-1767b485bb21")]
        public readonly InputSlot<float> VolumeScale = new();

        [Input(Guid = "721c500b-ae7c-4249-a374-1bcf6ae13abd")]
        public readonly InputSlot<System.Numerics.Vector3> VolumeRotate = new();

        [Input(Guid = "aa742901-75bd-4d30-b3cf-154dfd4ab9b0")]
        public readonly InputSlot<float> FallOff = new();

        [Input(Guid = "3f449bf0-7136-4b57-af51-0928b025c697")]
        public readonly InputSlot<float> Bias = new();

        [Input(Guid = "86c81e40-79e8-4699-a7fe-581f0b09d266")]
        public readonly InputSlot<int> VolumeShape = new();

        [Input(Guid = "8387dc9d-e536-4e0a-a650-46feddbea91b")]
        public readonly InputSlot<bool> ClampResult = new();

        [Input(Guid = "09a5476c-78e3-45dc-a9a3-f3bd170bbf05")]
        public readonly InputSlot<float> Phase = new();

        [Input(Guid = "f6efb038-fdea-439c-b25c-5cdf76f15b2b")]
        public readonly InputSlot<float> Threshold = new();

        [Input(Guid = "cf741b95-e3d1-4f43-99e9-15fcdaa6b648")]
        public readonly InputSlot<float> Strength = new();

        [Input(Guid = "eb33f5a6-df40-47fd-aab9-c1eb03b49bbd")]
        public readonly InputSlot<int> Mode = new();

        [Input(Guid = "f7266e64-172c-42c2-8ca3-5a8112852245")]
        public readonly InputSlot<float> SoftEdge = new();

        [Input(Guid = "0db5c531-721d-403e-9154-e31f6be20ec6")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> GizmoVisibility = new();

        [Input(Guid = "ceb7008d-c536-4d87-b3f8-d5ba9fe29eed")]
        public readonly InputSlot<float> Bounciness = new();

        [Input(Guid = "3b975bd9-b8aa-4a41-97fc-ccd3b5e89e63")]
        public readonly InputSlot<float> Attraction = new();

        [Input(Guid = "d6bf8d83-542b-44ee-bd35-f631179f8d0c")]
        public readonly InputSlot<float> AttractionDecay = new();

        [Input(Guid = "5264a72d-679e-4921-a086-42ef5b88469e")]
        public readonly InputSlot<float> MaxAcceleration = new();

        [Input(Guid = "7c6f58e7-27fc-4271-adef-248cffe5a8b7")]
        public readonly InputSlot<float> Repulsion = new();


        
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

