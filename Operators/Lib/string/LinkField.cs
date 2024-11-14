using T3.Core.Utils;
using static Lib.Utils.HitFilmComposite;

namespace Lib.@string;

[Guid("27c6aa52-7b3e-4a9a-b6e5-0359f7155cee")]
internal sealed class LinkField : Instance<LinkField>
,IGraphNodeOp
{
    [Output(Guid = "1ac58dd8-4393-4eaa-955b-05204c18874b")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public LinkField()
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
        float le = {ShaderNode}Length;
        float r1 = {ShaderNode}RadiusA;
        float r2 = {ShaderNode}RadiusB;
        
        float3 q = float3( p.x, max(abs(p.y)-le,0.0), p.z );
        return length(float2(length(q.xy)-r1,q.z)) - r2;;
            
        }}";
    }
    
    [GraphParam]
    [Input(Guid = "02d27492-30c1-46b0-abc6-e0cd5af163d8")]
    public readonly InputSlot<Vector3> Center = new();
    
    [GraphParam]
    [Input(Guid = "4f0942b7-24ac-49e6-a9aa-21f0812c036b")]
    public readonly InputSlot<float> Length = new();

    [GraphParam]
    [Input(Guid = "f438eea9-ff20-482b-b5ba-c59fa189837b")]
    public readonly InputSlot<float> RadiusA = new();

    [GraphParam]
    [Input(Guid = "6ca362c6-25d6-4bee-8e6b-37c5638414df")]
    public readonly InputSlot<float> RadiusB = new();
    
   
}

