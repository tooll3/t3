using System;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Operators.Types
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
                ShaderResourceView.Value = new ShaderResourceView(resourceManager._device, texture); // todo: create via resource manager
            }
        }

        [Input(Guid = "{D5AFA102-2F88-431E-9CD4-AF91E41F88F6}")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();
    }
}