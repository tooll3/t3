using System;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_4494473b_1868_460e_8ac3_b5d57c8a156e
{
    public class DsvFromTexture2d : Instance<DsvFromTexture2d>
    {
        [Output(Guid = "A2E78CBD-CB22-4D14-AB0C-54F20CC4CAD6")]
        public readonly Slot<DepthStencilView> DepthStencilView = new();

        public DsvFromTexture2d()
        {
            DepthStencilView.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (!Texture.DirtyFlag.IsDirty)
                return; // nothing to do

            try
            {
                var device = ResourceManager.Device;
                Texture2D texture = Texture.GetValue(context);
                if (texture != null)
                {
                    _depthBufferDsv = new DepthStencilView(device,
                                                           texture,
                                                           new DepthStencilViewDescription
                                                               {
                                                                   Format = Format.D32_Float,
                                                                   Dimension = DepthStencilViewDimension.Texture2D
                                                               });
                }
            }
            catch (Exception e)
            {
                Log.Warning("Failed to create DSV from Texture2d: " + e.Message);
            }

            DepthStencilView.Value = _depthBufferDsv;
        }

        [Input(Guid = "7a3a1f0c-9d60-4e8f-a199-7c3477886c68")]
        public readonly InputSlot<Texture2D> Texture = new();

        private DepthStencilView _depthBufferDsv;
    }
}