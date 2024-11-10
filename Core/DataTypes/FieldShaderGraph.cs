using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using T3.Core.Operator;

namespace T3.Core.DataTypes;

public class FieldShaderGraph
{
    #region node handling
    public static string BuildNodeId(Instance instance)
    {
        return instance.GetType().Name + "_" + ShortenGuid(instance.SymbolChildId) + "_";
    }

    public static FieldShaderGraph GetOrCreateDefinition(EvaluationContext context, string fn)
    {
        if (context.ObjectVariables.TryGetValue(ContextVariableName, out var sdObj)
            && sdObj is FieldShaderGraph sd)
        {
            var restartedFromRoot = sd.CollectedFeatureIds.Count > 0 && sd.CollectedFeatureIds[0] == fn;
            if (!restartedFromRoot)
                return sd;
        }

        sd = new FieldShaderGraph();
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
    
    public static string ContextVariableName = "__ScalarFieldDefinition";
    public bool HasErrors;
    
    public List<string> CollectedFeatureIds = [];

    #endregion
    

    #region parameters
    private sealed record ShaderParameter(string TypeName, string Name);
    
    public List<float> FloatBufferValues = [];
    private readonly List<ShaderParameter> _floatParameters = [];

    public void KeepScalarParameter(string name, float value, string fn)
    {
        _floatParameters.Add(new ShaderParameter("float", fn + name));
        FloatBufferValues.Add(value);
    }

    public void KeepVec2Parameter(string name, Vector2 value, string fn)
    {
        PadFloatParametersToVectorComponentCount(2);
        _floatParameters.Add(new ShaderParameter("float2", fn + name));
        FloatBufferValues.Add(value.X);
        FloatBufferValues.Add(value.Y);
    }

    public void KeepVec3Parameter(string name, Vector3 value, string fn)
    {
        PadFloatParametersToVectorComponentCount(3);
        _floatParameters.Add(new ShaderParameter("float3", fn + name));
        FloatBufferValues.Add(value.X);
        FloatBufferValues.Add(value.Y);
        FloatBufferValues.Add(value.Z);
    }

    public void KeepMatrixParameter(string name, Matrix4x4 matrix, string fn)
    {
        PadFloatParametersToVectorComponentCount(4);
        _floatParameters.Add(new ShaderParameter("float4x4", fn + name));
        Span<float> elements = MemoryMarshal.CreateSpan(ref matrix.M11, 16);
        foreach (float value in elements)
        {
            FloatBufferValues.Add(value);
        }
    }
    
    //
    //  |0123|0123|
    //  |VVV |    | 0 ok
    //  | VVV|    | 1 ok
    //  |  VV|V   | 2 -> padBy 2
    //  |   V|VV  | 3 -> padBy 1
    public void PadFloatParametersToVectorComponentCount(int size)
    {
        var currentStart = FloatBufferValues.Count % 4;
        if (currentStart <= 4 - size)
            return;

        var requiredPadding = size - currentStart + 1;

        for (var i = 0; i < requiredPadding; i++)
        {
            FloatBufferValues.Add(0);
            _floatParameters.Add(new ShaderParameter("float", "__padding" + FloatBufferValues.Count));
        }
    }
    #endregion
    

    #region shader code
    
    public string GetShaderCode()
    {
        return _shaderCodeBuilder.ToString();
    }

    public void AppendLineToShaderCode(string s)
    {
        _shaderCodeBuilder.AppendLine(s);
    }
    

    public string GenerateShaderCode(string templateCode)
    {
        InjectParameters(ref templateCode);
        InjectFunctions(ref templateCode);
        InjectCall(ref templateCode);
        return templateCode;
    }

    private void InjectCall(ref string templateCode)
    {
        var commentHook = ToHlslTemplateTag("FIELD_CALL");
        if(CollectedFeatureIds.Count == 0)
            return;

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return;

        var fn = CollectedFeatureIds[^1];
        var code = $"{fn}(pos);//"; // add comment to avoid syntax errors in generated code

        templateCode = templateCode.Replace(commentHook, code);
    }

    private void InjectParameters(ref string templateCode)
    {
        var commentHook = ToHlslTemplateTag("FLOAT_PARAMS");

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return;

        var sb = new StringBuilder();
        foreach (var name in _floatParameters)
        {
            sb.AppendLine($"\t{name.TypeName} {name.Name};");
        }

        templateCode = templateCode.Replace(commentHook, sb.ToString());
    }

    private void InjectFunctions(ref string templateCode)
    {
        var commentHook = ToHlslTemplateTag("FIELD_FUNCTIONS");

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return;

        templateCode = templateCode.Replace(commentHook, _shaderCodeBuilder.ToString());
    }

    private static string ToHlslTemplateTag( string hook)
    {
        return $"/*{{{hook}}}*/";
    }
    
    private readonly StringBuilder _shaderCodeBuilder = new();

    #endregion

    
    public class Node
    {
        public string Prefix;
        public Instance Instance;
    }
}