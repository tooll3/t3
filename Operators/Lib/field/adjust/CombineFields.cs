using T3.Core.Utils;
// ReSharper disable UnusedMember.Local

namespace Lib.field.adjust;

[Guid("82270977-07b5-4d86-8544-5aebc638d46c")]
internal sealed class CombineFields : Instance<CombineFields>, IGraphNodeOp
{
    [Output(Guid = "db0bbde0-18b6-4c53-8cf7-a294177d2089")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public CombineFields()
    {
        ShaderNode = new ShaderGraphNode(this, InputFields);
        Result.UpdateAction += Update;
        Result.Value = ShaderNode;
    }

    private void Update(EvaluationContext context)
    {
        // Get all parameters to clear operator dirty flag
        var combineMethod = CombineMethod.GetEnumValue<CombineMethods>(context);
        if (combineMethod != _combineMethod)
        {
            _combineMethod = combineMethod;
            ShaderNode.FlagCodeChanged();
        }
        
        ShaderNode.Update(context);

        InputFields.DirtyFlag.Clear();
    }
    
    

    public void GetShaderCode(StringBuilder _callDef, Dictionary<string, string> globals)
    {
        //_callDef.Clear();

        if ( ShaderNode.InputNodes.Count == 0)
        {
            _callDef.AppendLine($"float {ShaderNode}(float3 p) {{ return -999999; }}");
        }

        else if (ShaderNode.InputNodes.Count == 1)
        {
            _callDef.AppendLine($"float {ShaderNode}(float3 p) {{ return {ShaderNode.InputNodes[0]}(p); }}");
        }
        else
        {
            // Define combine method
            var mode = _combineModes[(int)_combineMethod];
            if (_combineMethod == CombineMethods.SmoothUnion)
            {
                _callDef.AppendLine($@"
inline float {ShaderNode}CombineFunc(float d1, float d2) {{
    float k = {ShaderNode}K;
    float h = max(k - abs(d1 - d2), 0.0);
    return min(d1, d2) - (h * h) / (4.0 * k);
}}
");
            }
            else if (_combineMethod == CombineMethods.TestBlend)
            {
                _callDef.AppendLine($@"
inline float {ShaderNode}CombineFunc(float d1, float d2) {{
    float k = {ShaderNode}K;
    return lerp(d1,d2,k);    
}}
");
            }
            else
            {
                _callDef.AppendLine($"#define {ShaderNode}CombineFunc(a,b) ({mode.Code})\n");
            }
            
            _callDef.AppendLine("");
            
            // Combine all input fields
            _callDef.AppendLine($"inline float {ShaderNode}(float3 p) {{");
            _callDef.AppendLine($"    float d={ShaderNode.InputNodes[0]}(p);");

            for (var index = 1; index < ShaderNode.InputNodes.Count; index++)
            {
                var inputNode = ShaderNode.InputNodes[index];
                _callDef.AppendLine($"    d = {ShaderNode}CombineFunc(d,  {inputNode}(p));");
            }

            _callDef.AppendLine("    return d;");
            _callDef.AppendLine("}");
        }
    }


    public ShaderGraphNode ShaderNode { get; }
    

    private CombineMethods _combineMethod;
    //private readonly StringBuilder _callDef = new();

    private sealed record CombineMethodDefs(string Code, float StartValue);

    private readonly CombineMethodDefs[] _combineModes =
        [
            new("(a) + (b)", 0),
            new("(a) - (b)", 0),
            new("(a) * (b)", 1),
            new("min(a, b)", 999999),
            new("max(a, b)", -999999),
            new("SmoothUnion(a, b)", 999999),
            new("max(a,-b)", 999999),
            new("TestBlend(a,b)", 999999),
        ];

    private enum CombineMethods
    {
        Add,
        Sub,
        Multiply,
        Min,
        Max,
        SmoothUnion,
        CutOut,
        TestBlend,
    }

    [Input(Guid = "7248C680-7279-4C1D-B968-3864CB849C77")]
    public readonly MultiInputSlot<ShaderGraphNode> InputFields = new();

    [GraphParam]
    [Input(Guid = "9E4F5916-722D-4C4B-B1CA-814958A5B836")]
    public readonly InputSlot<float> K = new();

    [Input(Guid = "4648E514-B48C-4A98-A728-3EBF9BCFA0B7", MappedType = typeof(CombineMethods))]
    public readonly InputSlot<int> CombineMethod = new();
}