using System;
using System.IO;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f7c625da_fede_4993_976c_e259e0ee4985
{
    public class PixelShader : Instance<PixelShader>
    {
        [Output(Guid = "9C6E72F8-5CE6-42C3-ABAA-1829D2C066C1")]
        public readonly Slot<SharpDX.Direct3D11.PixelShader> Shader = new Slot<SharpDX.Direct3D11.PixelShader>();

        private uint _pixelShaderResId;

        public PixelShader()
        {
            Shader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                string sourcePath = Source.GetValue(context);
                string entryPoint = EntryPoint.GetValue(context);
                string debugName = DebugName.GetValue(context);
                if (string.IsNullOrEmpty(debugName) && !string.IsNullOrEmpty(sourcePath))
                {
                    try
                    {
                        debugName = new FileInfo(sourcePath).Name;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Invalid sourcePath for shader: {sourcePath}: " + e.Message);
                        return;
                    }
                }
                _pixelShaderResId = resourceManager.CreatePixelShaderFromFile(sourcePath, entryPoint, debugName,
                                                                              () => Shader.DirtyFlag.Invalidate());
            }
            else
            {
                resourceManager.UpdatePixelShaderFromFile(Source.Value, _pixelShaderResId, ref Shader.Value);
            }

            if (_pixelShaderResId != ResourceManager.NullResource)
            {
                Shader.Value = resourceManager.GetPixelShader(_pixelShaderResId);
            }
        }

        [Input(Guid = "24646F06-1509-43CE-94C6-EEB608AD97CD")]
        public readonly InputSlot<string> Source = new InputSlot<string>();

        [Input(Guid = "501338B3-F432-49A5-BDBD-BCF209671305")]
        public readonly InputSlot<string> EntryPoint = new InputSlot<string>();

        [Input(Guid = "BE9B3DC1-7122-4B3D-B936-CCCF2581B69E")]
        public readonly InputSlot<string> DebugName = new InputSlot<string>();
    }
}