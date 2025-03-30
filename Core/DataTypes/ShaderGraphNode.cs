#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using T3.Core.Animation;
using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
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
public sealed class Color2dField : ShaderGraphNode
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
    
    /**
     * A counter that will be updated after op was updated by a connected output field.
     * This can later be used to optimize embedding shader code.
     * Because of dirty flag checking, Update() is only called once by update cycle then
     * resets the counter.
     */
    public int OutputCounts;

    public void Update(EvaluationContext context)
    {
        if (_lastUpdateFrame == Playback.FrameCount)
        {
            //Log.Debug($"Re-eval {OutputCounts++}", _instance);
            return;
        }

        OutputCounts = 0;

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
    
    /**
     * While collecting connected input nodes we computed a checksum to detect of the
     * graph structure has been changed and the shader code needs to be regenerated.
     */
    public int StructureHash;

    /// <summary>
    /// Recursive update all connected inputs nodes and collect their changes...
    /// </summary>
    /// <remarks>
    /// This invokes Update() on the connected notes which will then call this methode there. 
    /// </remarks>
    private void UpdateInputsNodes(EvaluationContext context)
    {
        if (_connectedNodeMultiInput == null && _connectedNodeInput == null)
            return;

        _connectedNodeOps.Clear();

        if (_connectedNodeMultiInput != null)
        {
            _connectedNodeOps.AddRange(_connectedNodeMultiInput.GetCollectedTypedInputs(true));
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
            
            updatedNode.OutputCounts++;
            //Log.Debug($"Updated {updatedNode} to {updatedNode.OutputCounts}");
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

    /** Flags the node to trigger a recompilation. */
    public void FlagCodeChanged()
    {
        _hasCodeChanges = true;
    }

    /** this will be cleared once the shader code is being collected */
    private bool _hasCodeChanges = true; 
    #endregion

    private int _lastParamUpdateFrame = -1;
    private int _lastCodeUpdateFrame = -1;
    

    public void CollectEmbeddedShaderCode(CodeAssembleContext cac)
    {
        if (_instance is not IGraphNodeOp nodeOp)
            return;

        if (InputNodes.Count == 0)
        {
            nodeOp.GetPreShaderCode(cac, 0);
            nodeOp.GetPostShaderCode(cac, 0);
            
            // We assume that a node without an input field is a distance function
            // We copy the local coordinates to the field result, so we can use
            // it later for things like UV mapping.
            cac.AppendCall($"f{cac}.xyz = p{cac}.xyz;");    
            return;
        }
        
        var isRoot = cac.ContextIdStack.Count==0;
        if (isRoot)
        {
            cac.ContextIdStack.Add("");
        }
        
        var hasMultipleInputFields = InputNodes.Count > 1;
        
        // We need to start a new fieldContext
        var contextId = cac.ContextIdStack[^1];
        var requiresSubContext = hasMultipleInputFields;
        
        if (requiresSubContext)
        {
            cac.SubContextCount++;
            cac.IndentCount++;
            cac.Calls.AppendLine();
            cac.AppendCall($"// {_instance.Symbol.Name}");

            var subContextCount = cac.SubContextCount;
            for (var inputFieldIndex = 0; inputFieldIndex < InputNodes.Count; inputFieldIndex++)
            {
                var subContextId = InputNodes.Count > 1 
                                       ? $"{subContextCount}{IntToChar(inputFieldIndex)}"
                                       : $"{subContextCount}";
                
                cac.ContextIdStack.Add(subContextId);
            
                var inputNode = InputNodes[inputFieldIndex];
                
                if(inputFieldIndex>0)
                    cac.Calls.AppendLine();
                
                cac.AppendCall($"float4 p{subContextId} = p{contextId};");
                cac.AppendCall($"float4 f{subContextId};");

                nodeOp.GetPreShaderCode(cac, inputFieldIndex);
                
                inputNode?.CollectEmbeddedShaderCode(cac);
                
                nodeOp.GetPostShaderCode(cac, inputFieldIndex);

                cac.ContextIdStack.RemoveAt(cac.ContextIdStack.Count-1);
            }

            cac.IndentCount--;
        }
        else
        {
            nodeOp.GetPreShaderCode(cac, 0);

            if (InputNodes.Count == 1)
            {
                InputNodes[0]?.CollectEmbeddedShaderCode(cac);
            }
            
            nodeOp.GetPostShaderCode(cac, 0);
        }
    }
    
    private static char IntToChar(int i) => (char)('a' + i);
    
    // Keep the input slot so we can detect and handle structural changes to the graph
    private readonly MultiInputSlot<ShaderGraphNode>? _connectedNodeMultiInput;

    private readonly List<Slot<ShaderGraphNode>> _connectedNodeOps = [];
    private readonly InputSlot<ShaderGraphNode>? _connectedNodeInput;
    public readonly List<ShaderGraphNode?> InputNodes = [];
    private int _lastParamGraphId;

    #region parameters ----------------------
    private List<ShaderParamHandling.ShaderParamInput> _shaderParameterInputs = [];

    public void CollectAllNodeParams(List<float> floatValues, List<ShaderParamHandling.ShaderCodeParameter> codeParams, int frameNumber, int graphId)
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

    
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<Parameter> AdditionalParameters = [];

    //public IReadOnlyCollection<ShaderParamHandling.ShaderCodeParameter> AllShaderCodeParams => _allShaderCodeParams;
    #endregion

    private readonly Instance _instance;

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
        Structural = 1 << 1,
        Code = 1 << 2,
        Parameters = 1 << 3,
        HasErrors = 1 << 4,
    }

    #region prefix
    private string? _prefix;

    private static string BuildNodeId(Instance instance)
    {
        return instance.GetType().Name + "_" + Utilities.ShortenGuid(instance.SymbolChildId) + "_";
    }

    public override string? ToString() => _prefix;
    #endregion
}

/**
 * Marks a field as a parameter for a shader graph node so it can be automatically checked for changes
 */
public sealed class GraphParamAttribute : Attribute;

/**
 * Needs to implemented by Symbols so provide access to its ShaderNode and code generation methods. 
 */
public interface IGraphNodeOp
{
    ShaderGraphNode ShaderNode { get; }
    void GetPreShaderCode(CodeAssembleContext cac, int inputIndex);
    void GetPostShaderCode(CodeAssembleContext cac, int inputIndex);
}

/**
 * Is passed along while collecting all connected nodes in a shader graph.  
 */
public sealed class CodeAssembleContext
{
    /**
     * A dictionary containing the pure methods that can be reuses by
     * one or more graph nodes.
     */
    public readonly Dictionary<string, string> Globals = new();
    
    /**
     * A string builder for collecting of instances specific methods containing
     * references to unique node parameters or resources.
     */
    public readonly StringBuilder Definitions=new();
    
    /**
     * A string builder for collecting the actual distance function. This is the
     * primary target CollectEmbeddedShaderCode is writing to.
     * Scopes are separated by introducing new local variables for positions and field results.
     */
    public readonly StringBuilder Calls = new();

    public void AppendCall(string code)
    {
        Calls.Append(new string('\t', (IndentCount+1)));
        Calls.AppendLine(code);
    }
    
    //public Stack<ShaderGraphNode> NodeStack = [];
    public readonly List<string> ContextIdStack = [];
    internal int IndentCount;
    internal int SubContextCount;
    
    public void Reset()
    {
        Globals.Clear();
        Definitions.Clear();
        Calls.Clear();
        ContextIdStack.Clear();
        
        IndentCount = 0;
        SubContextCount = 0;
    }

    public override string ToString()
    {
        return ContextIdStack.Count == 0 ? "" : ContextIdStack[^1];
    }
}