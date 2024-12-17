using T3.Core.Utils;

namespace Lib.@string;

[Guid("933aec2c-e7dd-44b4-a094-14d0195f9f7a")]
internal sealed class FractalSDF : Instance<FractalSDF>
,IGraphNodeOp
{
    [Output(Guid = "095c8c2a-4d5a-4aac-8ff9-b696ed14406d")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public FractalSDF()
    {
        ShaderNode = new ShaderGraphNode(this);
        Result.Value = ShaderNode;
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var iterations = Iterations.GetValue(context).Clamp(1,20);
        if (iterations != _iterations)
        {
            _iterations=iterations;
            ShaderNode.HasChangedCode = true;
        }
        
        ShaderNode.Update(context);
        
        
    }
    
    public ShaderGraphNode ShaderNode { get; }
    public string GetShaderCode()
    { 
        return @$"
static int mandelBoxIterations = {_iterations};

inline float {ShaderNode}(float3 pos) 
{{
    float4 pN = float4(pos, 1);
    // return dStillLogo(pN);

    // precomputed constants
    float minRad2 = clamp({ShaderNode}Minrad, 1.0e-9, 1.0);
    float4 scale = float4({ShaderNode}Scale, {ShaderNode}Scale, {ShaderNode}Scale, abs({ShaderNode}Scale)) / minRad2;
    float absScalem1 = abs({ShaderNode}Scale - 1.0);
    float AbsScaleRaisedTo1mIters = pow(abs({ShaderNode}Scale), float(1 - mandelBoxIterations));
    //float DIST_MULTIPLIER = StepSize;

    float4 p = float4(pos, 1);
    float4 p0 = p; // p.w is the distance estimate

    for (int i = 0; i < mandelBoxIterations; i++)
    {{
        // box folding:
        p.xyz = abs(1 + p.xyz) - p.xyz - abs(1.0 - p.xyz);                 // add;add;abs.add;abs.add (130.4%)
        p.xyz = clamp(p.xyz, {ShaderNode}Clamping.x, {ShaderNode}Clamping.y) * {ShaderNode}Clamping.z - p.xyz; // min;max;mad

        // sphere folding: if (r2 < minRad2) p /= minRad2; else if (r2 < 1.0) p /= r2;
        float r2 = dot(p.xyz, p.xyz);
        p *= clamp(max(minRad2 / r2, minRad2), {ShaderNode}Fold.x, {ShaderNode}Fold.y); // dp3,div,max.sat,mul
        p.xyz += float3({ShaderNode}Increment.x, {ShaderNode}Increment.y, {ShaderNode}Increment.z);
        // scale, translate
        p = p * scale + p0;
    }}
    float d = ((length(p.xyz) - absScalem1) / p.w - AbsScaleRaisedTo1mIters);
    return d;
}}
";
    }

    private int _iterations = 5;

    [GraphParam]
    [Input(Guid = "482FF1B2-2383-45DB-8A5A-30EF2A07C8FB")]
    public readonly InputSlot<Vector3> Clamping = new();
    
    [GraphParam]
    [Input(Guid = "5678312C-5C31-4B7E-9E82-4595AA8A6740")]
    public readonly InputSlot<float> Scale = new();

    [GraphParam]
    [Input(Guid = "CEF65141-085C-4E09-8F16-8338B0964BC4")]
    public readonly InputSlot<Vector3> Increment = new();


    [GraphParam]
    [Input(Guid = "AB32B4F6-DEA8-4E25-B5CD-2E232273A51E")]
    public readonly InputSlot<float> Minrad = new();

    [GraphParam]
    [Input(Guid = "5DC2517B-228C-45A7-ABE5-B87275EB5224")]
    public readonly InputSlot<Vector2> Fold = new();

    
    [Input(Guid = "094637CF-9526-49D5-BB9F-19991CDCC516")]
    public readonly InputSlot<int> Iterations = new();
    
}
