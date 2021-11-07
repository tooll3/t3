using System;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c2078514_cf1d_439c_a732_0d7b31b5084a
{
    public class SrvFromTexture2d : Instance<SrvFromTexture2d>
    {
        [Output(Guid = "{DC71F39F-3FBA-4FC6-B8EF-CE57C82BF78E}")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new Slot<ShaderResourceView>();

        public SrvFromTexture2d()
        {
            ShaderResourceView.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            Texture2D texture = Texture.GetValue(context);
            if (texture != null)
            {
                ShaderResourceView.Value?.Dispose();
                if ((texture.Description.BindFlags & BindFlags.DepthStencil) > 0)
                {
                    // it's a depth stencil texture, so we need to set the format explicitly
                    var desc = new ShaderResourceViewDescription()
                                   {
                                       Format = Format.R32_Float,
                                       Dimension = ShaderResourceViewDimension.Texture2D,
                                       Texture2D = new ShaderResourceViewDescription.Texture2DResource
                                                       {
                                                           MipLevels = 1,
                                                           MostDetailedMip = 0
                                                       }
                                   };
                    ShaderResourceView.Value = new ShaderResourceView(resourceManager.Device, texture, desc);
                }
                else
                {
                    ShaderResourceView.Value = new ShaderResourceView(resourceManager.Device, texture); // todo: create via resource manager
                }
            }
            else
            {
                Utilities.Dispose(ref ShaderResourceView.Value);
            }
        }

        [Input(Guid = "{D5AFA102-2F88-431E-9CD4-AF91E41F88F6}")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();
    }
}