using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fd5ac393_02a4_43ae_8cdf_64a947abbafa
{
    public class UseRenderTarget : Instance<UseRenderTarget>
    {
        [Output(Guid = "CB9850B3-119A-4839-B79F-A882112503C6")]
        public readonly Slot<RenderTargetReference> Reference = new();
        
        [Output(Guid = "7e846b2f-1d1a-4bfd-bbc8-d71aaaf9b5d4", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        [Output(Guid = "FB232E1D-A09B-4B02-8E2F-408CD9AC1FFF", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> DepthTexture = new();

        
        public UseRenderTarget()
        {
            Texture.UpdateAction = UpdateTexture;
            DepthTexture.UpdateAction = UpdateTexture;
            Reference.UpdateAction = UpdateTexture;
        }
        
        private void UpdateTexture(EvaluationContext context)
        {
            Reference.Value = _renderTargetReference;
            Texture.Value = _renderTargetReference.ColorTexture;
            DepthTexture.Value = _renderTargetReference.DepthTexture;
            
            DepthTexture.DirtyFlag.Clear();
            Texture.DirtyFlag.Clear();
        }

        private readonly RenderTargetReference _renderTargetReference = new();
    }
}