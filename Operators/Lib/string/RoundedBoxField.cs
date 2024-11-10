using T3.Core.Utils;

namespace Lib.@string;

[Guid("860da1cd-b341-4bc5-965a-4a9c295831f4")]
internal sealed class RoundedBoxField : Instance<RoundedBoxField>
{
    [Output(Guid = "9153c53c-0b19-4ce4-b086-e448d78ef032")]
    public readonly Slot<FieldShaderGraph> Result = new();

    public RoundedBoxField()
    {
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var fn = FieldShaderGraph.BuildNodeId(this);
        var sd = FieldShaderGraph.GetOrCreateDefinition(context, fn);
        Result.Value = sd;
        
        sd.KeepVec3Parameter("Center", Center.GetValue(context), fn);
        sd.KeepVec3Parameter("Size", Size.GetValue(context), fn);
        sd.KeepScalarParameter("Radius", Radius.GetValue(context), fn);
        
        sd.AppendLineToShaderCode($"float {fn}(float3 p) {{");
        sd.AppendLineToShaderCode($"   float3 q = abs(p- {fn}Center) - {fn}Size + {fn}Radius;");
        sd.AppendLineToShaderCode($"   return length(max(q,0.0)) + min(max(q.x,max(q.y,q.z)),0.0) - {fn}Radius;");
        sd.AppendLineToShaderCode("}");
        
        
        // return saturate( ({Radius} / {FallOff}) - (length(p - {Center}) / {FallOff}) + 0.5 ); 

        sd.CollectedFeatureIds.Add(fn);
    }
    
    
    [Input(Guid = "951b2983-1359-41e4-8fb0-8d97c50ed8d6")]
    public readonly InputSlot<Vector3> Center = new();
    
    [Input(Guid = "C4EF07B4-853B-48D4-9ADE-C93EE849071A")]
    public readonly InputSlot<Vector3> Size = new();
    
    [Input(Guid = "787e5d70-0aba-400f-8616-6ece6c5895bc")]
    public readonly InputSlot<float> Radius = new(); 
    
}

