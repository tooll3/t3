using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;

namespace Lib;

[Guid("88926602-4694-4632-9fd7-04c8d6ddd728")]
internal sealed class SetSDFMaterial : Instance<SetSDFMaterial>
,IGraphNodeOp
{
    [Output(Guid = "51c8b9dd-9798-44e6-a9d0-0baecfc9c9a5")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public SetSDFMaterial()
    {
        ShaderNode = new ShaderGraphNode(this, null, InputField);
        Result.Value = ShaderNode;
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);

        // var axis = Axis.GetEnumValue<AxisTypes>(context);
        //
        // var templateChanged = axis != _axis;
        // if (!templateChanged)
        //     return;
        //
        // _axis = axis;
        // ShaderNode.FlagCodeChanged();
    }

    public ShaderGraphNode ShaderNode { get; }

    public void GetPreShaderCode(CodeAssembleContext c, int inputIndex)
    {
        c.Definitions.AppendLine( $$"""
                                 
                                 float4 {{ShaderNode}}fMaterial(float4 pc, float4 f) 
                                 {
                                    if(pc.w < 0.5)
                                        return f;
                                        
                                    //if(pc.w < 1.5) {
                                    return float4({{ShaderNode}}Color.rgb, f.w);
                                    //}
                                 }
                                 """);

    }

    public void GetPostShaderCode(CodeAssembleContext c, int inputIndex)
    {
        c.AppendCall($"f{c} = {ShaderNode}fMaterial(p{c}, f{c});");
    }
    
    [Input(Guid = "7c656067-ef12-4990-b094-7f8160a242d1")]
    public readonly InputSlot<ShaderGraphNode> InputField = new();
    
    [GraphParam]
    [Input(Guid = "D2A64234-C7EF-424B-822C-ADDCE0B84E69")]
    public readonly InputSlot<Vector4> Color = new();
}