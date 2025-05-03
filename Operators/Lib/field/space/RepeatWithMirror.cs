using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;

namespace Lib.field.space;

[Guid("58ff3790-5ec5-4113-8e37-a934c3a7fdcd")]
internal sealed class RepeatWithMirror : Instance<RepeatWithMirror>
                                       , IGraphNodeOp
{
    [Output(Guid = "ef5c19ad-0226-4391-a016-f6caee2ccf40")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public RepeatWithMirror()
    {
        ShaderNode = new ShaderGraphNode(this, null, InputField);
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
        c.Globals["Common"] = ShaderGraphIncludes.Common;

        c.Globals["pModMirror1"] = """
                                   // https://mercury.sexy/hg_sdf/
                                   // Same, but mirror every second cell so they match at the boundaries
                                   float pModMirror1(inout float p, float size) {
                                   	float halfsize = size*0.5;
                                   	float c = floor((p + halfsize)/size);
                                   	p = mod(p + halfsize,size) - halfsize;
                                   	p *= mod(c, 2.0)*2 - 1;
                                   	return c;
                                   }
                                   """;
    }

    public void GetPreShaderCode(CodeAssembleContext c, int inputIndex)
    {
        c.AppendCall($"pModMirror1(p{c}.{_axisCodes0[(int)_axis]}, {ShaderNode}Size);");
    }

    private readonly string[] _axisCodes0 =
        [
            "x",
            "y",
            "z",
        ];

    private AxisTypes _axis;

    private enum AxisTypes
    {
        X,
        Y,
        Z,
    }

    [Input(Guid = "6262f448-92fb-41ed-8613-2b387f2e72f2")]
    public readonly InputSlot<ShaderGraphNode> InputField = new();

    [Input(Guid = "a1b0fb9c-1e12-480c-8cf6-6bd7ed95f02e", MappedType = typeof(AxisTypes))]
    public readonly InputSlot<int> Axis = new();

    [GraphParam]
    [Input(Guid = "1c437dc9-aabf-404a-b23d-99ca677b6eb7")]
    public readonly InputSlot<float> Size = new();
}