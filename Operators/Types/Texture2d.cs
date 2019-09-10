using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class Texture2d : Instance<Texture2d>
    {
        //[Output(Guid = "{007129E4-0EAE-4CB9-A142-90C1C171A5FB}")]
        //public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();

        [Output(Guid = "{0CC65A71-805E-4DE4-94F2-CC710F5F6319}")]
        public readonly Slot<ShaderResourceView> ShaderResourceView = new Slot<ShaderResourceView>();

        private Texture2D _texture;
        private uint _textureResId;
        private bool _textureChanged = true;
        private uint _textureSrvResId;

        public Texture2d()
        {
            //Texture.UpdateAction = UpdateTexture;
            ShaderResourceView.UpdateAction = UpdateShaderResourceView;
        }

        private void UpdateTexture(EvaluationContext context)
        {
            Size2 size = Size.GetValue(context);

            var texDesc = new Texture2DDescription
                          {
                              Width = size.Width,
                              Height = size.Height,
                              MipLevels = MipLevels.GetValue(context),
                              ArraySize = ArraySize.GetValue(context),
                              Format = Format.GetValue(context),
                              //SampleDescription = SampleDescription.GetValue(context),
                              SampleDescription = new SampleDescription(1, 0),
                              Usage = ResourceUsage.GetValue(context),
                              BindFlags = BindFlags.GetValue(context),
                              CpuAccessFlags = CpuAccessFlags.GetValue(context),
                              OptionFlags = ResourceOptionFlags.GetValue(context)
                          };
            _textureChanged = ResourceManager.Instance().CreateTexture(texDesc, "Texture2D", ref _textureResId, ref _texture); //Texture.Value);
        }

        private void UpdateShaderResourceView(EvaluationContext context)
        {
            //if (Texture.Value == null)
            UpdateTexture(context);

            if (_textureChanged)
            {
                ResourceManager.Instance().CreateShaderResourceView(_textureResId, "Texture2dSrv", ref ShaderResourceView.Value);
                _textureChanged = false;
            }
        }

        [Input(Guid = "{B77088A9-2676-4CAA-809A-5E0F120D25D7}")]
        public readonly InputSlot<Size2> Size = new InputSlot<Size2>();

        [Input(Guid = "{58FF26E7-6BEB-44CB-910B-FE467402CEE9}")]
        public readonly InputSlot<int> MipLevels = new InputSlot<int>();

        [Input(Guid = "{940D3D3C-607A-460C-A7FE-22876960D706}")]
        public readonly InputSlot<int> ArraySize = new InputSlot<int>();

        [Input(Guid = "{67CD82C3-504B-4C80-8C49-5B303733ED52}")]
        public readonly InputSlot<Format> Format = new InputSlot<Format>();

        //public readonly InputSlot<SampleDescription> SampleDescription = new InputSlot<SampleDescription>();

        [Input(Guid = "{98353EF2-A18D-43E5-8828-ED8BD182DDD1}")]
        public readonly InputSlot<ResourceUsage> ResourceUsage = new InputSlot<ResourceUsage>();

        [Input(Guid = "{CFEBC37F-6813-416A-9073-E48D31074115}")]
        public readonly InputSlot<BindFlags> BindFlags = new InputSlot<BindFlags>();

        [Input(Guid = "{DA0D06BD-F5CC-400B-8E79-35756DF9B2D5}")]
        public InputSlot<CpuAccessFlags> CpuAccessFlags = new InputSlot<CpuAccessFlags>();

        [Input(Guid = "{2C9E4CB0-0333-439E-ABCC-1148A840A260}")]
        public InputSlot<ResourceOptionFlags> ResourceOptionFlags = new InputSlot<ResourceOptionFlags>();
    }
}