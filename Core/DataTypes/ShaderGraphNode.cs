#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;


namespace T3.Core.DataTypes;

/**
 * Represents the attributes and parameters required to build the a shader code from
 * nested instances. This basically replicates the instance graph structure so the graph
 * can be processed without actually updating instances. This is required for maintaining
 * the structure while allowing to completely cache the output.
 *
 * Storing the references to its instance here is unfortunate.
 *
 *
 * The process is somewhat complex:
 *
 * - [_GetFieldShaderAttributes] calls Update() on its connected graphNode.
 * - This then invokes Update() on all connected graphNode-Ops which calls its...
 * - ShaderGraphNode.Update() -> Which..
 *   - updates its connected graph nodes
 *     - recursively calls Update() on their connected ops
 *   - checks with parameters are changed
 */
public class ShaderGraphNode
{
    #region node handling
    public ShaderGraphNode(Instance instance, MultiInputSlot<ShaderGraphNode>? nodeMultiInputInput = null, InputSlot<ShaderGraphNode>? inputSlot = null)
    {
        _instance = instance;
        _connectedNodeMultiInput = nodeMultiInputInput;
        _connectedNodeInput = inputSlot;
    }

    public void Update(EvaluationContext context)
    {
        CollectedChanges = ChangedFlags.None;
        
        if (_hasCodeChanges)
            CollectedChanges |= ChangedFlags.Code;
        
        // Initialize prefix and collect parameters inputs.
        // (Deferred because symbolChildId is not set at construction time)
        if (string.IsNullOrEmpty(Prefix))
        {
            Prefix = BuildNodeId(_instance);
            _shaderParameterInputs = ShaderParamHandling.CollectInputSlots(_instance, Prefix);
        }
        
        
        UpdateInputsNodes(context);
        
        var paramsNeedUpdate = IsAnyParameterDirty() || true;  
        if (paramsNeedUpdate)
        {
            CollectedChanges |= ChangedFlags.Parameters;
            foreach (var paramInput in _shaderParameterInputs)
            {
                paramInput.Update(context);
            }
        }
        //Log.Debug($"Processed {_instance}  > {CollectedChanges.ToDetailedString()}", _instance);
    }
    
    
    public ChangedFlags CollectedChanges;

    /// <summary>
    /// Recursive update all connected inputs nodes and collect their changes..
    /// </summary>
    /// <remarks>
    /// This is invoke Update() on the connected notes which will then call this methode there. 
    /// </remarks>
    private void UpdateInputsNodes(EvaluationContext context)
    {
        if (_connectedNodeMultiInput == null && _connectedNodeInput == null)
            return;

        _connectedNodeOps.Clear();
        
        //List<Slot<ShaderGraphNode>>? connectedFields = [];

        if (_connectedNodeMultiInput != null)
        {
            _connectedNodeOps.AddRange( _connectedNodeMultiInput.GetCollectedTypedInputs());
        }

        if (_connectedNodeInput != null)
        {
            _connectedNodeOps.Add(_connectedNodeInput);
        }

        // If it HAS an input field, but it's not connected, we have an error
        var hasConnectedInputFields = _connectedNodeOps.Count > 0;
        if (!hasConnectedInputFields)
        {
            CollectedChanges |= ChangedFlags.HasErrors;
            return;
        }
        
        // Align size of input nodes to connected fields
        if (_connectedNodeOps.Count != InputNodes.Count)
        {
            CollectedChanges |= ChangedFlags.Structural;
            InputNodes.Clear();
            InputNodes.AddRange(Enumerable.Repeat<ShaderGraphNode>(null!, _connectedNodeOps.Count).ToList());
        }

        // Update all connected nodes
        for (var index = 0; index < _connectedNodeOps.Count; index++)
        {
            
            // Update connected shader node...
            var updatedNode = _connectedNodeOps[index].GetValue(context);
            
            if (updatedNode != InputNodes[index])
            {
                CollectedChanges |= ChangedFlags.Structural;
                InputNodes[index] = updatedNode;
            }

            if (updatedNode == null)
            {
                CollectedChanges |= ChangedFlags.Structural | ChangedFlags.HasErrors;
                continue;
            }
            
            CollectedChanges |= updatedNode.CollectedChanges;
        }

        // Remove all nulls from InputNodes
        InputNodes.RemoveAll(node => node == null);

        // Clear dirty flags for multi input to prevent ops stuck in dirty state 
        _connectedNodeMultiInput?.DirtyFlag.Clear();
    }

    public void FlagCodeChanged()
    {
        //_lastCodeUpdateFrame = -1;
        _hasCodeChanges = true;
    }

    private bool _hasCodeChanges = true; // this will be clear once the shader code is being collected 

    #endregion


    private int _lastParamUpdateFrame = -1;
    private int _lastCodeUpdateFrame = -1;
    
    public void CollectShaderCode(StringBuilder sb, int frameNumber)
    {
        // Prevent double evaluation
        if (_lastCodeUpdateFrame == frameNumber)
            return;
        
        _hasCodeChanges = false;
        _lastCodeUpdateFrame = frameNumber;

        foreach (var inputNode in InputNodes)
        {
            inputNode.CollectShaderCode(sb, frameNumber);
        }

        if (_instance is IGraphNodeOp nodeOp)
        {
            sb.AppendLine(nodeOp.GetShaderCode());
        }
    }

    public void ClearAllChanges()
    {
        CollectedChanges = ChangedFlags.None;

        foreach (var inputNode in InputNodes)
        {
            inputNode.ClearAllChanges();
        }
    }

    
    // Keep the input slot so we can detect and handle structural changes to the graph
    private readonly MultiInputSlot<ShaderGraphNode>? _connectedNodeMultiInput;

    private readonly List<Slot<ShaderGraphNode>> _connectedNodeOps = [];
    private readonly InputSlot<ShaderGraphNode>? _connectedNodeInput;
    public readonly List<ShaderGraphNode?> InputNodes = [];

    #region parameters ----------------------
    
    private List<ShaderParamHandling.ShaderParamInput> _shaderParameterInputs = [];

    public void CollectAllNodeParams(List<float> floatValues, List<ShaderParamHandling.ShaderCodeParameter> codeParams, int frameNumber)
    {
        // Prevent double evaluation (note that _lastUpdateFrame will be updated after getting the code)
        if (_lastParamUpdateFrame == frameNumber)
            return;

        _lastParamUpdateFrame = frameNumber;
        
        foreach (var inputNode in InputNodes)
        {
            inputNode?.CollectAllNodeParams(floatValues, codeParams, frameNumber);
        }

        foreach (var input in _shaderParameterInputs)
        {
            input.GetFloat(floatValues, codeParams);
        }

        // Update non input parameters
        foreach (var param in AdditionalParameters)
        {
            if (param.Value is Matrix4x4 matrix4X4)
            {
                ShaderParamHandling.AddMatrixParameter(floatValues, codeParams, $"{Prefix}{param.Name}", matrix4X4);
            }
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

    private readonly Instance? _instance;
    
    public List<Parameter> AdditionalParameters = [];

    //public IReadOnlyCollection<ShaderParamHandling.ShaderCodeParameter> AllShaderCodeParams => _allShaderCodeParams;
    #endregion
    
    public static class ShaderParamHandling
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
    
    public sealed class Parameter(string shaderTypeName, string name, object value)
    {
        public string ShaderTypeName = shaderTypeName;
        internal readonly string Name = name;
        public object Value = value;
    }

    [Flags]
    public enum ChangedFlags
    {
        None = 0,
        Structural = 1<<1,
        Code = 1<<2,        
        Parameters = 1<<3,
        HasErrors = 1<<4,
    }
    
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


public static class EnumExtensions
{
    public static string ToDetailedString<T>(this T flags) where T : Enum
    {
        var result = new StringBuilder();
        long flagsValue = Convert.ToInt64(flags);

        foreach (T value in Enum.GetValues(typeof(T)))
        {
            long valueAsLong = Convert.ToInt64(value);

            if (valueAsLong != 0 && (flagsValue & valueAsLong) == valueAsLong)
            {
                result.Append($"{value} ({valueAsLong}), ");
            }
        }

        if (result.Length > 0)
        {
            result.Length -= 2; // Remove the trailing ", "
        }

        return result.ToString();
    }
}