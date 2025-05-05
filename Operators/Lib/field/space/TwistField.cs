using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;

namespace Lib.field.space;

[Guid("D7C6E980-6766-4F83-A32C-677E3713E8A6")]
internal sealed class TwistField : Instance<TwistField>
                                 , IGraphNodeOp
{
    [Output(Guid = "0125A816-2885-4E83-97F9-357E24F8AFC3")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public TwistField()
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
        switch (_axis)
        {
            case AxisTypes.X:
                c.Globals["TwistX"] = """
                                      void opTwistX(inout float4 p, float k) {
                                          float c = cos(k * p.x);
                                          float s = sin(k * p.x);
                                          float2x2  m = float2x2(c,-s,s,c);
                                          p.yzx = float3(mul(m,p.yz), p.x);
                                      }
                                      """;
                break;
            case AxisTypes.Y:
                c.Globals["TwistY"] = """
                                      void opTwistY(inout float4 p, float k) {
                                          float c = cos(k * p.y);
                                          float s = sin(k * p.y);
                                          float2x2  m = float2x2(c,-s,s,c);
                                          p.xzy = float3(mul(m,p.xz), p.y);
                                      }
                                      """;
                break;
            case AxisTypes.Z:
                c.Globals["TwistZ"] = """
                                      void opTwistZ(inout float4 p, float k) {
                                          float c = cos(k * p.z);
                                          float s = sin(k * p.z);
                                          float2x2  m = float2x2(c,-s,s,c);
                                          p.xyz = float3(mul(m,p.xy), p.z);
                                      }
                                      """;
                break;
        }

        var twistOp = _axis switch
                          {
                              AxisTypes.X => "opTwistX",
                              AxisTypes.Y => "opTwistY",
                              AxisTypes.Z => "opTwistZ",
                              _           => throw new ArgumentOutOfRangeException()
                          };
        c.AppendCall($"{twistOp}(p{c}, {ShaderNode}Amount);");
    }

    public void GetPostShaderCode(CodeAssembleContext c, int inputIndex)
    {
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

    [Input(Guid = "84DD59CE-4726-4604-AFEE-92FFDEC0E3F5")]
    public readonly InputSlot<ShaderGraphNode> InputField = new();

    [Input(Guid = "195F4642-52F7-4C95-9127-07F6175549F5", MappedType = typeof(AxisTypes))]
    public readonly InputSlot<int> Axis = new();

    [GraphParam]
    [Input(Guid = "4935F569-8D23-495D-BB87-9A39E5B3981E")]
    public readonly InputSlot<float> Amount = new();
}