using System.Diagnostics;
using T3.Core.DataTypes.ShaderGraph;
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
        var combineMethod = CombineMethod.GetEnumValue<CombineMethods>(context);
        if (combineMethod != _combineMethod)
        {
            _combineMethod = combineMethod;
            ShaderNode.FlagCodeChanged();
        }
        
        ShaderNode.Update(context);

        // Get all parameters to clear operator dirty flag
        InputFields.DirtyFlag.Clear();
    }

    public void GetPreShaderCode(CodeAssembleContext cac, int inputIndex)
    {
        // Register global method
        switch (_combineMethod)
        {
            case CombineMethods.SmoothUnion:
                cac.Globals["fOpSmoothUnion"] = """
                                                float fOpSmoothUnion(float a, float b, float k) {
                                                    float h = max(k - abs(a - b), 0.0);
                                                    return min(a, b) - (h * h) / (4.0 * k);
                                                };
                                                """;
                break;
        }
    }
    
    
    public void GetPostShaderCode(CodeAssembleContext cac, int inputIndex)
    {
        // Just pass along subcontext if not enough connected fields...
        if ( ShaderNode.InputNodes.Count <= 1)
        {
            cac.AppendCall("// skipping combine with single or no input...");
            return;
        }
        
        Debug.Assert(cac.ContextIdStack.Count>=2);
        
        var contextId = cac.ContextIdStack[^2];
        var subContextId = cac.ContextIdStack[^1];
        
        if (inputIndex == 0)
        {
            // Keep initial value
            cac.AppendCall($"f{contextId} = f{subContextId};");
        }
        else
        {
            // Combine initial value with new value...
            switch (_combineMethod)
            {
                case CombineMethods.Add:
                    cac.AppendCall($"f{contextId}.w += f{subContextId}.w;");
                    break;
                case CombineMethods.Sub:
                    cac.AppendCall($"f{contextId}.w -=  f{subContextId}.w;");
                    break;
                case CombineMethods.Multiply:
                    cac.AppendCall($"f{contextId}.w *= f{subContextId}.w;");
                    break;
                case CombineMethods.Min:
                    cac.AppendCall($"f{contextId}.w = min(f{contextId}.w, f{subContextId}.w);");
                    break;
                case CombineMethods.Max:
                    cac.AppendCall($"f{contextId}.w = max(f{contextId}.w, f{subContextId}.w);");
                    break;
                case CombineMethods.SmoothUnion:
                    cac.AppendCall($"f{contextId}.w = fOpSmoothUnion(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.CutOut:
                    cac.AppendCall($"f{contextId}.w = max(f{contextId}.w, -f{subContextId}.w);");
                    break;
                case CombineMethods.TestBlend:
                    cac.AppendCall($"f{contextId}.w = lerp(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            cac.AppendCall($"f{contextId}.xyz = f{contextId}.w < f{subContextId}.w ? f{contextId}.xyz : f{subContextId}.xyz;");
        }
    }
    
    public ShaderGraphNode ShaderNode { get; }

    private CombineMethods _combineMethod;
    
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