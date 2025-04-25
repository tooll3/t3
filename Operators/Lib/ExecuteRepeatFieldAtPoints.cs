#nullable enable
using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;
using T3.Core.Utils.Geometry;

namespace Lib;

[Guid("296c774b-1cf0-4e37-9c22-7ac4fd5d78e5")]
internal sealed class ExecuteRepeatFieldAtPoints : Instance<ExecuteRepeatFieldAtPoints>
,IGraphNodeOp
{
    [Output(Guid = "b246c7f7-04dd-4632-aff8-fa0a2c03af4f")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public ExecuteRepeatFieldAtPoints()
    {
        ShaderNode = new ShaderGraphNode(this, null, InputField);

        Result.Value = ShaderNode;
        ShaderNode.AdditionalParameters = [new ShaderGraphNode.Parameter("float4x4", "Transform", Matrix4x4.Identity)];
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);

        var buffer = Points.GetValue(context);
        _count = buffer?.Srv?.Description != null ? buffer.Srv.Description.Buffer.ElementCount : 0;

        if (buffer != null && buffer.Srv != null)
        {
            _srv = buffer.Srv.IsDisposed ? null : buffer.Srv;
        }
        else
        {
            _srv = null;
        }
    }

    public ShaderGraphNode ShaderNode { get; }
    private int _count;
    private ShaderResourceView? _srv;

    void IGraphNodeOp.AddDefinitions(CodeAssembleContext c)
    {
        c.Globals["__Point__"] = """
                                 #include "shared/point.hlsl"
                                 """;
        
        c.Globals["PointMatrix"] = """
                                 struct PointTransform
                                 {
                                     float4x4 WorldToPointObject;
                                     float4 PointColor;
                                 };
                                 """;
    }

    bool IGraphNodeOp.TryBuildCustomCode(CodeAssembleContext c)
    {
        var fields = ShaderNode?.InputNodes;
        if (fields == null || fields.Count == 0)
            return true;

        var inputField = fields[0];
        //c.PushContext(0, "for");

        c.AppendCall($"float4 pKeep{c} = p{c};");
        c.AppendCall($"float4 fKeep{c} = float4(1,1,1,999);");
        c.AppendCall($"for(int i{c}=0; i{c}<{_count} && i{c}<100; i{c}++) {{");
        c.Indent();
        c.AppendCall($"p{c} = pKeep{c};");
        c.AppendCall($"f{c} = float4(1,1,1,9999);");
        c.AppendCall($"p{c}.xyz = mul(float4(pKeep{c}.xyz,1), {ShaderNode}PointTransforms[i{c}].WorldToPointObject).xyz;");
        //c.AppendCall($"p{c}.x += i{c};");
        inputField?.CollectEmbeddedShaderCode(c);
        
        //c.AppendCall($"f{c}.rgb *= {ShaderNode}PointTransforms[i{c}].PointColor.rgb;");
        //c.AppendCall($"fKeep{c}.rgb = lerp(f{c}.rgb, fKeep{c}.rgb, f{c}.w < fKeep{c}.w ? 0:1);");
        c.AppendCall($"fKeep{c}.w =   i{c} == 0 ? f{c}.w:  min(f{c}.w, fKeep{c}.w);");
        c.Unindent();
        c.AppendCall("}");
        c.AppendCall($"f{c} = fKeep{c};");
        //c.PopContext();
        //c.AppendCall($"f{c}.r *= 0.2;");
        
        return true;
    }

    void IGraphNodeOp.AppendShaderResources(ref List<ShaderGraphNode.SrvBufferReference> list)
    {
        if (_srv == null)
            return;

        // Skip if already added
        foreach (var x in list)
        {
            if (x.Srv == _srv)
                return;
        }

        list.Add(new ShaderGraphNode.SrvBufferReference($"StructuredBuffer<PointTransform> {ShaderNode}PointTransforms", _srv));
        //Log.Debug($"Add with length {_srv.Description.Buffer.ElementCount}  disposed:{_srv.IsDisposed}   check: {_srv.GetHashCode()}", this);
    }

    [Input(Guid = "bb4e6ad8-5941-4218-9e4b-4ba402be7ed4")]
    public readonly InputSlot<T3.Core.DataTypes.ShaderGraphNode> InputField = new();

    [Input(Guid = "1E5288D2-C2AE-4A1D-AD69-FE63D32A00C6")]
    public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();
}