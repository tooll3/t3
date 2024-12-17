using T3.Core.Utils;
using static Lib.Utils.HitFilmComposite;

namespace Lib.@string;

[Guid("c7ef5f64-2654-47a8-a2ab-30b28446b934")]
internal sealed class BendField : Instance<BendField>
,IGraphNodeOp
{
    [Output(Guid = "b4427b7d-f2b8-433f-af97-14c0181fb3d6")]
    public readonly Slot<ShaderGraphNode> Result = new();
    
    public BendField()
    {
        ShaderNode = new ShaderGraphNode(this, null, InputField);
        Result.Value = ShaderNode;
        Result.UpdateAction += Update;
    }
    
    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);
        
        _inputFn= ShaderNode.InputNodes.Count == 1
                      ? ShaderNode.InputNodes[0].ToString()
                      : string.Empty;
        
    }
    
    public ShaderGraphNode ShaderNode { get; }
    
    public string GetShaderCode()
    {
// Repeat around the origin by a fixed angle.
// For easier use, num of repetitions is use to specify the angle.
            
            return $@"

#ifndef PI
#define PI 3.14159265359f
#endif

#ifndef mod
#define mod(x, y) ((x) - (y) * floor((x) / (y)))
#endif
    
        float {ShaderNode}(float3 p) 
        {{
float k = {ShaderNode}Amount; // or some other amount
float c = cos(k*p.x);
float s = sin(k*p.x);
float2x2  m = float2x2(c,-s,s,c);
float3  q = float3( mul(m,p.xy),p.z);
return {_inputFn}(q);
        }}
        ";
    }
    
    private string _inputFn;
    
    [Input(Guid = "adaf8efd-47b3-4d4b-9102-d8a3c6a7e34a")]
    public readonly InputSlot<ShaderGraphNode> InputField = new();    
    
    [GraphParam]
    [Input(Guid = "c0490245-8f7c-4972-8ded-736883b4e650")]
    public readonly InputSlot<float> Amount = new();


}

