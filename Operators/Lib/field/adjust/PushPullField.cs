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
        ShaderNode = new ShaderGraphNode(this, null, InputField);
        Result.Value = ShaderNode;
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);

    }

    public ShaderGraphNode ShaderNode { get; }

    public void GetPreShaderCode(CodeAssembleContext c, int inputIndex)
    {
    }

    public void GetPostShaderCode(CodeAssembleContext c, int inputIndex)
    {
        c.AppendCall($"f{c}.w += {ShaderNode}Offset;");
    }
    
    
    [Input(Guid = "c57d91b6-a26a-4e69-be1f-dc04f86594b6")]
    public readonly InputSlot<ShaderGraphNode> InputField = new();
    
    [GraphParam]
    [Input(Guid = "9cd1120d-81fa-43ab-b023-c462fe651ffb")]
    public readonly InputSlot<float> Offset = new();
}