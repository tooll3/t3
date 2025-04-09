using T3.Core.Utils;

namespace Lib.field.generate;

[Guid("a54e0946-71d0-4985-90bc-184cdb1b6b34")]
internal sealed class TorusField : Instance<TorusField>
                                 , IGraphNodeOp
{
    [Output(Guid = "14cd4d1f-0b9b-43c4-93cc-d730c137cee8")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public TorusField()
    {
        ShaderNode = new ShaderGraphNode(this);
        Result.Value = ShaderNode;
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);
        
        var axis = Axis.GetEnumValue<AxisTypes>(context);
        
        var templateChanged = axis != _axis;
        if (!templateChanged)
            return;

        _axis = axis;
        ShaderNode.FlagCodeChanged();      
    }

    public ShaderGraphNode ShaderNode { get; }

    void IGraphNodeOp.AddDefinitions(CodeAssembleContext c)
    {
        c.Globals["fTorus"]
            = """
              float fTorus(float3 p, float2 size) {
                  float2 q = float2(length(p.xy) - size.x, p.z);
                  return length(q) - size.y;
              }
              """;
    }

    public void GetPreShaderCode(CodeAssembleContext c, int inputIndex)
    {
        var a = _axisCodes0[(int)_axis];

        c.AppendCall($"f{c}.w = fTorus(p{c}.{a} - {ShaderNode}Center.{a} , {ShaderNode}Size);");
        c.AppendCall($"f{c}.xyz = p.w < 0.5 ?  p{c}.xyz : 1;"); // save local space
    }
    
    private readonly string[] _axisCodes0 =
        [
            "yzx",
            "xzy",
            "xyz",
        ];

    private AxisTypes _axis;

    private enum AxisTypes
    {
        X,
        Y,
        Z,
    }
    
    [Input(Guid = "522A9640-CA8C-47E6-AD36-5C316A9092AE", MappedType = typeof(AxisTypes))]
    public readonly InputSlot<int> Axis = new();

    
    [GraphParam]
    [Input(Guid = "dbc72bd7-6191-4145-a69f-d17b3808b3ab")]
    public readonly InputSlot<Vector3> Center = new();

    [GraphParam]
    [Input(Guid = "5fe2ab92-f8e5-400d-b5a3-197f20570d6f")]
    public readonly InputSlot<Vector2> Size = new();
}