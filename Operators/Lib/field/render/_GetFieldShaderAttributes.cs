using Lib.render._dx11.api;
using SharpDX;
using SharpDX.Direct3D11;
using Utilities = T3.Core.Utils.Utilities;

namespace Lib.field.render;

/** *
 * Handles triggering the generation of shader graph code and updating the float parameters.
 *
 * The focus of this is to avoid all unnecessary updates or even recompilations. For this we
 * follow the following strategy:
 *
 * ## Field is not dirty:
 * No Updates necessary. All parameters code and graph are already up to date.
 * 
 * ## Field is dirty:
 * Update the invalidated part of connected FieldGraph-instances and their GraphNodes. Each GraphNode
 * will fetch is updated float parameters. It will try to detected changes in the the graph setup by comparing
 * if the connected graph nodes have change.
 *
 * They will recursively return an invalidate settings for CodeNeedsUpdate and ParamsNeedUpdate. 
 *
 * If CodeNeedsUpdate is true, the shader code will be updated.
 * 
 * 
 */
[Guid("73c028d1-3de2-4269-b503-97f62bbce320")]
internal sealed class _GetFieldShaderAttributes : Instance<_GetFieldShaderAttributes>, IStatusProvider
{
    [Output(Guid = "A1AB0C16-ED15-4334-A529-10E3C217DF1A")]
    public readonly Slot<string> ShaderCode = new();

    [Output(Guid = "1a9b5e15-e9a7-4ed4-aa1a-2072398921b4")]
    public readonly Slot<Buffer> FloatParams = new();
    
    // [Output(Guid = "1791B00E-583F-4E3A-BEB9-7C4CA6648935")]
    // public readonly Slot<List<float>> FloatParams = new();
    
    public _GetFieldShaderAttributes()
    {
        ShaderCode.UpdateAction += Update;
    }

    private bool _needsInvalidation = true;

    private int Invalidate(ISlot slot)
    {
        var slotFlag = slot.DirtyFlag;
        if (slot.TryGetFirstConnection(out var firstConnection))
        {
            // slot is an output of an composition op
            slotFlag.Target = Invalidate(firstConnection);
        }
        else
        {
            Instance parent = slot.Parent;

            foreach (var input in parent.Inputs)
            {
                var inputFlag = input.DirtyFlag;
                if (input.TryGetFirstConnection(out var inputConnection))
                {
                    if (input.IsMultiInput)
                    {
                        var multiInput = (IMultiInputSlot)input;
                        int dirtySum = 0;
                        foreach (var entry in multiInput.GetCollectedInputs())
                        {
                            dirtySum += Invalidate(entry);
                        }

                        inputFlag.Target = dirtySum;
                    }
                    else
                    {
                        inputFlag.Target = Invalidate(inputConnection);
                    }
                }
                else
                {
                    inputFlag.Invalidate();
                }
            }

            slotFlag.Invalidate();
        }

        return slotFlag.Target;
    }
    
    /// <summary>
    /// This is only updated if subgraph has been changed.
    /// </summary>
    private void Update(EvaluationContext context)
    {
        
        //Log.Debug("_GetShaderAttributes.Update", this);
        var templateCode = TemplateCode.GetValue(context);
        if (string.IsNullOrEmpty(templateCode))
        {
            _lastErrorMessage = "Missing input template code";
            return;
        }

        //Invalidate(Field);
        if (_needsInvalidation)
        {
            _needsInvalidation = false;
        }
        
        // Recursively update complete shader graph and collect changes
        _graphNode = Field.GetValue(context); 
        
        if (_graphNode == null)
        {
            _lastErrorMessage = "Missing input field";
            _needsInvalidation = true;
            return;
        }

        
        var changes = _graphNode.CollectedChanges;
        if (changes == ShaderGraphNode.ChangedFlags.None)
            return;

        Log.Debug(" Update parameter buffer...");
        AssembleParams();
        var floatParams = AllFloatValues;
        if (floatParams.Count > 0)
        {
            CreateParameterBuffer(FloatParams, floatParams);
            //Log.Debug("No params?");
            //return;
        }
        
        if ((changes & (ShaderGraphNode.ChangedFlags.Code|ShaderGraphNode.ChangedFlags.Structural)) != 0)
        {
            Log.Debug(" Regenerate shader code");
            ShaderCode.Value = GenerateShaderCode(templateCode);
        }

        _graphNode.ClearAllChanges();
        _frameUpdateCount++;
    }

    private string GenerateShaderCode(string code)
    {
        AssembleAndInjectParameters(ref code);
        AssembleAndInjectFunctions(ref code);
        InjectCall(ref code);
        return code;
    }

    private void InjectCall(ref string code)
    {
        var commentHook =  ToHlslTemplateTag("FIELD_CALL");
        var callCode = $"{_graphNode}(pos);//"; // add comment to avoid syntax errors in generated code

        code = code.Replace(commentHook, callCode);
    }

    private void AssembleAndInjectParameters(ref string templateCode)
    {
        //AssembleParams();
        var commentHook = ToHlslTemplateTag("FLOAT_PARAMS");

        if (templateCode.IndexOf((string)commentHook, StringComparison.Ordinal) == -1)
            return;

        var sb = new StringBuilder();
        foreach (var name in _allShaderCodeParams)
        {
            sb.AppendLine($"\t{name.ShaderTypeName}  {name.Name};");
        }

        templateCode = templateCode.Replace(commentHook, sb.ToString());
    }
    
    /**
     * Before generating the complete shader, we need to collect all parameters of all nodes
     * into to lists: One for the float constant buffer and one of parameter names and definition
     */
    public void AssembleParams()
    {
        // if (_graphNode.CollectedChanges == ShaderGraphNode.ChangedFlags.None)
        //     return;

        _allFloatParameterValues.Clear();
        _allShaderCodeParams.Clear();

        _graphNode.CollectAllNodeParams(_allFloatParameterValues, _allShaderCodeParams, _frameUpdateCount);
    }
    
    private readonly List<ShaderGraphNode.ShaderParamHandling.ShaderCodeParameter> _allShaderCodeParams = [];
    public IReadOnlyList<float> AllFloatValues => _allFloatParameterValues;
    private readonly List<float> _allFloatParameterValues = [];


    private void AssembleAndInjectFunctions(ref string templateCode)
    {
        AssembleShaderCode();
        var commentHook = ToHlslTemplateTag("FIELD_FUNCTIONS");

        if (templateCode.IndexOf((string)commentHook, StringComparison.Ordinal) == -1)
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
        _graphNode.CollectShaderCode(_shaderCodeBuilder, _frameUpdateCount);
    }
    
    private readonly StringBuilder _shaderCodeBuilder = new();
    private int _frameUpdateCount;
    
    private static void CreateParameterBuffer(Slot<Buffer> floatSlotBuffer, IReadOnlyList<float> floatParams)
    {
        try
        {
            //var floatParams = Params.GetValue(context);
            if (floatParams == null || floatParams.Count == 0)
                return;
            
            //var array = floatParams.ToArray();
            var arraySize = (floatParams.Count / 4 + (floatParams.Count % 4 == 0 ? 0 : 1)) * 4; // always 16byte slices for alignment
            var array = new float[arraySize];
            
            for (var i = 0; i < floatParams.Count; i++)
            {
                array[i] = floatParams[i];
            }
            
            var device = ResourceManager.Device;

            var size = sizeof(float) * array.Length;
            using (var data = new DataStream(size, true, true))
            {
                data.WriteRange(array);
                data.Position = 0;

                if (floatSlotBuffer.Value == null || floatSlotBuffer.Value.Description.SizeInBytes != size)
                {
                    Utilities.Dispose(ref floatSlotBuffer.Value);
                    var bufferDesc = new BufferDescription
                                         {
                                             Usage = ResourceUsage.Default,
                                             SizeInBytes = size,
                                             BindFlags = BindFlags.ConstantBuffer
                                         };
                    floatSlotBuffer.Value = new Buffer(device, data, bufferDesc);
                }
                else
                {
                    device.ImmediateContext.UpdateSubresource(new DataBox(data.DataPointer, 0, 0), floatSlotBuffer.Value, 0);
                }
            }
            floatSlotBuffer.Value.DebugName = nameof(FloatsToBuffer);
        }
        catch (Exception e)
        {
            Log.Warning("Failed to creat float value buffer" + e.Message);
        }        
    }    
    
        
    #region Implementation of IStatusProvider
    IStatusProvider.StatusLevel IStatusProvider.GetStatusLevel() =>
        string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;

    string IStatusProvider.GetStatusMessage() => _lastErrorMessage;
    private string _lastErrorMessage = string.Empty;
    #endregion
    
    private ShaderGraphNode _graphNode;

    [Input(Guid = "FFC1C70E-B717-4337-916D-C3A13343E9CC")]
    public readonly InputSlot<ShaderGraphNode> Field = new();
    
    [Input(Guid = "BCF6DE27-1FFD-422C-9F5B-910D89CAD1A4")]
    public readonly InputSlot<string> TemplateCode = new();

    public static string ToHlslTemplateTag(string hook)
    {
        return $"/*{{{hook}}}*/";
    }
}