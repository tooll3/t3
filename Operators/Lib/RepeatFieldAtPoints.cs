#nullable enable
using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;
using T3.Core.Utils.Geometry;

namespace Lib;

[Guid("296c774b-1cf0-4e37-9c22-7ac4fd5d78e5")]
internal sealed class RepeatFieldAtPoints : Instance<RepeatFieldAtPoints>
                                          , IGraphNodeOp
{
    [Output(Guid = "b246c7f7-04dd-4632-aff8-fa0a2c03af4f")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public RepeatFieldAtPoints()
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
        _srv = buffer?.Srv is { IsDisposed: false }
                   ? buffer.Srv
                   : null;
    }

    public ShaderGraphNode ShaderNode { get; }
    private int _count;
    private ShaderResourceView? _srv;

    bool IGraphNodeOp.TryBuildCustomCode(CodeAssembleContext c)
    {
        var fields = ShaderNode?.InputNodes;
        if (fields == null || fields.Count == 0)
            return true;

        var inputField = fields[0];

        c.AppendCall($"float4 pKeep{c} = p{c};");
        c.AppendCall($"float4 fKeep{c} = 99999;");
        c.AppendCall($"for(int {c}i=0; {c}i<{_count} && {c}i<100; i++) {{");
        c.Indent();
        c.AppendCall($"p{c} = pKeep{c};");
        c.AppendCall($"p{c}.x += {c}i;");
        inputField?.CollectEmbeddedShaderCode(c);
        c.AppendCall($"fKeep{c}.w = min(f{c}.w, fKeep{c}.w);");
        c.Unindent();
        c.AppendCall("}");
        c.AppendCall($"f{c}.w = fKeep{c}.w;");
        return true;
    }

    void IGraphNodeOp.AppendShaderResources(ref List<ShaderGraphNode.SrvBufferReference> list)
    {
        if (_srv == null)
            return;

        foreach (var x in list)
        {
            if (x.Srv == _srv)
                return;
        }

        Log.Debug($"Adding point resource on {list.Count} ", this);
        list.Add(new ShaderGraphNode.SrvBufferReference("Points", _srv));
    }

    [Input(Guid = "bb4e6ad8-5941-4218-9e4b-4ba402be7ed4")]
    public readonly InputSlot<T3.Core.DataTypes.ShaderGraphNode> InputField = new InputSlot<T3.Core.DataTypes.ShaderGraphNode>();

    [Input(Guid = "1E5288D2-C2AE-4A1D-AD69-FE63D32A00C6")]
    public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();
}