namespace Lib.field.generate;

[Guid("e589af25-4c03-4fcd-8ec3-0af77969f99c")]
internal sealed class LinearColorField : Instance<LinearColorField>
,IGraphNodeOp
{
    [Output(Guid = "E0EB3494-675D-4185-9799-8AB8893CB311")]
    public readonly Slot<Color2dField> Result = new();

    public LinearColorField()
    {
        _fieldNode = new Color2dField(this);
        
        Result.Value = _fieldNode;
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        ShaderNode.Update(context);
    }

    private readonly Color2dField _fieldNode;
    
    public ShaderGraphNode ShaderNode  => _fieldNode;
   
    public string GetShaderCode()
    { 
        return $@"
float4 {ShaderNode}(float2 uv) {{
    uv = uv - {ShaderNode}Center;
    //float2 t = {ShaderNode}Width;
    float2 q = float2(uv);
    return lerp({ShaderNode}ColorA, {ShaderNode}ColorB, length(q));
}}";
    }
    
    [GraphParam]
    [Input(Guid = "78B3B3F0-7CEB-4426-8C1C-90D23BCCB559")]
    public readonly InputSlot<Vector2> Center = new();
    
    [GraphParam]
    [Input(Guid = "d849ad46-d653-4f63-b149-53efc9978749")]
    public readonly InputSlot<Vector2> Width = new();
    
    [GraphParam]
    [Input(Guid = "E00571DE-B0C5-46E0-9A8F-53295D8674FC")]
    public readonly InputSlot<Vector4> ColorA = new();

    [GraphParam]
    [Input(Guid = "DB50797B-BC24-4C33-BC65-79C419D3D003")]
    public readonly InputSlot<Vector4> ColorB = new();
    
}

