using T3.Core.Utils;
using T3.Core.Utils.Geometry;

namespace Lib.@string;

[Guid("c44d23c7-bfac-403d-b49e-49d00001a316")]
internal sealed class TransformField : Instance<TransformField>
{
    [Output(Guid = "9b12e766-9dcd-4c8f-83ee-2a0b78beae43")]
    public readonly Slot<FieldShaderGraph> Result = new();

    public TransformField()
    {
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        // Setup feature
        var fn = FieldShaderGraph.BuildNodeId(this);
        var sd = FieldShaderGraph.GetOrCreateDefinition(context, fn);
        Result.Value = sd;
        
        // Get connected field
        var connectedField = InputField.GetValue(context);
        if (connectedField == null)
        {
            sd.HasErrors = true;
            sd.CollectedFeatureIds.Add(fn);
            return;
        }
        
        // Get parameters 
        var s = Scale.GetValue(context) * UniformScale.GetValue(context);
        var r = Rotation.GetValue(context);
        float yaw = r.Y.ToRadians();
        float pitch =r.X.ToRadians();
        float roll = r.Z.ToRadians();
        var pivot = Pivot.GetValue(context);
        var t = Translation.GetValue(context);
        var objectToParentObject = GraphicsMath.CreateTransformationMatrix(scalingCenter: pivot, 
                                                                           scalingRotation: Quaternion.Identity, 
                                                                           scaling: new Vector3(s.X, s.Y, s.Z), 
                                                                           rotationCenter: pivot,
                                                                           rotation: Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll), 
                                                                           translation: new Vector3(t.X, t.Y, t.Z));

        var shearing = Shear.GetValue(context);
        
        Matrix4x4 m = Matrix4x4.Identity;
        m.M12=shearing.Y; 
        m.M21=shearing.X; 
        m.M13=shearing.Z;             
        objectToParentObject = Matrix4x4.Multiply(objectToParentObject,m);
            
        // transpose all as mem layout in hlsl constant buffer is row based
        objectToParentObject.Transpose();
        
        // Set parameters to shader definition
        sd.KeepMatrixParameter("Transform", objectToParentObject, fn);
        
        // Create shader function
        var inputFn = connectedField.CollectedFeatureIds[^1];
        sd.AppendLineToShaderCode($"float {fn}(float3 pos) {{");
        sd.AppendLineToShaderCode($"    return {inputFn}(mul(float4(pos.xyz,1), {fn}Transform).xyz);");
        sd.AppendLineToShaderCode("}");
        
        sd.CollectedFeatureIds.Add(fn);
    }
    
    [Input(Guid = "3B817E6C-F532-4A8C-A2FF-A00DC926EEB2")]
    public readonly InputSlot<Vector3> Translation = new();
        
    [Input(Guid = "5339862D-5A18-4D0C-B908-9277F5997563")]
    public readonly InputSlot<Vector3> Rotation = new();
        
    [Input(Guid = "58B9DFB6-0596-4F0D-BAF6-7FB3AE426C94")]
    public readonly InputSlot<Vector3> Scale = new();

    [Input(Guid = "566F1619-1DE0-4B41-B167-7FC261730D62")]
    public readonly InputSlot<float> UniformScale = new();
        
    [Input(Guid = "F53F3311-E1FC-418B-8861-74ADC175D5FA")]
    public readonly InputSlot<Vector3> Shear = new();

    [Input(Guid = "279730B7-C427-4924-9FDE-77EB65A3076C")]
    public readonly InputSlot<Vector3> Pivot = new();

    
    
    [Input(Guid = "10afeac5-971c-4762-b73f-fea73a21dcd3")]
    public readonly InputSlot<Vector3> Center = new();
    
    [Input(Guid = "267aeadd-ec11-445d-9166-a741c84813bf")]
    public readonly InputSlot<float> Radius = new(); 
    
    [Input(Guid = "a1e1d1a9-0b8c-4756-b805-0839a2ee54c3")]
    public readonly InputSlot<float> FallOff = new(); 
    
    [Input(Guid = "7248C680-7279-4C1D-B968-3864CB849C77")]
    public readonly InputSlot<FieldShaderGraph> InputField = new();
}

