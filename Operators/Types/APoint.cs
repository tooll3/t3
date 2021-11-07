using System;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_9989f539_f86c_4508_83d7_3fc0e559f502
{
    public class APoint : Instance<APoint>, ITransformable
    {
        [Output(Guid = "D9C04756-8922-496D-8380-120F280EF65B", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<StructuredList> ResultList = new Slot<StructuredList>();
        
        public APoint()
        {
            //ResultList.TransformableOp = this;
            ResultList.UpdateAction = Update;
            _pointListWithSeparator.TypedElements[1] = Point.Separator();
        }
        
        System.Numerics.Vector3 ITransformable.Translation { get => Position.Value; set => Position.SetTypedInputValue(value); }
        System.Numerics.Vector3 ITransformable.Rotation { get => System.Numerics.Vector3.Zero; set { } }
        System.Numerics.Vector3 ITransformable.Scale { get => System.Numerics.Vector3.One; set { } }

        public Action<ITransformable, EvaluationContext> TransformCallback { get; set; }

        private void Update(EvaluationContext context)
        {
            TransformCallback?.Invoke(this, context);
            
            var from = Position.GetValue(context);
            var w = W.GetValue(context);
            var addSeparator = AddSeparator.GetValue(context);

            var rot = Quaternion.CreateFromAxisAngle(RotationAxis.GetValue(context), RotationAngle.GetValue(context) * MathUtils.ToRad);
            var array = addSeparator ? _pointListWithSeparator : _pointList;
            
            array.TypedElements[0].Position = from;
            array.TypedElements[0].W = w;
            array.TypedElements[0].Orientation = rot;
            ResultList.Value = array;
        }

        private readonly StructuredList<Point> _pointListWithSeparator = new StructuredList<Point>(2);
        private readonly StructuredList<Point> _pointList = new StructuredList<Point>(1);
        //private readonly Point Separator;

        [Input(Guid = "a0a453db-d8f1-415a-9a98-3c88a25b15e7")]
        public readonly InputSlot<Vector3> Position = new InputSlot<Vector3>();

        [Input(Guid = "55a3370f-1768-414f-b38d-4accc5e93914")]
        public readonly InputSlot<Vector3> RotationAxis = new InputSlot<Vector3>();

        [Input(Guid = "E9859381-EB88-4856-91C5-60D30AC6035A")]
        public readonly InputSlot<float> RotationAngle = new InputSlot<float>();

        [Input(Guid = "2d7d85ce-7b5e-4e86-bae2-88a7c4f7a2e5")]
        public readonly InputSlot<float> W = new InputSlot<float>();
        
        [Input(Guid = "53CDE701-435F-42E4-B598-DB0E607A238C")]
        public readonly InputSlot<bool> AddSeparator = new InputSlot<bool>();
        
    }
}