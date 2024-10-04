using System;
using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Operators.Types.Id_00618c91_f39a_44ea_b9d8_175c996460dc;

namespace T3.Operators.Types.Id_5b127401_600c_4247_9d59_2f6ff359ba85
{
    public class _GetSceneDefinitionPoints : Instance<_GetSceneDefinitionPoints>
    {
        [Output(Guid = "11DA2373-6F05-4818-8616-4F2C33F63301")]
        public readonly Slot<BufferWithViews> Points = new();
        
        [Output(Guid = "CB0E5B54-1C68-43A6-9101-2C9BC9B67C51")]
        public readonly Slot<BufferWithViews> ChunkDefsBuffer = new();
        
        
        public _GetSceneDefinitionPoints()
        {
            ChunkDefsBuffer.UpdateAction = Update;
            Points.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var sceneDefinition = SceneSetup.GetValue(context);
            if(sceneDefinition== null)
                return;
            
            sceneDefinition.GenerateSceneDrawDispatches();
            
            var dispatchesCount = sceneDefinition.Dispatches.Count;
            if(dispatchesCount == 0)
                return;
            
            //var instancePoints = new StructuredList<Point>(dispatchesCount);
            if (dispatchesCount != _pointList.NumElements)
            {
                _pointList = new StructuredList<Point>(dispatchesCount);
            }
            
            var chunkIndices = new int[dispatchesCount];
            
            for (var index = 0; index < dispatchesCount; index++)
            {
                var sceneDispatch = sceneDefinition.Dispatches[index];
                var matrix = sceneDispatch.CombinedTransform;
                
                _pointList.TypedElements[index].W = 1;
                _pointList.TypedElements[index].Color = Vector4.One;
                Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation);
                _pointList.TypedElements[index].Position = translation;
                _pointList.TypedElements[index].Stretch = new Vector3( MathF.Abs(scale.X), MathF.Abs(scale.Y), MathF.Abs(scale.Z));
                _pointList.TypedElements[index].Selected = 1;
                _pointList.TypedElements[index].Orientation = rotation;
                chunkIndices[index] = sceneDispatch.ChunkIndex;
            }
            
            _chunksDefBuffer = new BufferWithViews();
            ResourceManager.SetupStructuredBuffer(chunkIndices, dispatchesCount * 4, 4, ref _chunksDefBuffer.Buffer);
            ResourceManager.CreateStructuredBufferSrv(_chunksDefBuffer.Buffer, ref _chunksDefBuffer.Srv);
            ResourceManager.CreateStructuredBufferUav(_chunksDefBuffer.Buffer, UnorderedAccessViewBufferFlags.None,
                                                      ref _chunksDefBuffer.Uav);
            
            _pointsBuffer = new BufferWithViews();
            ResourceManager.SetupStructuredBuffer(_pointList.TypedElements, _pointList.ElementSizeInBytes * _pointList.NumElements, _pointList.ElementSizeInBytes, ref _pointsBuffer.Buffer);
            ResourceManager.CreateStructuredBufferSrv(_pointsBuffer.Buffer, ref _pointsBuffer.Srv);
            ResourceManager.CreateStructuredBufferUav(_pointsBuffer.Buffer, UnorderedAccessViewBufferFlags.None,
                                                      ref _pointsBuffer.Uav);
            
            ChunkDefsBuffer.Value = _chunksDefBuffer;
            Points.Value = _pointsBuffer;
            
            ChunkDefsBuffer.DirtyFlag.Clear();
            Points.DirtyFlag.Clear();
        }
        
        private  StructuredList<Point> _pointList = new(1);
        private BufferWithViews _chunksDefBuffer = new();
        private BufferWithViews _pointsBuffer = new();
        
        [Input(Guid = "41054d35-5564-42db-9109-263f8c447057")]
        public readonly InputSlot<SceneSetup> SceneSetup = new();
        
    }
}