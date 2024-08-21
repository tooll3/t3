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
        // [Output(Guid = "8a1e3bc8-a7bd-40b5-a4cf-241c13bddbfb")]
        // public readonly Slot<Command> Output = new();
        
        [Output(Guid = "D9C04756-8922-496D-8380-120F280EF65B")]
        public readonly Slot<StructuredList> ResultList = new();
        
        [Output(Guid = "CB0E5B54-1C68-43A6-9101-2C9BC9B67C51")]
        public readonly Slot<BufferWithViews> IndicesBuffer = new();
        
        
        public _GetSceneDefinitionPoints()
        {
            ResultList.UpdateAction = Update;
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
            
            //var rot = Quaternion.CreateFromAxisAngle(Vector3.Normalize( RotationAxis.GetValue(context)), RotationAngle.GetValue(context) * MathUtils.ToRad);
            //var array = _addSeparator ? _pointListWithSeparator : _pointList;
            var instancePoints = new StructuredList<Point>(dispatchesCount);
            var chunkIndices = new int[dispatchesCount];
            
            for (var index = 0; index < dispatchesCount; index++)
            {
                var sceneDispatch = sceneDefinition.Dispatches[index];
                var matrix = sceneDispatch.CombinedTransform;
                
                
                //OutPosition.Value = pos;
                instancePoints.TypedElements[index].Position = new Vector3(matrix.M41, matrix.M42, matrix.M43);
                instancePoints.TypedElements[index].W = 1;
                instancePoints.TypedElements[index].Color = Vector4.One;
                
                //Matrix4x4.Decompose(matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation);
                
                //instancePoints.TypedElements[index].Stretch = scale;
                
                instancePoints.TypedElements[index].Stretch = Vector3.TransformNormal(Vector3.One,matrix );
                //instancePoints.TypedElements[index].Stretch = new Vector3( MathF.Abs(matrix.M11), MathF.Abs(matrix.M22), MathF.Abs(matrix.M33));
                instancePoints.TypedElements[index].Selected = 1;
                instancePoints.TypedElements[index].Orientation = Quaternion.CreateFromRotationMatrix(matrix);
                
                chunkIndices[index] = sceneDispatch.ChunkIndex;
                //Log.Debug("Stretch: " + instancePoints.TypedElements[index].Stretch);
            }
            
            _indicesBuffer = new BufferWithViews();
            ResourceManager.SetupStructuredBuffer(chunkIndices, dispatchesCount * 4, 4, ref _indicesBuffer.Buffer);
            ResourceManager.CreateStructuredBufferSrv(_indicesBuffer.Buffer, ref _indicesBuffer.Srv);
            ResourceManager.CreateStructuredBufferUav(_indicesBuffer.Buffer, UnorderedAccessViewBufferFlags.None,
                                                      ref _indicesBuffer.Uav);
            
            ResultList.Value = instancePoints;
            IndicesBuffer.Value = _indicesBuffer;
        }
        
        private readonly StructuredList<Point> _pointList = new(1);
        private BufferWithViews _indicesBuffer = new();
        

        [Input(Guid = "41054d35-5564-42db-9109-263f8c447057")]
        public readonly InputSlot<SceneSetup> SceneSetup = new();
        
    }
}