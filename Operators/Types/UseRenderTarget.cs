using System;
using System.Resources;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fd5ac393_02a4_43ae_8cdf_64a947abbafa
{
    public class UseRenderTarget : Instance<UseRenderTarget>
    {
        [Output(Guid = "7e846b2f-1d1a-4bfd-bbc8-d71aaaf9b5d4", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();

        [Output(Guid = "CB9850B3-119A-4839-B79F-A882112503C6")]
        public readonly Slot<RenderTargetReference> Reference = new Slot<RenderTargetReference>();
        
        public UseRenderTarget()
        {
            Texture.UpdateAction = UpdateTexture;
            Reference.UpdateAction = UpdateTexture;
        }
        
        private void UpdateTexture(EvaluationContext context)
        {
            Reference.Value = _renderTargetReference;
            Texture.Value = _renderTargetReference.ColorTexture;
        }

        private readonly RenderTargetReference _renderTargetReference = new RenderTargetReference();
    }
}