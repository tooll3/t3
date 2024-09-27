using T3.Core.Utils.Geometry;

namespace lib.exec.context;

[Guid("b7731197-b922-4ed8-8e22-bc7596c64f6c")]
public class GetPosition : Instance<GetPosition>
{
    [Output(Guid = "C809C405-81D3-44CA-A39C-C8DFA6AB3205")]
    public readonly Slot<Command> UpdateCommand = new();

    [Output(Guid = "BB81C71D-9E19-4371-8995-BD61AE426A16")]
    public readonly Slot<Vector3> Position = new();

    [Output(Guid = "ACE050AC-8E49-409A-A194-4CB3192148CA")]
    public readonly Slot<Vector3> Scale = new();
        
    [Output(Guid = "751E97DE-C418-48C7-823E-D4660073A559")]
    public readonly Slot<Vector4[]> ObjectToWorld = new();
        
    public GetPosition()
    {
        UpdateCommand.UpdateAction += Update;
        Position.UpdateAction += UpdateReturnResults;
        Scale.UpdateAction += UpdateReturnResults;
        ObjectToWorld.UpdateAction += UpdateReturnResults;
    }

    private void UpdateReturnResults(EvaluationContext context)
    {
        Position.Value = _lastPosition;
        Scale.Value = _lastScale;
        ObjectToWorld.Value = _matrix;
    }

    private void Update(EvaluationContext context)
    {
        var p = new Vector4(0, 0, 0, 1);
        var pInWorld = Vector4.Transform(p, context.ObjectToWorld);
        _lastPosition = new Vector3(pInWorld.X, pInWorld.Y, pInWorld.Z);
            
        var s = new Vector4(1, 1, 1, 0);
        var sInWorld = Vector4.Transform(s, context.ObjectToWorld);
        _lastScale = new Vector3(sInWorld.X, sInWorld.Y, sInWorld.Z);
            
        _matrix[0] = context.ObjectToWorld.Row1();
        _matrix[1] = context.ObjectToWorld.Row2();
        _matrix[2] = context.ObjectToWorld.Row3();
        _matrix[3] = context.ObjectToWorld.Row4();
    }

    private readonly Vector4[] _matrix = new Vector4[4] ;
    private Vector3 _lastPosition;
    private Vector3 _lastScale;
        

}