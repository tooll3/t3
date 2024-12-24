using T3.Core.Utils;
using static Lib.Utils.HitFilmComposite;

namespace Lib.field.generate;

[Guid("a54e0946-71d0-4985-90bc-184cdb1b6b34")]
internal sealed class TorusField : Instance<TorusField>
,IGraphNodeOp
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
   /* float sdTorus(float3 p, float2 t)
    {
        float2 q = float2(length(p.xz) - t.x, p.y);
        return length(q) - t.y;
    }*/
    public string GetShaderCode()
    { 
        return $@"
        float {ShaderNode}(float3 p) {{
            p = p - {ShaderNode}Center;
            float2 t = {ShaderNode}Size;
            float2 q = float2(length(p.xz) - t.x, p.y);
            return length(q) - t.y;
        }}";
    }
    
    [GraphParam]
    [Input(Guid = "dbc72bd7-6191-4145-a69f-d17b3808b3ab")]
    public readonly InputSlot<Vector3> Center = new();
    
    [GraphParam]
    [Input(Guid = "5fe2ab92-f8e5-400d-b5a3-197f20570d6f")]
    public readonly InputSlot<Vector2> Size = new();
    
   
}

