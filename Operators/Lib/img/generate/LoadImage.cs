using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Texture2D = T3.Core.DataTypes.Texture2D;

namespace lib.img.generate
{
	[Guid("0b3436db-e283-436e-ba85-2f3a1de76a9d")]
    public class LoadImage : Instance<LoadImage>, IDescriptiveFilename
    {
        [Output(Guid = "{E0C4FEDD-5C2F-46C8-B67D-5667435FB037}")]
        public readonly Slot<Texture2D> Texture = new();
        
        [Output(Guid = "{A4A46C04-FF03-48CE-83C9-0C0BAA0F72E7}")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new();

        public LoadImage()
        {
            _textureResource = ResourceManager.CreateTextureResource(Path);
            Texture.UpdateAction = UpdateTexture;
            ShaderResourceView.UpdateAction = UpdateSRV;
        }

        private void UpdateSRV(EvaluationContext context)
        {
            var texture = Texture.GetValue(context);
            if (texture == null)
                return;

            var currentSrv = ShaderResourceView.Value;
            ResourceManager.CreateShaderResourceView(texture, "", ref currentSrv);

            try
            {
                ResourceManager.Device.ImmediateContext.GenerateMips(currentSrv);
            }
            catch (Exception exception)
            {
                Log.Error($"Failed to generate mipmaps for texture {Path.Value}:" + exception);
            }

            ShaderResourceView.Value = currentSrv;
        }

        private void UpdateTexture(EvaluationContext context)
        {
            Texture.Value = _textureResource.GetValue(context);
        }

        [Input(Guid = "{76CC3811-4AE0-48B2-A119-890DB5A4EEB2}")]
        public readonly InputSlot<string> Path = new();

        public IEnumerable<string> FileFilter => FileFilters;
        private static readonly string[] FileFilters = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.tga", "*.dds", "*.gif"];
        public InputSlot<string> SourcePathSlot => Path;
        
        private Resource<T3.Core.DataTypes.Texture2D> _textureResource;
        private ShaderResourceView _srv;
    }
}