using T3.Core.Utils;
using static Lib.Utils.HitFilmComposite;

namespace Lib.@string;

[Guid("1d9f133d-eba6-4b28-9dfd-08f6c5417ed6")]
internal sealed class PolarRepeat : Instance<PolarRepeat>
                                  , IGraphNodeOp
{
    [Output(Guid = "de78d5d8-b232-44f6-ab18-cc765f81eb38")]
    public readonly Slot<ShaderGraphNode> Result = new();
    
    public PolarRepeat()
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
            float angle = 2 * PI / {ShaderNode}Repetitions;
            float a = atan2(p.y, p.x) + angle / 2.;
            float r = length(p);
            float c = floor(a / angle);
            a = mod(a, angle) - angle / 2.;
            p = float3(float2(cos(a), sin(a)) * r, p.z);
           //p.z += {ShaderNode}Repetitions;

            return {_inputFn}(p);                
        }}
        ";
    }
    
    private string _inputFn;
    
    [Input(Guid = "7248C680-7279-4C1D-B968-3864CB849C77")]
    public readonly InputSlot<ShaderGraphNode> InputField = new();    
    
    [GraphParam]
    [Input(Guid = "b4c551a3-28c1-418a-83b4-ebdd61ed599c")]
    public readonly InputSlot<float> Repetitions = new();


}

