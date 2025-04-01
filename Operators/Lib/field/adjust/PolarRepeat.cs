using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;

namespace Lib.field.adjust;

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

        var axis = Axis.GetEnumValue<AxisTypes>(context);
        
        var templateChanged = axis != _axis;
        if (!templateChanged)
            return;

        _axis = axis;
        ShaderNode.FlagCodeChanged();
    }

    public ShaderGraphNode ShaderNode { get; }

    public void GetPreShaderCode(CodeAssembleContext c, int inputIndex)
    {
        c.Globals["Common"] = ShaderGraphIncludes.Common;

        c.Globals["pModPolar"] = """
                                 // https://mercury.sexy/hg_sdf/
                                 void pModPolar(inout float2 p, float repetitions, float offset) {
                                     float angle = 2*PI/repetitions;
                                     float a = atan2(p.y, p.x) + angle/2. +  offset / (180 *PI);
                                     float r = length(p);
                                     float c = floor(a/angle);
                                     a = mod(a,angle) - angle/2.;
                                     p = float2(cos(a), sin(a))*r;
                                 }
                                 """;

        c.AppendCall($"pModPolar(p{c}.{_axisCodes0[(int)_axis]}, {ShaderNode}Repetitions, {ShaderNode}Offset);");
    }

    public void GetPostShaderCode(CodeAssembleContext cac, int inputIndex)
    {
    }

    private readonly string[] _axisCodes0 =
        [
            "zy",
            "zx",
            "yx",
        ];

    private AxisTypes _axis;

    private enum AxisTypes
    {
        X,
        Y,
        Z,
    }

    [Input(Guid = "7248C680-7279-4C1D-B968-3864CB849C77")]
    public readonly InputSlot<ShaderGraphNode> InputField = new();

    [Input(Guid = "02E4130F-8A0C-4EFB-B75F-F7DA29CC95EB", MappedType = typeof(AxisTypes))]
    public readonly InputSlot<int> Axis = new();

    [GraphParam]
    [Input(Guid = "b4c551a3-28c1-418a-83b4-ebdd61ed599c")]
    public readonly InputSlot<float> Repetitions = new();

    [GraphParam]
    [Input(Guid = "A0231B91-8AB8-4591-A3DA-3CD7F3980D2F")]
    public readonly InputSlot<float> Offset = new();
}