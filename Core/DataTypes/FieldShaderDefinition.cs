using System;
using System.Collections.Generic;
using System.Text;
using T3.Core.Operator;

namespace T3.Core.DataTypes;

public class FieldShaderDefinition
{
    public string GetShaderCode()
    {
        return _shaderDefBuilder.ToString();
    }

    public void AppendLineToShaderDef(string s)
    {
        _shaderDefBuilder.AppendLine(s);
    }

    private record ShaderParameter(string TypeName, string Name);

    public List<float> FloatBufferValues = [];
    private List<ShaderParameter> FloatParameters = [];

    public void KeepScalarParameter(string name, float value, string fn)
    {
        FloatParameters.Add(new ShaderParameter("float", fn + name));
        FloatBufferValues.Add(value);
    }

    public void KeepVec2Parameter(string name, Vector2 value, string fn)
    {
        PadFloatParametersToVectorComponentCount(2);
        FloatParameters.Add(new ShaderParameter("float2", fn + name));
        FloatBufferValues.Add(value.X);
        FloatBufferValues.Add(value.Y);
    }

    public void KeepVec3Parameter(string name, Vector3 value, string fn)
    {
        FloatParameters.Add(new ShaderParameter("float3", fn + name));
        FloatBufferValues.Add(value.X);
        FloatBufferValues.Add(value.Y);
        FloatBufferValues.Add(value.Z);
    }

    //
    //  |0123|0123|
    //  |VVV |    | 0 ok
    //  | VVV|    | 1 ok
    //  |  VV|V   | 2 -> padBy 2
    //  |   V|VV  | 3 -> padBy 1
    public void PadFloatParametersToVectorComponentCount(int size)
    {
        var rest = FloatBufferValues.Count % 4;
        if (rest <= 4 - size)
            return;

        var requiredPadding = size - rest + 1;

        for (var i = 0; i < requiredPadding; i++)
        {
            FloatBufferValues.Add(0);
            FloatParameters.Add(new ShaderParameter("float", "__padding" + FloatBufferValues.Count));
        }
    }

    public static string ContextVariableName = "__ScalarFieldDefinition";
    public bool HasErrors;

    public List<string> CollectedFeatureIds = [];

    public static string BuildInstanceId(Instance instance)
    {
        return instance.GetType().Name + "_" + ShortenGuid(instance.SymbolChildId) + "_";
    }

    public static FieldShaderDefinition GetOrCreateDefinition(EvaluationContext context, string fn)
    {
        if (context.ObjectVariables.TryGetValue(ContextVariableName, out var sdObj)
            && sdObj is FieldShaderDefinition sd)
        {
            var restartedFromRoot = sd.CollectedFeatureIds.Count > 0 && sd.CollectedFeatureIds[0] == fn;
            if (!restartedFromRoot)
                return sd;
        }

        sd = new FieldShaderDefinition();
        context.ObjectVariables[ContextVariableName] = sd;
        return sd;
    }

    private static string ShortenGuid(Guid guid, int length = 7)
    {
        if (length < 1 || length > 22)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 1 and 22.");

        var guidBytes = guid.ToByteArray();
        var base64 = Convert.ToBase64String(guidBytes);
        var alphanumeric = base64.Replace("+", "").Replace("/", "").Replace("=", "");
        return alphanumeric.Substring(0, length);
    }

    private readonly StringBuilder _shaderDefBuilder = new();

    public string GenerateShaderCode(string templateCode)
    {
        InjectParameters(ref templateCode);
        InjectFunctions(ref templateCode);
        InjectCall(ref templateCode);
        return templateCode;
    }

    private bool InjectCall(ref string templateCode)
    {
        var commentHook = ToCommentHook("FIELD_CALL");
        if(CollectedFeatureIds.Count == 0)
            return false;

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return false;

        var fn = CollectedFeatureIds[^1];
        var code = $"{fn}(pos);//";

        templateCode = templateCode.Replace(commentHook, code);
        return true;
    }

    private bool InjectParameters(ref string templateCode)
    {
        var commentHook = ToCommentHook("FLOAT_PARAMS");

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return false;

        var sb = new StringBuilder();
        foreach (var name in FloatParameters)
        {
            sb.AppendLine($"\t{name.TypeName} {name.Name};");
        }

        templateCode = templateCode.Replace(commentHook, sb.ToString());
        return true;
    }

    private bool InjectFunctions(ref string templateCode)
    {
        var commentHook = ToCommentHook("FIELD_FUNCTIONS");

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return false;

        templateCode = templateCode.Replace(commentHook, _shaderDefBuilder.ToString());
        return true;
    }

    private string ToCommentHook(string hook)
    {
        return $"/*{{{hook}}}*/";
    }
}