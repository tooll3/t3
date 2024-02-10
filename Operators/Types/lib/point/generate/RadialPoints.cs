using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3352d3a1_ab04_4d0a_bb43_da69095b73fd
{
    public class RadialPoints : Instance<RadialPoints>
,ITransformable
    {

        [Output(Guid = "d7605a96-adc6-4a2b-9ba4-33adef3b7f4c")]
        public readonly TransformCallbackSlot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        public RadialPoints()
        {
            OutBuffer.TransformableOp = this;
        }
        
        IInputSlot ITransformable.TranslationInput => Center;
        IInputSlot ITransformable.RotationInput => null;
        IInputSlot ITransformable.ScaleInput => null;

        public Action<Instance, EvaluationContext> TransformCallback { get; set; }
        
        [Input(Guid = "b654ffe2-d46e-4a62-89b3-a9692d5c6481")]
        public readonly InputSlot<int> Count = new();

        [Input(Guid = "acce4779-56d6-47c4-9c52-874fca91a3a1")]
        public readonly InputSlot<float> Radius = new();

        [Input(Guid = "13cbb509-f90c-4ae7-a9d3-a8fc907794e3")]
        public readonly InputSlot<float> RadiusOffset = new();

        [Input(Guid = "ca84209e-d821-40c6-b23c-38fc4bbd47b0")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new();

        [Input(Guid = "f6986f68-851b-4cd1-ae59-bf189aa1698e")]
        public readonly InputSlot<System.Numerics.Vector3> Offset = new();

        [Input(Guid = "5a3347a2-ba87-4b38-a1a8-94bd0ef70f48")]
        public readonly InputSlot<float> StartAngle = new();

        [Input(Guid = "94b2a118-f760-4043-933c-31283e6e7006")]
        public readonly InputSlot<float> Cycles = new();

        [Input(Guid = "76124db6-4b89-4d7c-bd25-2ebf95b1c141")]
        public readonly InputSlot<bool> CloseCircleLine = new();

        [Input(Guid = "6df5829e-a534-4620-bcd5-9324f94b4f54")]
        public readonly InputSlot<System.Numerics.Vector3> Axis = new();

        [Input(Guid = "3ee710be-8954-431b-8d3a-38f7f03f0f02")]
        public readonly InputSlot<float> W = new();

        [Input(Guid = "526cf26b-6cf6-4cba-be2a-4819c2a422bf")]
        public readonly InputSlot<float> WOffset = new();

        [Input(Guid = "01a62754-7629-487d-a43a-f0cd2fbfafce")]
        public readonly InputSlot<System.Numerics.Vector3> OrientationAxis = new();

        [Input(Guid = "cd917c3d-489e-4e4d-b5dc-eacc846d82ef")]
        public readonly InputSlot<float> OrientationAngle = new();

        [Input(Guid = "ef8d1fe2-8470-4113-8d20-40a92d0dab97")]
        public readonly InputSlot<System.Numerics.Vector2> BiasAndGain = new InputSlot<System.Numerics.Vector2>();
    }
}

