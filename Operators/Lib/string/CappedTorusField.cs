using T3.Core.Utils;
using static Lib.Utils.HitFilmComposite;

namespace Lib.@string;

[Guid("b85198a8-6475-45e1-b2d2-83ec8f59d6ed")]
internal sealed class CappedTorusField : Instance<CappedTorusField>
,IGraphNodeOp
{
    [Output(Guid = "170b545f-c7c0-47d9-8b19-d8d8e8a3d2fa")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public CappedTorusField()
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

    public string GetShaderCode()
    { 
        return $@"
        float {ShaderNode}(float3 p) {{
        p = p - {ShaderNode}Center;
        float ra = {ShaderNode}RadiusA;
        float rb = {ShaderNode}RadiusB;
        
        float an = 2.5*(0.5+0.5*({ShaderNode}Size*1.1+3.0));
        float2 sc = float2(sin(an),cos(an));

        p.x = abs(p.x);
        float k = (sc.y*p.x>sc.x*p.y) ? dot(p.xy,sc) : length(p.xy);
        return sqrt(dot(p,p) + ra*ra - 2.0*ra*k ) - rb;
            
        }}";
    }
    
    [GraphParam]
    [Input(Guid = "f5a24e80-741a-49c5-9a0b-d7074e80940a")]
    public readonly InputSlot<Vector3> Center = new();
    
    [GraphParam]
    [Input(Guid = "d37914dd-7bbd-47fa-ad90-9921621afb9e")]
    public readonly InputSlot<float> Size = new();

    [GraphParam]
    [Input(Guid = "00cec9de-3be4-403f-8677-dcc2ef473708")]
    public readonly InputSlot<float> RadiusA = new();

    [GraphParam]
    [Input(Guid = "e1dc5cdc-24d2-409f-9ea6-6dd14fdb70d3")]
    public readonly InputSlot<float> RadiusB = new();
    
   
}

