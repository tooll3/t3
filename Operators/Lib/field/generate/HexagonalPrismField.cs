using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;

namespace Lib.field.generate;

[Guid("1e1e65cd-3564-45e1-88f8-6cb4b4b18c5a")]
internal sealed class HexagonalPrismField : Instance<HexagonalPrismField>
                                          , IGraphNodeOp
{
    [Output(Guid = "e3d6161a-68bd-41e6-882c-5092a61fc449")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public HexagonalPrismField()
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

    public void GetPreShaderCode(CodeAssembleContext c, int inputIndex)
    {
        c.Globals["fHexPrism"] = """
                                 // h is radius and length
                                 float fHexPrism(float3 p, float2 h, float round) { 
                                     const float3 k = float3(-0.8660254, 0.5, 0.57735);
                                 
                                     p = abs(p);
                                     p.xy -= 2.0*min(dot(k.xy, p.xy), 0.0)*k.xy;
                                     float2 d = float2(length(p.xy-float2(clamp(p.x,-k.z*h.x,k.z*h.x), h.x))*sign(p.y-h.x),p.z-h.y );
                                     return min(max(d.x,d.y),0.0) + length(max(d,0.0))-round;
                                 };
                                 """;

        var n = ShaderNode;
        var a = _axisCodes0[(int)_axis];
        c.AppendCall($"f{c}.w = fHexPrism(p{c}.{a} - {n}Center.{a}, {n}RadiusLength, {n}Round);");
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
    
    [Input(Guid = "522A9640-CA8C-47E6-AD36-5C316A9092AE", MappedType = typeof(AxisTypes))]
    public readonly InputSlot<int> Axis = new();


    
    [GraphParam]
    [Input(Guid = "cfc6f8f3-94b4-46f6-9501-11d7b7916bb7")]
    public readonly InputSlot<Vector3> Center = new();

    [GraphParam]
    [Input(Guid = "a87ec8e0-ad88-45f4-8ce4-33baad9ab1a9")]
    public readonly InputSlot<Vector2> RadiusLength = new();

    [GraphParam]
    [Input(Guid = "356E85DD-BF81-4A1C-96A4-1D2983916A4B")]
    public readonly InputSlot<float> Round = new();
}