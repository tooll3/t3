using System;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_32a6a351_6d22_4915_aa0e_e0483b7f4e76
{
    public class GenerateMips : Instance<GenerateMips>
    {
        [Output(Guid = "ac14864f-3288-4cab-87a0-636cee626a2b")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureWithMips = new();

        public GenerateMips()
        {
            TextureWithMips.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            Texture2D texture = Texture.GetValue(context);
            if (texture != null)
            {
                try
                {

                    if ((texture.Description.BindFlags & BindFlags.RenderTarget) > 0)
                    {
                        if (_srv == null || _srv.Resource != texture)
                        {
                            _srv?.Dispose();
                            _srv = new ShaderResourceView(ResourceManager.Device, texture); // todo: create via resource manager
                        }

                        ResourceManager.Device.ImmediateContext.GenerateMips(_srv);
                    }
                    else
                    {
                        Log.Warning("Trying to create mips for a texture2d that doesn't have 'RenderTarget` Bindflags set", this);
                    }
                }
                catch (Exception e)
                {
                    Log.Warning("Generating MipMaps resulted in an Exception: " + e.Message, this);
                }
            }

            TextureWithMips.Value = texture;
        }

        private ShaderResourceView _srv = null;

        [Input(Guid = "a4e3001c-0663-48ec-8f56-b11ff0b40850")]
        public readonly InputSlot<Texture2D> Texture = new();
    }
}