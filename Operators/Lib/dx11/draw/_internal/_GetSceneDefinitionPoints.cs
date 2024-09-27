using SharpDX.Direct3D11;

namespace lib.dx11.draw._internal;

[Guid("5b127401-600c-4247-9d59-2f6ff359ba85")]
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
            
        var instancePoints = new StructuredList<Point>(dispatchesCount);
        var chunkIndices = new int[dispatchesCount];
            
        for (var index = 0; index < dispatchesCount; index++)
        {
            var sceneDispatch = sceneDefinition.Dispatches[index];
            var matrix = sceneDispatch.CombinedTransform;
                
            instancePoints.TypedElements[index].W = 1;
            instancePoints.TypedElements[index].Color = Vector4.One;
            Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation);
            instancePoints.TypedElements[index].Position = translation;
            instancePoints.TypedElements[index].Stretch = new Vector3( MathF.Abs(scale.X), MathF.Abs(scale.Y), MathF.Abs(scale.Z));
            instancePoints.TypedElements[index].Selected = 1;
            instancePoints.TypedElements[index].Orientation = rotation;
            chunkIndices[index] = sceneDispatch.ChunkIndex;
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