using System;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class LoadTexture2d : Instance<LoadTexture2d>
    {
        [Output(Guid = "{E0C4FEDD-5C2F-46C8-B67D-5667435FB037}")]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();

        private uint _textureResId;

        public LoadTexture2d()
        {
            Texture.UpdateAction = UpdateTexture;
        }

        private void UpdateTexture(EvaluationContext context)
        {
            if (Path.DirtyFlag.IsDirty)
            {
                string imagePath = Path.GetValue(context);
                (uint textureResId, _) = ResourceManager.Instance().CreateTextureFromFile(imagePath);
                _textureResId = textureResId;

                if (ResourceManager.Instance().Resources[_textureResId] is TextureResource textureResource)
                    Texture.Value = textureResource.Texture;
            }
        }

        [Input(Guid = "{76CC3811-4AE0-48B2-A119-890DB5A4EEB2}")]
        public readonly InputSlot<string> Path = new InputSlot<string>();
    }
}