using Lib.dx11.api;
using Lib.dx11.buffer;
using SharpDX;
using SharpDX.Direct3D11;
using Utilities = T3.Core.Utils.Utilities;

namespace Lib.math.@float;

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
 *  - &&
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

    private void Update(EvaluationContext context)
    {
        var shaderGraph = Field.GetValue(context);
        if (shaderGraph == null)
        {
            _lastErrorMessage = "Missing input field";
            return;
        }
        
        var templateCode = TemplateCode.GetValue(context);
        if (string.IsNullOrEmpty(templateCode))
        {
            _lastErrorMessage = "Missing input template code";
            return;
        }
        
        ShaderCode.Value = shaderGraph.GenerateShaderCode(templateCode);
        
        var floatParams = shaderGraph.AllFloatValues;
        if(floatParams.Count == 0)
            return;
        
        CreateBuffer(FloatParams, floatParams);
    }


    private static void CreateBuffer(Slot<Buffer> floatSlotBuffer, IReadOnlyList<float> floatParams)
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
    
    [Input(Guid = "FFC1C70E-B717-4337-916D-C3A13343E9CC")]
    public readonly InputSlot<ShaderGraphNode> Field = new();
    
    [Input(Guid = "BCF6DE27-1FFD-422C-9F5B-910D89CAD1A4")]
    public readonly InputSlot<string> TemplateCode = new();
}