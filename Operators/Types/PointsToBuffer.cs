using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_59b810f1_7849_40a7_ae10_7e8008685311
{
    public class PointsToBuffer : Instance<PointsToBuffer>
    {
        [Output(Guid = "293E44BF-58C8-4D97-AAA1-AFD40D182AA0")]
        public readonly Slot<BufferWithViews> OutBuffer = new Slot<BufferWithViews>();

        
        [Output(Guid = "36FD3A40-6416-4BCB-9FAC-9CD9221BEBA8")]
        public readonly Slot<int> Length = new Slot<int>();
        
        public PointsToBuffer()
        {

            OutBuffer.UpdateAction = Update;
            Length.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var pointArray = PointArray.GetValue(context);
            if (pointArray == null || pointArray.Length == 0)
            {
                Length.Value = 0;
                Log.Warning("Invalid input for PointsToBuffer");
                return;
            }

            Length.Value = pointArray.Length;
            
            var resourceManager = ResourceManager.Instance();
            if (_bufferData.Length != pointArray.Length)
            {
                _bufferData = new T3.Core.DataTypes.Point[pointArray.Length];
            }
            

            for (int index = 0; index < pointArray.Length; index++)
            {
                _bufferData[index] = pointArray[index];
            }

            var stride = 32;

            _bufferWithViews.Buffer = _buffer;
            resourceManager.SetupStructuredBuffer(_bufferData, stride * pointArray.Length, stride, ref _buffer);
            resourceManager.CreateStructuredBufferSrv(_buffer, ref _bufferWithViews.Srv);
            resourceManager.CreateStructuredBufferUav(_buffer, UnorderedAccessViewBufferFlags.None, ref _bufferWithViews.Uav);
            OutBuffer.Value = _bufferWithViews;
        }

        private Buffer _buffer;
        private T3.Core.DataTypes.Point[] _bufferData = new T3.Core.DataTypes.Point[0];
        private BufferWithViews _bufferWithViews = new BufferWithViews();

        [Input(Guid = "6fddc26b-31e2-41f1-b86c-0b71d898801a")]
        public readonly InputSlot<T3.Core.DataTypes.Point[]> PointArray = new InputSlot<T3.Core.DataTypes.Point[]>();
    }
}