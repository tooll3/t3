using System;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class LoadTexture2d : Instance<LoadTexture2d>
    {
        [Output(Guid = "{E0C4FEDD-5C2F-46C8-B67D-5667435FB037}")]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();
        [Output(Guid = "{A4A46C04-FF03-48CE-83C9-0C0BAA0F72E7}")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new Slot<ShaderResourceView>();

        private uint _textureResId;
        private uint _srvResId;

        public LoadTexture2d()
        {
            Texture.UpdateAction = UpdateTexture;
            ShaderResourceView.UpdateAction = UpdateShaderResourceView;
        }

        private void UpdateShaderResourceView(EvaluationContext context)
        {
            if (Texture.DirtyFlag.IsDirty)
            {
                UpdateTexture(context);
                Texture.DirtyFlag.Clear();
            }
        }

        private void UpdateTexture(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            if (Path.DirtyFlag.IsDirty)
            {
                string imagePath = Path.GetValue(context);
                (_textureResId, _srvResId) = resourceManager.CreateTextureFromFile(imagePath, () =>
                                                                                              {
                                                                                                  Texture.DirtyFlag.Invalidate();
                                                                                                  ShaderResourceView.DirtyFlag.Invalidate();
                                                                                              });
                if (resourceManager.Resources.TryGetValue(_textureResId, out var resource1) && resource1 is TextureResource textureResource)
                    Texture.Value = textureResource.Texture;
                if (resourceManager.Resources.TryGetValue(_srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
                    ShaderResourceView.Value = srvResource.ShaderResourceView;
            }
            else
            {
                ResourceManager.Instance().UpdateTextureFromFile(_textureResId, Path.Value, ref Texture.Value);
                ResourceManager.Instance().CreateShaderResourceView(_textureResId, "", ref ShaderResourceView.Value);
            }
        }

        [Input(Guid = "{76CC3811-4AE0-48B2-A119-890DB5A4EEB2}")]
        public readonly InputSlot<string> Path = new InputSlot<string>();
    }
}