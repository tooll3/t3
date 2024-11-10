#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using T3.Core.Operator;
using T3.Core.Operator.Slots;

namespace T3.Core.DataTypes;

/**
 * Represents the attributes and parameters required to build the a shader code from
 * nested instances. This basically replicates the instance graph structure so the graph
 * can be processed without actually updating instances. This is required for maintaining
 * the structure without while allowing to completely cache the output.
 *
 * Storing the references to its instance here is unfortunate.
 *
 */
public class ShaderGraphNode
{
    #region node handling
    public ShaderGraphNode(Instance instance, MultiInputSlot<ShaderGraphNode>? nodeMultiInputInput = null, InputSlot<ShaderGraphNode>? inputSlot = null)
    {
        Instance = instance;
        _connectedNodeMultiInput = nodeMultiInputInput;
        _connectedNodeInput = inputSlot;
    }

    public void Update(EvaluationContext context)
    {
        // Deferred because symbolChildId is not set at construction time
        if (string.IsNullOrEmpty(Prefix))
        {
            Prefix = BuildNodeId(Instance);
            _shaderParameterInputs = ShaderParamHandling.CollectInputSlots(Instance, Prefix);
        }

        HasChangedParameters = false;
        UpdateInputsNodes(context);
        HasChangedParameters |= IsAnyParameterDirty();

        foreach (var inputNode in _shaderParameterInputs)
        {
            inputNode.Update(context);
        }
    }

    private bool IsAnyParameterDirty()
    {
        foreach (var input in _shaderParameterInputs)
        {
            if (input.Slot.IsDirty)
                return true;
        }

        return false;
    }

    public Instance Instance;

    #region prefix
    public string Prefix;

    private static string BuildNodeId(Instance instance)
    {
        return instance.GetType().Name + "_" + ShortenGuid(instance.SymbolChildId) + "_";
    }

    public override string ToString() => Prefix;

    private static string ShortenGuid(Guid guid, int length = 7)
    {
        if (length < 1 || length > 22)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 1 and 22.");

        var guidBytes = guid.ToByteArray();
        var base64 = Convert.ToBase64String(guidBytes);
        var alphanumeric = base64.Replace("+", "").Replace("/", "").Replace("=", "");
        return alphanumeric[..length];
    }
    #endregion

    public bool HasErrors;
    public bool HasChangedStructure;

    /** Recursively update all dirty nodes and collect all parameters */
    private void UpdateInputsNodes(EvaluationContext context)
    {
        if (_connectedNodeMultiInput == null && _connectedNodeInput == null)
            return;

        List<Slot<ShaderGraphNode>>? connectedFields = [];

        if (_connectedNodeMultiInput != null)
        {
            connectedFields = _connectedNodeMultiInput.GetCollectedTypedInputs();
        }

        if (_connectedNodeInput != null)
        {
            connectedFields.Add(_connectedNodeInput);
        }

        // If it HAS an input field, but it's not connected, we have an error
        var hasConnectedInputFields = connectedFields.Count > 0;
        if (!hasConnectedInputFields)
        {
            HasErrors = true;
            return;
        }

        HasChangedStructure = false;

        // Align size of input nodes to connected fields
        if (connectedFields.Count != InputNodes.Count)
        {
            HasChangedStructure = true;
            InputNodes.Clear();
            InputNodes.AddRange(Enumerable.Repeat<ShaderGraphNode>(null!, connectedFields.Count).ToList());
        }

        // Update all connected nodes
        for (var index = 0; index < connectedFields.Count; index++)
        {
            var updatedNode = connectedFields[index].GetValue(context);

            if (updatedNode != InputNodes[index])
            {
                HasChangedStructure = true;
                InputNodes[index] = updatedNode;
            }

            if (updatedNode == null)
            {
                HasChangedStructure = true;
                HasErrors = true;
                continue;
            }

            HasErrors |= updatedNode.HasErrors;
            HasChangedStructure |= updatedNode.HasChangedStructure;
            HasChangedParameters |= updatedNode.HasChangedParameters;
        }

        // Remove all nulls from InputNodes
        InputNodes.RemoveAll(node => node == null);

        // Clear dirty flags for multi input to prevent ops stuck in dirty state 
        _connectedNodeMultiInput?.DirtyFlag.Clear();
    }

    public bool HasChangedCode;
    public bool HasChangedParameters;
    #endregion

    #region shader code generation
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
        var code = $"{this}(pos);//"; // add comment to avoid syntax errors in generated code

        templateCode = templateCode.Replace(commentHook, code);
    }

    private void InjectParameters(ref string templateCode)
    {
        AssembleParams();
        var commentHook = ToHlslTemplateTag("FLOAT_PARAMS");

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return;

        var sb = new StringBuilder();
        foreach (var name in _allShaderCodeParams)
        {
            sb.AppendLine($"\t{name.ShaderTypeName}  {name.Name};");
        }

        templateCode = templateCode.Replace(commentHook, sb.ToString());
    }

    private void InjectFunctions(ref string templateCode)
    {
        AssembleShaderCode();
        var commentHook = ToHlslTemplateTag("FIELD_FUNCTIONS");

        if (templateCode.IndexOf(commentHook, StringComparison.Ordinal) == -1)
            return;

        templateCode = templateCode.Replace(commentHook, _shaderCodeBuilder.ToString());
    }

    private void AssembleShaderCode()
    {
        // if (!HasChangedStructure)
        //     return;

        //_allFloatParameterValues.Clear();
        //_allShaderCodeParams.Clear();
        _shaderCodeBuilder.Clear();
        CollectShaderCode(_shaderCodeBuilder);
    }

    private void CollectShaderCode(StringBuilder sb)
    {
        // if (!HasChangedStructure)
        //     return;

        foreach (var inputNode in InputNodes)
        {
            inputNode.CollectShaderCode(sb);
        }

        if (Instance is IGraphNodeOp nodeOp)
        {
            sb.AppendLine(nodeOp.GetShaderCode());
        }
    }

    private static string ToHlslTemplateTag(string hook)
    {
        return $"/*{{{hook}}}*/";
    }

    private readonly StringBuilder _shaderCodeBuilder = new();

    // Keep the input slot so we can detect and handle structural changes to the graph
    private readonly MultiInputSlot<ShaderGraphNode>? _connectedNodeMultiInput;

    private readonly InputSlot<ShaderGraphNode>? _connectedNodeInput;
    #endregion

    #region parameters ----------------------
    /**
     * Before generating the complete shader, we need to collect all parameters of all nodes
     * into to lists: One for the float constant buffer and one of parameter names and definition
     */
    private void AssembleParams()
    {
        if (!HasChangedParameters && !HasChangedStructure)
            return;

        _allFloatParameterValues.Clear();
        _allShaderCodeParams.Clear();

        CollectAllNodeParams(_allFloatParameterValues, _allShaderCodeParams);
    }

    public List<ShaderGraphNode> InputNodes = [];

    private List<ShaderParamHandling.ShaderParamInput> _shaderParameterInputs = [];

    private void CollectAllNodeParams(List<float> floatValues, List<ShaderParamHandling.ShaderCodeParameter> codeParams)
    {
        foreach (var inputNode in InputNodes)
        {
            inputNode.CollectAllNodeParams(floatValues, codeParams);
        }

        foreach (var input in _shaderParameterInputs)
        {
            input.GetFloat(floatValues, codeParams);
        }

        // Append non input parameters
        foreach (var param in AdditionalParameters)
        {
            if (param.Value is Matrix4x4 matrix4X4)
            {
                ShaderParamHandling.AddMatrixParameter(floatValues, codeParams, $"{Prefix}{param.Name}", matrix4X4);
            }
        }
    }

    public IReadOnlyList<float> AllFloatValues => _allFloatParameterValues;
    public List<Parameter> AdditionalParameters = [];
    private readonly List<float> _allFloatParameterValues = [];

    //public IReadOnlyCollection<ShaderParamHandling.ShaderCodeParameter> AllShaderCodeParams => _allShaderCodeParams;
    private readonly List<ShaderParamHandling.ShaderCodeParameter> _allShaderCodeParams = [];
    #endregion

    private static class ShaderParamHandling
    {
        public static List<ShaderParamInput> CollectInputSlots(Instance instance, string nodePrefix)
        {
            List<ShaderParamInput> inputSlots = [];
            var fields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                // Check if the field is of type InputSlot and has the [GraphParam] attribute
                if (field.GetCustomAttribute<GraphParamAttribute>() == null)
                    continue;

                if (field.GetValue(instance) is not IInputSlot slot)
                    continue;

                inputSlots
                   .Add(slot switch
                            {
                                Slot<float> floatSlot
                                    => new ShaderParamInput(slot,
                                                            field.Name,
                                                            "float",
                                                            (floatValues, codeParams)
                                                                =>
                                                            {
                                                                AddScalarParameter(floatValues, codeParams, $"{nodePrefix}{field.Name}", floatSlot.Value);
                                                                //codeParams.Add(new ShaderCodeParameter("float", $"{nodePrefix}{field.Name}"));
                                                            },
                                                            context => { floatSlot.GetValue(context); }
                                                           ),
                                Slot<Vector2> vec2Slot
                                    => new ShaderParamInput(
                                                            slot,
                                                            field.Name,
                                                            "float2",
                                                            (floatValues, codeParams)
                                                                =>
                                                            {
                                                                AddVec2Parameter(floatValues, codeParams, $"{nodePrefix}{field.Name}", vec2Slot.Value);
                                                                //codeParams.Add(new ShaderCodeParameter("float2", $"{nodePrefix}{field.Name}"));
                                                            },
                                                            context => { vec2Slot.GetValue(context); }
                                                           ),
                                Slot<Vector3> vec3Slot
                                    => new ShaderParamInput(slot,
                                                            field.Name,
                                                            "float3",
                                                            (floatValues, codeParams)
                                                                =>
                                                            {
                                                                AddVec3Parameter(floatValues, codeParams, $"{nodePrefix}{field.Name}", vec3Slot.Value);
                                                                //codeParams.Add(new ShaderCodeParameter("float3", $"{nodePrefix}{field.Name}"));
                                                            },
                                                            context => { vec3Slot.GetValue(context); }
                                                           ),

                                Slot<Matrix4x4> matrixSlot
                                    => new ShaderParamInput(slot,
                                                            field.Name,
                                                            "float4x4",
                                                            (floatValues, codeParams)
                                                                =>
                                                            {
                                                                AddMatrixParameter(floatValues, codeParams, $"{nodePrefix}{field.Name}", matrixSlot.Value);
                                                                //codeParams.Add(new ShaderCodeParameter("float4x4", $"{nodePrefix}{field.Name}"));
                                                            },
                                                            context => { matrixSlot.GetValue(context); }
                                                           ),

                                Slot<int> intSlot
                                    => new ShaderParamInput(slot,
                                                            field.Name,
                                                            "int",
                                                            (floatValues, codeParams)
                                                                =>
                                                            {
                                                                AddScalarParameter(floatValues, codeParams, $"{nodePrefix}{field.Name}", intSlot.Value);
                                                                //codeParams.Add(new ShaderCodeParameter("int", $"{nodePrefix}{field.Name}"));
                                                            },
                                                            context => { intSlot.GetValue(context); }
                                                           ),
                                _ => throw new ArgumentOutOfRangeException()
                            });
            }

            return inputSlots;
        }

        private static void AddScalarParameter(List<float> floatValues, List<ShaderCodeParameter> codeParams, string name, float value)
        {
            floatValues.Add(value);
            codeParams.Add(new ShaderCodeParameter("float", name));
        }

        private static void AddVec2Parameter(List<float> floatValues, List<ShaderCodeParameter> codeParams, string name, Vector2 value)
        {
            PadFloatParametersToVectorComponentCount(floatValues, codeParams, 2);
            floatValues.Add(value.X);
            floatValues.Add(value.Y);
            codeParams.Add(new ShaderCodeParameter("float2", name));
        }

        private static void AddVec3Parameter(List<float> floatValues, List<ShaderCodeParameter> codeParams, string name, Vector3 value)
        {
            PadFloatParametersToVectorComponentCount(floatValues, codeParams, 3);
            floatValues.Add(value.X);
            floatValues.Add(value.Y);
            floatValues.Add(value.Z);
            codeParams.Add(new ShaderCodeParameter("float3", name));
        }

        public static void AddMatrixParameter(List<float> floatValues, List<ShaderCodeParameter> codeParams, string name, Matrix4x4 matrix)
        {
            PadFloatParametersToVectorComponentCount(floatValues, codeParams, 4);
            Span<float> elements = MemoryMarshal.CreateSpan(ref matrix.M11, 16);
            foreach (var value in elements)
            {
                floatValues.Add(value);
            }

            codeParams.Add(new ShaderCodeParameter("float4x4", name));
        }

        /**
         *  |0123|0123|
         *  |VVV |    | 0 ok
         *  | VVV|    | 1 ok
         *  |  VV|V   | 2 -> padBy 2
         *  |   V|VV  | 3 -> padBy 1
         *
         *  |0123|0123|
         *  |vvvv|    | 0 ok
         *  | vvv|v   | 1 -> padBy 3
         *  |  vv|vv  | 2 -> padBy 2
         *  |   v|vvv | 3 -> padBy 1
        */
        private static void PadFloatParametersToVectorComponentCount(List<float> values, List<ShaderCodeParameter> codeParams,
                                                                     int size)
        {
            var currentStart = values.Count % 4;
            var requiredPadding = 0;
            if (size == 2)
            {
                requiredPadding = currentStart % 2;
            }
            else if (size == 3)
            {
                if (currentStart == 2)
                    requiredPadding = 2;
                else if(currentStart == 3)
                    requiredPadding = 1;
            }
            else if (size == 4)
            {
                requiredPadding = (4 - currentStart) % 4;
            }
            
            for (var i = 0; i < requiredPadding; i++)
            {
                values.Add(0);
                codeParams.Add(new ShaderParamHandling.ShaderCodeParameter("float", "__padding" + values.Count));
            }
        }

        /** When collect these at instance construction to to avoid later casting */
        public sealed record ShaderParamInput(
            IInputSlot Slot,
            string Name,
            string ShaderTypeName,
            GetFloatDelegate GetFloat,
            UpdateDelegate Update);

        public sealed record ShaderCodeParameter(string ShaderTypeName, string Name);

        public delegate void GetFloatDelegate(List<float> floatValues,
                                              List<ShaderCodeParameter> codeParams);

        public delegate void UpdateDelegate(EvaluationContext context);
    }

    public class Parameter(string shaderTypeName, string name, object value)
    {
        public string ShaderTypeName = shaderTypeName;
        public string Name = name;
        public object Value = value;
    }
}

/**
 * Mark a field as a parameter for a shader graph node so it can be automatically checked for changes
 */
public class GraphParamAttribute : Attribute;

public interface IGraphNodeOp
{
    public ShaderGraphNode ShaderNode { get; }
    public string GetShaderCode();
}