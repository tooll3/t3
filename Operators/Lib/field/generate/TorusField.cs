using T3.Core.DataTypes.ShaderGraph;

namespace Lib.field.generate;

[Guid("a54e0946-71d0-4985-90bc-184cdb1b6b34")]
internal sealed class TorusField : Instance<TorusField>
                                 , IGraphNodeOp
{
    [Output(Guid = "14cd4d1f-0b9b-43c4-93cc-d730c137cee8")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public TorusField()
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

    public void GetPreShaderCode(CodeAssembleContext cac, int inputIndex)
    {
        cac.Globals["fTorus"]
            = """
              float fTorus(float3 p, float3 center, float2 size) {
                  p = p - center;
                  float2 q = float2(length(p.xy) - size.x, p.z);
                  return length(q) - size.y;
              }
              """;
        
        var c = cac.ContextIdStack[^1];
        cac.AppendCall($"f{c}.w = fTorus(p{c}.xyz, {ShaderNode}Center, {ShaderNode}Size);");
        cac.AppendCall($"f{c}.xyz = p{c}.xyz;");
    }

    public void GetPostShaderCode(CodeAssembleContext cac, int inputIndex)
    {
    }

    [GraphParam]
    [Input(Guid = "dbc72bd7-6191-4145-a69f-d17b3808b3ab")]
    public readonly InputSlot<Vector3> Center = new();

    [GraphParam]
    [Input(Guid = "5fe2ab92-f8e5-400d-b5a3-197f20570d6f")]
    public readonly InputSlot<Vector2> Size = new();
}