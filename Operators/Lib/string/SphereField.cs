using T3.Core.Utils;

namespace Lib.@string;

[Guid("fc2a33fc-d957-4113-8096-92d4dcbe14b5")]
internal sealed class SphereField : Instance<SphereField>
{
    [Output(Guid = "02f7d494-72ed-4247-88d7-0cbb730edf65")]
    public readonly Slot<FieldShaderDefinition> Result = new();

    public SphereField()
    {
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var fn = FieldShaderDefinition.BuildInstanceId(this);
        var sd = FieldShaderDefinition.GetOrCreateDefinition(context, fn);
        Result.Value = sd;
        
        sd.KeepVec3Parameter("Center", Center.GetValue(context), fn);
        sd.KeepScalarParameter("Radius", Radius.GetValue(context), fn);
        
        sd.AppendLineToShaderDef($"float {fn}(float3 p) {{");
        sd.AppendLineToShaderDef($"    return length(p - {fn}Center) / {fn}Radius;");
        sd.AppendLineToShaderDef("}");
        
        sd.CollectedFeatureIds.Add(fn);
    }
    
    
    [Input(Guid = "CA582E39-37D7-4DF6-B942-E2330F2BF2C6")]
    public readonly InputSlot<Vector3> Center = new();
    
    [Input(Guid = "3DD7C779-7982-4E7C-B4CE-F1915F477AD0")]
    public readonly InputSlot<float> Radius = new(); 
}

