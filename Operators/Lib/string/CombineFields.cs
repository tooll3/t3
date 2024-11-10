using T3.Core.Utils;

namespace Lib.@string;

[Guid("82270977-07b5-4d86-8544-5aebc638d46c")]
internal sealed class CombineFields : Instance<CombineFields>
{
    [Output(Guid = "db0bbde0-18b6-4c53-8cf7-a294177d2089")]
    public readonly Slot<FieldShaderGraph> Result = new();

    public CombineFields()
    {
        Result.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var fn = FieldShaderGraph.BuildNodeId(this);
        var sd = FieldShaderGraph.GetOrCreateDefinition(context, fn);
        Result.Value = sd;
        
        var connectedFields = InputFields.GetCollectedTypedInputs();
        
        var hasConnectedInputFields = connectedFields.Count > 0;
        if(!hasConnectedInputFields)
        {
            sd.HasErrors = true;
            sd.CollectedFeatureIds.Add(fn);
            return;
        }
        
        var combineMethod = CombineMethod.GetEnumValue<CombineMethods>(context);
        var callDef = new StringBuilder();
        var mode = _combineMethodDefinitions2[(int)combineMethod];
        
        callDef.AppendLine("");
        callDef.AppendLine($"#define {fn}CombineFunc(a,b) ({mode.Code})\n");
        callDef.AppendLine($"float {fn}(float3 p) {{");
        callDef.AppendLine($"    float d={mode.StartValue};" );
        
        foreach(var i in connectedFields) 
        {
            var inputDef = i.GetValue(context);
            if (inputDef != sd)
            {
                Log.Warning("Inconsistent field shader definition", this);
            }
            
            var inputFn = inputDef.CollectedFeatureIds[^1];
            callDef.AppendLine($"    d = {fn}CombineFunc(d,  {inputFn}(p));");
        }
        
        callDef.AppendLine("    return d;");
        callDef.AppendLine("}");
        
        sd.AppendLineToShaderCode(callDef.ToString());
        
        sd.CollectedFeatureIds.Add(fn);
    }

    //private readonly StringBuilder _stringBuilder = new();
    //private  string _fn;

    private sealed record CombineMethod2(string Code, float StartValue);
    
    private CombineMethod2[] _combineMethodDefinitions2=
        [
            new CombineMethod2("(a) + (b)",0), 
            new CombineMethod2("(a) - (b)", 0),
            new CombineMethod2("(a) * (b)", 1),
            new CombineMethod2("min(a, b)", 999999),            
            new CombineMethod2("max(a, b)", -999999),            
        ];
    
    private enum CombineMethods
    {
        Add,
        Sub,
        Multiply,
        Min,
        Max,
    }

    [Input(Guid = "7248C680-7279-4C1D-B968-3864CB849C77")]
    public readonly MultiInputSlot<FieldShaderGraph> InputFields = new();
        
    [Input(Guid = "4648E514-B48C-4A98-A728-3EBF9BCFA0B7", MappedType = typeof(CombineMethods))]
    public readonly InputSlot<int> CombineMethod = new();
}

