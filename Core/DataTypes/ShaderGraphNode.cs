#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public Color2dField(Instance instance, 
                        MultiInputSlot<ShaderGraphNode>? nodeMultiInputInput = null, 
                        InputSlot<ShaderGraphNode>? inputSlot = null)
        : base(instance, nodeMultiInputInput, inputSlot)
    {
    }
}

public class ShaderGraphNode
{
    #region node handling
    public ShaderGraphNode(Instance instance,
                           MultiInputSlot<ShaderGraphNode>? nodeMultiInputInput = null,
                           params InputSlot<ShaderGraphNode>?[] inputsSlots)
    {
        _instance = instance;
        _connectedNodeMultiInput = nodeMultiInputInput;
        _connectedNodeInputs = inputsSlots;
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
        {
            CollectedChanges |= ChangedFlags.Code;
            _hasCodeChanges = false;
        }

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
        _connectedNodeOps.Clear();

        if (_connectedNodeMultiInput != null)
        {
            _connectedNodeOps.AddRange(_connectedNodeMultiInput.GetCollectedTypedInputs(true));
        }

        _connectedNodeOps.AddRange(_connectedNodeInputs);
        
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
        
        InputNodes.Clear();

        foreach(var connectedNodeOp in _connectedNodeOps)
        {
            var updatedNode = connectedNodeOp?.GetValue(context);

            if (updatedNode == null) 
                continue;
            
            updatedNode.OutputCounts++;
            StructureHash = StructureHash * 31 + updatedNode.StructureHash;
            CollectedChanges |= updatedNode.CollectedChanges;
            InputNodes.Add(updatedNode);
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

    public void CollectEmbeddedShaderCode(CodeAssembleContext cac)
    {
        if (_instance is not IGraphNodeOp nodeOp)
            return;

        var isRoot = cac.ContextIdStack.Count == 0;
        if (isRoot)
        {
            cac.ContextIdStack.Add("");
        }
        
        nodeOp.AddDefinitions(cac);

        if (nodeOp.TryBuildCustomCode(cac))
        {
            return;
        }
        
        if (InputNodes.Count == 0)
        {
            nodeOp.GetPreShaderCode(cac, 0);
            nodeOp.GetPostShaderCode(cac, 0);

            // We assume that a node without an input field is a distance function
            // We copy the local coordinates to the field result, so we can use
            // it later for things like UV mapping.
            //cac.AppendCall($"f{cac}.xyz = p.w < 0.5 ?  p{cac}.xyz : 1; // #{string.Join(' ', cac.ContextIdStack)} {this}");
            return;
        }

        // We need to start a new fieldContext
        var contextId = cac.ContextIdStack[^1];
        var requiresSubContext = InputNodes.Count > 1;

        if (requiresSubContext)
        {
            cac.SubContextCount++;
            cac.IndentCount++;
            cac.Calls.AppendLine();
            cac.AppendCall($"// {_instance.Symbol.Name}");

            var subContextIndex = cac.SubContextCount;
            for (var inputFieldIndex = 0; inputFieldIndex < InputNodes.Count; inputFieldIndex++)
            {
                cac.PushContext(subContextIndex, 
                                InputNodes.Count == 0 ? "" : ((char)('a' + inputFieldIndex)).ToString());

                nodeOp.GetPreShaderCode(cac, inputFieldIndex);

                InputNodes[inputFieldIndex]?.CollectEmbeddedShaderCode(cac);

                nodeOp.GetPostShaderCode(cac, inputFieldIndex);

                cac.PopContext();
                cac.Calls.AppendLine();
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

    // Keep the input slot so we can detect and handle structural changes to the graph
    private readonly MultiInputSlot<ShaderGraphNode>? _connectedNodeMultiInput;
    private readonly InputSlot<ShaderGraphNode>?[] _connectedNodeInputs;

    private readonly List<Slot<ShaderGraphNode>?> _connectedNodeOps = [];
    public readonly List<ShaderGraphNode?> InputNodes = [];
    private int _lastParamGraphId;
    
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