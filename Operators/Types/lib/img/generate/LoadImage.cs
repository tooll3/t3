using System;
using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_0b3436db_e283_436e_ba85_2f3a1de76a9d
{
    public class LoadImage : Instance<LoadImage>
    {
        [Output(Guid = "{E0C4FEDD-5C2F-46C8-B67D-5667435FB037}")]
        public readonly Slot<Texture2D> Texture = new();
        
        [Output(Guid = "{A4A46C04-FF03-48CE-83C9-0C0BAA0F72E7}")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new();

        private uint _textureResId;
        private uint _srvResId;

        public LoadImage()
        {
            Texture.UpdateAction = UpdateTexture;
            ShaderResourceView.UpdateAction = UpdateShaderResourceView;
        }

        private void UpdateShaderResourceView(EvaluationContext context)
        {
            if (Texture.DirtyFlag.IsDirty || ShaderResourceView.DirtyFlag.IsDirty)
            {
                UpdateTexture(context);
                //Texture.DirtyFlag.Clear();
            }
        }

        private void UpdateTexture(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            if (Path.DirtyFlag.IsDirty)
            { 
                string imagePath = Path.GetValue(context);
                try
                {
                    (_textureResId, _srvResId) = resourceManager.CreateTextureFromFile(imagePath, this, () =>
                                                                                                  {
                                                                                                      Texture.DirtyFlag.Invalidate();
                                                                                                      ShaderResourceView.DirtyFlag.Invalidate();
                                                                                                  });
                    if (ResourceManager.ResourcesById.TryGetValue(_textureResId, out var resource1) && resource1 is Texture2dResource textureResource)
                        Texture.Value = textureResource.Texture;
                    if (ResourceManager.ResourcesById.TryGetValue(_srvResId, out var resource2) && resource2 is ShaderResourceViewResource srvResource)
                        ShaderResourceView.Value = srvResource.ShaderResourceView;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create texture from file '{imagePath}':" + e.Message);
                }
            }
            else
            {
                resourceManager.UpdateTextureFromFile(_textureResId, Path.Value, ref Texture.Value);
                resourceManager.CreateShaderResourceView(_textureResId, "", ref ShaderResourceView.Value);
            }

            try
            {
                if (ShaderResourceView.Value != null)
                    ResourceManager.Device.ImmediateContext.GenerateMips(ShaderResourceView.Value);
            }
            catch (Exception e)
            {
                Log.Error($"Failed to generate mipmaps for texture {Path.GetValue(context)}:" + e);
            }

            Texture.DirtyFlag.Clear();
            ShaderResourceView.DirtyFlag.Clear();
        }

        [Input(Guid = "{76CC3811-4AE0-48B2-A119-890DB5A4EEB2}")]
        public readonly InputSlot<string> Path = new();        
    }
}