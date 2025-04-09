using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;

namespace Lib.field.adjust;

[Guid("0ff18a32-c1e9-406d-b3e3-23700e2453a0")]
internal sealed class PushPullField : Instance<PushPullField>
,IGraphNodeOp
{
    [Output(Guid = "ffbb6690-d57e-4dea-8aa7-fbf0101b9c96")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public PushPullField()
    {
        ShaderNode = new ShaderGraphNode(this, null, SdfField, AmountField);
        Result.Value = ShaderNode;
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);

    }

    public ShaderGraphNode ShaderNode { get; }

    bool IGraphNodeOp.TryBuildCustomCode(CodeAssembleContext c)
    {
        var subContextIndex = c.ContextIdStack.Count;

        var inputNodeIndex = 0;
        
        // Return base distance (required for blending)
        if (SdfField.HasInputConnections)
        {
            ShaderNode.InputNodes[inputNodeIndex]?.CollectEmbeddedShaderCode(c);
            inputNodeIndex++;
        }
        //c.AppendCall($"f{c}.w += {ShaderNode}Offset;");
        

        c.AppendCall("{");
        c.Indent();

        if (AmountField.HasInputConnections)
        {
            c.PushContext(subContextIndex, "amount");
            var subContextId = c.ToString();
            ShaderNode.InputNodes[inputNodeIndex]?.CollectEmbeddedShaderCode(c);
            c.PopContext();
            c.AppendCall($"f{c}.w += f{subContextId}.r *{ShaderNode}Amount;");
            c.AppendCall($"f{c}.w /= 1 + {ShaderNode}StepScale;");
        }
        else
        {
            c.AppendCall($"f{c}.w += {ShaderNode}Amount;");
        }
        
        c.Unindent();
        c.AppendCall("}");

        return true;
    }
    
    
    [Input(Guid = "c57d91b6-a26a-4e69-be1f-dc04f86594b6")]
    public readonly InputSlot<ShaderGraphNode> SdfField = new();

    [Input(Guid = "91C09FAC-0B50-4F62-97CA-18461801B442")]
    public readonly InputSlot<ShaderGraphNode> AmountField = new();
    
    [GraphParam]
    [Input(Guid = "9cd1120d-81fa-43ab-b023-c462fe651ffb")]
    public readonly InputSlot<float> Amount = new();
    
    [GraphParam]
    [Input(Guid = "463149FA-B53B-4D43-B4E7-57DCB045BBA0")]
    public readonly InputSlot<float> StepScale = new();    
}