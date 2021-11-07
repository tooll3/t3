using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1a241222_200b_417d_a8c7_131e3b48cc36
{
    public class PointsOnSphere : Instance<PointsOnSphere>
    {

        [Output(Guid = "c20f4675-6387-45da-b14f-8d0a3af5e672")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        // public RadialGPoints()
        // {
        //     OutBuffer.TransformableOp = this;
        // }
        //
        // System.Numerics.Vector3 ITransformable.Translation { get => Center.Value; set => Center.SetTypedInputValue(value); }
        // System.Numerics.Vector3 ITransformable.Rotation { get => System.Numerics.Vector3.Zero; set { } }
        // System.Numerics.Vector3 ITransformable.Scale { get => System.Numerics.Vector3.One; set { } }
        //
        // public Action<ITransformable, EvaluationContext> TransformCallback { get => OutBuffer.TransformCallback; set => OutBuffer.TransformCallback = value; }
        
        
        [Input(Guid = "0b42b3e6-a6fd-4edc-88b1-d91f9c775023")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "0bdc6243-3e52-4b1a-b070-731ed27388c6")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "21140fe1-9fb5-4a79-b03a-7deac242fba2")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "813df416-a783-433c-9645-921c885c9840")]
        public readonly InputSlot<float> StartAngle = new InputSlot<float>();
    }
}

