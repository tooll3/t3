#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.Logging;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using Log = T3.Core.Logging.Log;

namespace T3.Core.DataTypes;

/**
 * Represents the attributes and parameters required to build a shader code from
 * nested instances. This basically replicates the instance graph structure so the graph
 * can be processed without actually updating instances. This is required for maintaining
 * the structure while allowing to completely cache the output.
 *
 * Storing the references to its operator Instance here is unfortunate.
 *
 * The process is somewhat complex:
 *
 * - [_GetFieldShaderAttributes] calls Update() on its connected graphNode(s).
 * - This then invokes .Update() on all connected graphNode-Ops which calls its...
 * - ShaderGraphNode.Update() -> Which...
 *   - updates its connected graph nodes
 *     - recursively calls Update() on their connected ops
 *   - checks if parameters are changed
 * - after completing this first pass of traversing the tree, we know if parameters, the code or the whole graph structure changed.
 * - [_GetFieldShaderAttributes]
 *      then calls...
 *          _graphNode.CollectShaderCode()
 *          _graphNode.CollectAllNodeParams()
 *      to recursively collect all required code and parameter fragments.
 */

public class Color2dField : ShaderGraphNode
{
    public Color2dField(Instance instance, MultiInputSlot<ShaderGraphNode>? nodeMultiInputInput = null, InputSlot<ShaderGraphNode>? inputSlot = null) 
        : base(instance, nodeMultiInputInput, inputSlot)
    {
    }
}

public class ShaderGraphNode
{
    #region node handling
    public ShaderGraphNode(Instance instance, MultiInputSlot<ShaderGraphNode>? nodeMultiInputInput = null, InputSlot<ShaderGraphNode>? inputSlot = null)
    {
        _instance = instance;
        _connectedNodeMultiInput = nodeMultiInputInput;
        _connectedNodeInput = inputSlot;
    }
    
    private int _lastUpdateFrame;

    public void Update(EvaluationContext context)
    {
        if (_lastUpdateFrame == Playback.FrameCount)
            return;
        
        _lastUpdateFrame = Playback.FrameCount;
        
        CollectedChanges = ChangedFlags.None;
        
        if (_hasCodeChanges)
            CollectedChanges |= ChangedFlags.Code;
        
        // Initialize prefix and collect parameters inputs.
        // (Deferred because symbolChildId is not set at construction time)
        if (string.IsNullOrEmpty(_prefix))
        {
            _prefix = BuildNodeId(_instance);
            _shaderParameterInputs = ShaderParamHandling.CollectInputSlots(_instance, _prefix);
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
    public int StructureHash = 0;
    
    /// <summary>
    /// Recursive update all connected inputs nodes and collect their changes..
    /// </summary>
    /// <remarks>
    /// This invokes Update() on the connected notes which will then call this methode there. 
    /// </remarks>
    private void UpdateInputsNodes(EvaluationContext context)
    {
        if (_connectedNodeMultiInput == null && _connectedNodeInput == null)
            return;

        _connectedNodeOps.Clear();
        
        //List<Slot<ShaderGraphNode>>? connectedFields = [];

        if (_connectedNodeMultiInput != null)
        {
            _connectedNodeOps.AddRange( _connectedNodeMultiInput.GetCollectedTypedInputs(true));
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
        var lastStructureHash = StructureHash;
        StructureHash = _instance.SymbolChildId.GetHashCode();
        
        for (var index = 0; index < _connectedNodeOps.Count; index++)
        {
            // Update connected shader node...
            var updatedNode = _connectedNodeOps[index].GetValue(context);
            if (updatedNode == null)
            {
                CollectedChanges |= ChangedFlags.Structural | ChangedFlags.HasErrors;
                continue;
            }
            
            StructureHash = StructureHash * 31 + updatedNode.StructureHash;
            
            if (updatedNode != InputNodes[index])
            {
                CollectedChanges |= ChangedFlags.Structural;
                InputNodes[index] = updatedNode;
            }
            
            CollectedChanges |= updatedNode.CollectedChanges;
        }

        if (StructureHash != lastStructureHash)
        {
            CollectedChanges |= ChangedFlags.Structural;
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
    
    public void CollectShaderCode(StringBuilder sb, Dictionary<string, string> globals, int frameNumber, Guid graphId)
    {
        // Prevent double evaluation
        if (_lastCodeUpdateFrame == frameNumber && _lastCodeGraphId == graphId)
            return;
        
        _hasCodeChanges = false;
        _lastCodeGraphId = graphId;
        _lastCodeUpdateFrame = frameNumber;

        foreach (var inputNode in InputNodes)
        {
            inputNode?.CollectShaderCode(sb, globals, frameNumber, graphId);
        }

        if (_instance is IGraphNodeOp nodeOp)
        {
            nodeOp.GetShaderCode(sb, globals);
        }
    }

    public void ClearAllChanges()
    {
        CollectedChanges = ChangedFlags.None;

        foreach (var inputNode in InputNodes)
        {
            inputNode?.ClearAllChanges();
        }
    }

    
    // Keep the input slot so we can detect and handle structural changes to the graph
    private readonly MultiInputSlot<ShaderGraphNode>? _connectedNodeMultiInput;

    private readonly List<Slot<ShaderGraphNode>> _connectedNodeOps = [];
    private readonly InputSlot<ShaderGraphNode>? _connectedNodeInput;
    public readonly List<ShaderGraphNode?> InputNodes = [];
    private Guid _lastParamGraphId;
    private Guid _lastCodeGraphId;

    #region parameters ----------------------
    
    private List<ShaderParamHandling.ShaderParamInput> _shaderParameterInputs = [];

    public void CollectAllNodeParams(List<float> floatValues, List<ShaderParamHandling.ShaderCodeParameter> codeParams, int frameNumber, Guid graphId)
    {
        // Prevent double evaluation (note that _lastUpdateFrame will be updated after getting the code)
        if (_lastParamUpdateFrame == frameNumber && _lastParamGraphId == graphId)
            return;

        _lastParamUpdateFrame = frameNumber;
        _lastParamGraphId = graphId;
        
        foreach (var inputNode in InputNodes)
        {
            inputNode?.CollectAllNodeParams(floatValues, codeParams, frameNumber, graphId);
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
                ShaderParamHandling.AddMatrixParameter(floatValues, codeParams, $"{_prefix}{param.Name}", matrix4X4);
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

    private readonly Instance _instance;
    
    public List<Parameter> AdditionalParameters = [];

    //public IReadOnlyCollection<ShaderParamHandling.ShaderCodeParameter> AllShaderCodeParams => _allShaderCodeParams;
    #endregion
    
    public static class ShaderParamHandling
    {
        internal static List<ShaderParamInput> CollectInputSlots(Instance instance, string nodePrefix)
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
                                Slot<Vector4> vec4Slot
                                    => new ShaderParamInput(slot,
                                                            field.Name,
                                                            "float4",
                                                            (floatValues, codeParams)
                                                                =>
                                                            {
                                                                AddVec4Parameter(floatValues, codeParams, $"{nodePrefix}{field.Name}", vec4Slot.Value);
                                                            },
                                                            context => { vec4Slot.GetValue(context); }
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

        private static void AddVec4Parameter(List<float> floatValues, List<ShaderCodeParameter> codeParams, string name, Vector4 value)
        {
            PadFloatParametersToVectorComponentCount(floatValues, codeParams, 3);
            floatValues.Add(value.X);
            floatValues.Add(value.Y);
            floatValues.Add(value.Z);
            floatValues.Add(value.W);
            codeParams.Add(new ShaderCodeParameter("float4", name));
        }
        
        internal static void AddMatrixParameter(List<float> floatValues, List<ShaderCodeParameter> codeParams, string name, Matrix4x4 matrix)
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

        /** We collect these at instance construction to avoid later casting */
        internal sealed record ShaderParamInput(
            IInputSlot Slot,
            string Name,
            string ShaderTypeName,
            GetFloatDelegate GetFloat,
            UpdateDelegate Update);

        public sealed record ShaderCodeParameter(string ShaderTypeName, string Name);

        internal delegate void GetFloatDelegate(List<float> floatValues,
                                                List<ShaderCodeParameter> codeParams);

        internal delegate void UpdateDelegate(EvaluationContext context);
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
    private string? _prefix;

    private static string BuildNodeId(Instance instance)
    {
        return instance.GetType().Name + "_" + ShortenGuid(instance.SymbolChildId) + "_";
    }

    public override string? ToString() => _prefix;

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
 * Marks a field as a parameter for a shader graph node so it can be automatically checked for changes
 */
public sealed class GraphParamAttribute : Attribute;

public interface IGraphNodeOp
{
    ShaderGraphNode ShaderNode { get; }
    void GetShaderCode(StringBuilder shaderStringBuilder, Dictionary<string, string> globals);
}

