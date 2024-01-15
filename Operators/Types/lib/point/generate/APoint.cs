using System;
using SharpDX;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;
using Point = T3.Core.DataTypes.Point;
using Quaternion = System.Numerics.Quaternion;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_9989f539_f86c_4508_83d7_3fc0e559f502
{
    public class APoint : Instance<APoint>, ITransformable
    {
        [Output(Guid = "5915D7E2-054D-4917-86BD-25AD1BEB1754")]
        public readonly Slot<BufferWithViews> Buffer = new();
        
        [Output(Guid = "D9C04756-8922-496D-8380-120F280EF65B")]
        public readonly Slot<StructuredList> ResultList = new();
        
        [Output(Guid = "8698D60D-8CD9-4A3F-9001-19DAC29028CC")]
        public readonly Slot<Vector3> OutPosition = new();
        
        
        public APoint()
        {
            Buffer.UpdateAction = UpdateWithBuffer;
            ResultList.UpdateAction = Update;
            OutPosition.UpdateAction = Update;
            _pointListWithSeparator.TypedElements[1] = Point.Separator();

        }
        
        IInputSlot ITransformable.TranslationInput => Position;
        IInputSlot ITransformable.RotationInput => null;
        IInputSlot ITransformable.ScaleInput => null;

        public Action<Instance, EvaluationContext> TransformCallback { get; set; }

        private void UpdateWithBuffer(EvaluationContext context)
        {
            Update(context);
            UpdateBuffer();
            Buffer.Value = _bufferWithViews;
        }

        private void Update(EvaluationContext context)
        {
            TransformCallback?.Invoke(this, context);
            
            var pos = Position.GetValue(context);
            _addSeparator = AddSeparator.GetValue(context);

            var rot = Quaternion.CreateFromAxisAngle(Vector3.Normalize( RotationAxis.GetValue(context)), RotationAngle.GetValue(context) * MathUtils.ToRad);
            var array = _addSeparator ? _pointListWithSeparator : _pointList;
            OutPosition.Value = pos;
            array.TypedElements[0].Position = pos;
            array.TypedElements[0].W = W.GetValue(context);
            array.TypedElements[0].Color = Color.GetValue(context);
            array.TypedElements[0].Stretch = Extend.GetValue(context);
            array.TypedElements[0].Selected = Selected.GetValue(context);
            array.TypedElements[0].Orientation = rot;
            ResultList.Value = array;
            
            ResultList.DirtyFlag.Clear();
            OutPosition.DirtyFlag.Clear();
        }

        private void UpdateBuffer()
        {
            var source = _addSeparator ? _pointListWithSeparator : _pointList;
            var sizeChanged = _buffer == null || _buffer.Description.SizeInBytes != source.TotalSizeInBytes;
            
            using (var data = new DataStream(source.TotalSizeInBytes, true, true))
            {
                data.Position = 0;
                source.WriteToStream(data);

                try
                {
                    ResourceManager.SetupStructuredBuffer(data, source.TotalSizeInBytes, Point.Stride, ref _buffer);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to setup structured buffer " + e.Message, this);
                    return;
                }
            }

            if (sizeChanged)
            {
                Log.Debug("Updating Point buffer size....", this);
                ResourceManager.CreateStructuredBufferSrv(_buffer, ref _bufferWithViews.Srv);
                ResourceManager.CreateStructuredBufferUav(_buffer, UnorderedAccessViewBufferFlags.None, ref _bufferWithViews.Uav);
                _bufferWithViews.Buffer = _buffer;
            }
        }

        private readonly StructuredList<Point> _pointListWithSeparator = new(2);
        private readonly StructuredList<Point> _pointList = new(1);
        
        private SharpDX.Direct3D11.Buffer _buffer;
        private readonly BufferWithViews _bufferWithViews = new() ;
        private bool _addSeparator;
        
        [Input(Guid = "a0a453db-d8f1-415a-9a98-3c88a25b15e7")]
        public readonly InputSlot<Vector3> Position = new();

        [Input(Guid = "55a3370f-1768-414f-b38d-4accc5e93914")]
        public readonly InputSlot<Vector3> RotationAxis = new();

        [Input(Guid = "E9859381-EB88-4856-91C5-60D30AC6035A")]
        public readonly InputSlot<float> RotationAngle = new();

        [Input(Guid = "2d7d85ce-7b5e-4e86-bae2-88a7c4f7a2e5")]
        public readonly InputSlot<float> W = new();
        
        [Input(Guid = "34AD759E-9A81-4D7E-9024-5ABACC279895")]
        public readonly InputSlot<Vector4> Color = new();

        [Input(Guid = "130B5C11-66DD-4C0E-AC67-924554BAD2D8")]
        public readonly InputSlot<Vector3> Extend = new();
        
        [Input(Guid = "CA12DF13-7529-4EDE-B6FC-CE8AEBA4F33E")]
        public readonly InputSlot<float> Selected = new();
        
        [Input(Guid = "53CDE701-435F-42E4-B598-DB0E607A238C")]
        public readonly InputSlot<bool> AddSeparator = new();
        
    }
}