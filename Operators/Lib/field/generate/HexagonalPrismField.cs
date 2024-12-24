using T3.Core.Utils;
using static Lib.Utils.HitFilmComposite;

namespace Lib.field.generate;


[Guid("1e1e65cd-3564-45e1-88f8-6cb4b4b18c5a")]
internal sealed class HexagonalPrismField : Instance<HexagonalPrismField>
,IGraphNodeOp
{
    [Output(Guid = "e3d6161a-68bd-41e6-882c-5092a61fc449")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public HexagonalPrismField()
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
        float2 h = {ShaderNode}RadiusLength;
              
        const float3 k = float3(-0.8660254, 0.5, 0.57735);

        p = abs(p);
        p.xy -= 2.0*min(dot(k.xy, p.xy), 0.0)*k.xy;
        float2 d = float2(length(p.xy-float2(clamp(p.x,-k.z*h.x,k.z*h.x), h.x))*sign(p.y-h.x),p.z-h.y );
        return min(max(d.x,d.y),0.0) + length(max(d,0.0))-{ShaderNode}Round;
            
        }}";
    }
    
    [GraphParam]
    [Input(Guid = "cfc6f8f3-94b4-46f6-9501-11d7b7916bb7")]
    public readonly InputSlot<Vector3> Center = new();
    
    [GraphParam]
    [Input(Guid = "a87ec8e0-ad88-45f4-8ce4-33baad9ab1a9")]
    public readonly InputSlot<Vector2> RadiusLength = new();
   
    [GraphParam]
    [Input(Guid = "356E85DD-BF81-4A1C-96A4-1D2983916A4B")]
    public readonly InputSlot<float> Round = new();



}

