namespace Lib.field.generate;


[Guid("752e4358-05e0-4651-a147-f141138f5244")]
internal sealed class CapsuleLineField : Instance<CapsuleLineField>
,IGraphNodeOp
{
    [Output(Guid = "1d6a73c3-1a6a-452a-a4a2-ddee308aa0ae")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public CapsuleLineField()
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
    public void GetShaderCode(StringBuilder shaderStringBuilder, Dictionary<string, string> globals)
    { 
        shaderStringBuilder.AppendLine( $@"
        float {ShaderNode}(float3 p) {{
            p = p - {ShaderNode}Center;
            float3 a = {ShaderNode}A;
            float3 b = {ShaderNode}B;
            float r = {ShaderNode}Radius;
            float3 pa = p - a;
            float3 ba = b - a;
            float h = clamp( dot(pa,ba)/dot(ba,ba), 0.0, 1.0 );
            return length( pa - ba*h ) - r;
        }}");
    }
    
    [GraphParam]
    [Input(Guid = "50A47EA6-A941-4FF4-9B87-14DDD2791FE4")]
    public readonly InputSlot<Vector3> Center = new();

    [GraphParam]
    [Input(Guid = "ace2fa64-382c-4386-b623-5b1051f4bcf4")]
    public readonly InputSlot<Vector3> A = new();

    [GraphParam]
    [Input(Guid = "0B4FDE07-5439-4DBD-AF37-32E5C6DC9315")]
    public readonly InputSlot<Vector3> B = new();

    [GraphParam]
    [Input(Guid = "808C44FF-44E2-4A22-91F1-F459F298E6EB")]
    public readonly InputSlot<float> Radius = new();
}

