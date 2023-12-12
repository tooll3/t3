using System.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_9f784a4a_857f_41ad_afc1_0de08c1cfec6
{
    public class PixelShaderFromSource : Instance<PixelShaderFromSource>
    {
        [Output(Guid = "C513F15D-3A7E-4501-B825-EF3E585293C7")]
        public readonly Slot<SharpDX.Direct3D11.PixelShader> PixelShader = new();
        
        private uint _pixelShaderResId;

        public PixelShaderFromSource()
        {
            PixelShader.UpdateAction = Update;
        }

        public string GetDescriptiveString()
        {
            return _description;
        }

        private string _description = "not loaded";

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            var shaderSourceIsDirty = ShaderSource.DirtyFlag.IsDirty;
            
            if (shaderSourceIsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                var shaderSource = ShaderSource.GetValue(context);
                var entryPoint = EntryPoint.GetValue(context);
                var debugName = DebugName.GetValue(context);
                if (string.IsNullOrEmpty(debugName) && !string.IsNullOrEmpty(shaderSource))
                {
                    debugName = new FileInfo(shaderSource).Name;
                }
                
                var success = resourceManager.CreatePixelShaderFromSource(shaderSource: shaderSource,
                                                                            name: debugName,
                                                                            entryPoint: entryPoint,
                                                                            ref _pixelShaderResId);
                if (!success)
                {
                    // We don't have to log a failure here, because the ResourceManager already did that.
                }
            }

            if (_pixelShaderResId == ResourceManager.NullResource)
            {
                PixelShader.DirtyFlag.Clear();
                return;
            }
            
            PixelShader.Value = resourceManager.GetPixelShader(_pixelShaderResId);
            
            // var shaderReflection = new ShaderReflection(resourceManager.GetPixelShaderBytecode(_pixelShaderResId));
            // shaderReflection.GetThreadGroupSize(out int x, out int y, out int z);
            // ThreadCount.Value = new Int3(x, y, z);
        }

        [Input(Guid = "a192e8cc-2874-4f02-bbf1-4622e99666e1")]
        public readonly InputSlot<string> ShaderSource = new();

        [Input(Guid = "2b616fb0-2966-45a9-a0cc-da960ca509cf")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "baa49d7d-127c-4c93-ae90-7e4db3598af9")]
        public readonly InputSlot<string> DebugName = new();
    }
}