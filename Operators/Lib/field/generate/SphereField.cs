namespace Lib.field.generate;

[Guid("fc2a33fc-d957-4113-8096-92d4dcbe14b5")]
internal sealed class SphereField : Instance<SphereField>, IGraphNodeOp
{
    [Output(Guid = "02f7d494-72ed-4247-88d7-0cbb730edf65")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public SphereField()
    {
        ShaderNode = new ShaderGraphNode(this);
        Result.Value = ShaderNode;
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);
    }
    
    public ShaderGraphNode ShaderNode { get; }

    public void GetPreShaderCode(CodeAssembleContext c, int inputIndex)
    {
        var n = ShaderNode;
        c.AppendCall($"f{c}.w = length(p{c}.xyz - {n}Center) - {n}Radius;");
    }

    public void GetPostShaderCode(CodeAssembleContext c, int inputIndex)
    {
    }


    [GraphParam]
    [Input(Guid = "CA582E39-37D7-4DF6-B942-E2330F2BF2C6")]
    public readonly InputSlot<Vector3> Center = new();
    
    [GraphParam]
    [Input(Guid = "3DD7C779-7982-4E7C-B4CE-F1915F477AD0")]
    public readonly InputSlot<float> Radius = new(); 
    

}

