using System.ComponentModel;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D11;

namespace T3.Core.Operator.Types
{
    //public int Width;
    //public int Height;
    //public int MipLevels;
    //public int ArraySize;
    //public Format Format;
    //public SampleDescription SampleDescription;
    //public ResourceUsage Usage;
    //public BindFlags BindFlags;
    //public CpuAccessFlags CpuAccessFlags;
    //public ResourceOptionFlags OptionFlags;

    public class Texture2d : Instance<Texture2d>
    {
        //[Output(Guid = "{007129E4-0EAE-4CB9-A142-90C1C171A5FB}")]
        //public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();

        //[Output(Guid = "{0CC65A71-805E-4DE4-94F2-CC710F5F6319}")]
        //public readonly Slot<ShaderResourceView> TextureSrv = new Slot<ShaderResourceView>();

        public Texture2d()
        {
            //Texture.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Size2 size = Size.GetValue(context);
            int mipLevels = MipLevels.GetValue(context);
            int arraySize = MipLevels.GetValue(context);

            var texDesc = new Texture2DDescription
                          {
                              Width = size.Width,
                              Height = size.Height,
                              MipLevels = mipLevels,
                              ArraySize = arraySize,
                          };
        }

        [Size2Input(256, 256, Guid = "{B77088A9-2676-4CAA-809A-5E0F120D25D7}")]
        public readonly InputSlot<Size2> Size = new InputSlot<Size2>();

        [FormatInput(DefaultValue = SharpDX.DXGI.Format.R8G8B8A8_UNorm, Guid = "{67CD82C3-504B-4C80-8C49-5B303733ED52}")]
        public readonly InputSlot<SharpDX.DXGI.Format> Format = new InputSlot<SharpDX.DXGI.Format>();

        [IntInput(DefaultValue = 0, Guid = "{58FF26E7-6BEB-44CB-910B-FE467402CEE9}")]
        public readonly InputSlot<int> MipLevels = new InputSlot<int>();

        [IntInput(DefaultValue = 0, Guid = "{940D3D3C-607A-460C-A7FE-22876960D706}")]
        public readonly InputSlot<int> ArraySize = new InputSlot<int>();

        [ResourceUsageInput(DefaultValue = SharpDX.Direct3D11.ResourceUsage.Default, Guid = "{98353EF2-A18D-43E5-8828-ED8BD182DDD1}")]
        public readonly InputSlot<ResourceUsage> ResourceUsage = new InputSlot<ResourceUsage>();
    }
}