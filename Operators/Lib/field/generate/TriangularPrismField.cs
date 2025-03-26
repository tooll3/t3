namespace Lib.field.generate;


[Guid("280989a6-a35d-4a91-b8c6-b1c7ca9ca9d6")]
internal sealed class TriangularPrismField : Instance<TriangularPrismField>
,IGraphNodeOp
{
    [Output(Guid = "15393ac5-2e6c-4f34-8409-b98a95103d6f")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public TriangularPrismField()
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
        float2 h = {ShaderNode}RadiusLength;
        float3 q = abs(p);
        return max(q.z-h.y,max(q.x*0.866025+p.y*0.5,-p.y)-h.x*0.5);
        }}");
    }
    
    [GraphParam]
    [Input(Guid = "f088b205-2587-4f7c-903d-ee162b8d919b")]
    public readonly InputSlot<Vector3> Center = new();
    
    [GraphParam]
    [Input(Guid = "78b9be91-d1f2-4d96-ae7b-e6d2c126cc9b")]
    public readonly InputSlot<Vector2> RadiusLength = new();
   
   
}

