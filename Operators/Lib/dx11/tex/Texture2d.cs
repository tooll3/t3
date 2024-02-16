using System.Runtime.InteropServices;
using System;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace lib.dx11.tex
{
	[Guid("f52db9a4-fde9-49ca-9ef7-131825c34e65")]
    public class Texture2d : Instance<Texture2d>
    {
        [Output(Guid = "{007129E4-0EAE-4CB9-A142-90C1C171A5FB}")]
        public readonly Slot<Texture2D> Texture = new();

        private uint _textureResId;

        public Texture2d()
        {
            Texture.UpdateAction = UpdateTexture;
        }

        private void UpdateTexture(EvaluationContext context)
        {
            var size = Size.GetValue(context);
            if (size.Height <= 0 || size.Width <= 0)
            {
                Log.Warning($"Requested invalid texture resolution: {size}", this);
                return;
            }

            var requestedMipLevels = MipLevels.GetValue(context);
            var maxMapLevels = (int)Math.Log2(Math.Max(size.Width, size.Height));
            var mipLevels = Math.Min(requestedMipLevels, maxMapLevels)+1;
            

            try
            {
                var texDesc = new Texture2DDescription
                                  {
                                      Width = size.Width,
                                      Height = size.Height,
                                      MipLevels = mipLevels,
                                      ArraySize = ArraySize.GetValue(context),
                                      Format = Format.GetValue(context),
                                      //SampleDescription = SampleDescription.GetValue(context),
                                      SampleDescription = new SampleDescription(1, 0),
                                      Usage = ResourceUsage.GetValue(context),
                                      BindFlags = BindFlags.GetValue(context),
                                      CpuAccessFlags = CpuAccessFlags.GetValue(context),
                                      OptionFlags = ResourceOptionFlags.GetValue(context)
                                  };
                ResourceManager.Instance().CreateTexture2d(texDesc, "Texture2D", ref _textureResId, ref Texture.Value);
            }
            catch(Exception e)
            {
                Log.Error($"Failed to create Texture2d: {e.Message}", this);
            }
            //ResourceManager.Instance().id .TestId = _textureResId;
        }

        [Input(Guid = "{B77088A9-2676-4CAA-809A-5E0F120D25D7}")]
        public readonly InputSlot<Int2> Size = new();

        [Input(Guid = "{58FF26E7-6BEB-44CB-910B-FE467402CEE9}")]
        public readonly InputSlot<int> MipLevels = new();

        [Input(Guid = "{940D3D3C-607A-460C-A7FE-22876960D706}")]
        public readonly InputSlot<int> ArraySize = new();

        [Input(Guid = "{67CD82C3-504B-4C80-8C49-5B303733ED52}")]
        public readonly InputSlot<Format> Format = new();

        //public readonly InputSlot<SampleDescription> SampleDescription = new InputSlot<SampleDescription>();

        [Input(Guid = "{98353EF2-A18D-43E5-8828-ED8BD182DDD1}")]
        public readonly InputSlot<ResourceUsage> ResourceUsage = new();

        [Input(Guid = "{CFEBC37F-6813-416A-9073-E48D31074115}")]
        public readonly InputSlot<BindFlags> BindFlags = new();

        [Input(Guid = "{DA0D06BD-F5CC-400B-8E79-35756DF9B2D5}")]
        public InputSlot<CpuAccessFlags> CpuAccessFlags = new();

        [Input(Guid = "{2C9E4CB0-0333-439E-ABCC-1148A840A260}")]
        public InputSlot<ResourceOptionFlags> ResourceOptionFlags = new();
    }
}