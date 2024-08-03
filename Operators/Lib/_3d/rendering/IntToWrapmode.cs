using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib._3d.rendering
{
	[Guid("54ba8673-ff58-48d1-ae2e-ee2b83bc6860")]
    public class IntToWrapmode : Instance<IntToWrapmode>
    {
        [Output(Guid = "D3E48911-F6A6-439F-B34A-84FE9D75B388")]
        public readonly Slot<SharpDX.Direct3D11.TextureAddressMode> Selected = new();

        public IntToWrapmode()
        {
            Selected.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            var index = ModeIndex.GetValue(context)
                       .Clamp((int)SharpDX.Direct3D11.TextureAddressMode.Wrap,
                              (int)SharpDX.Direct3D11.TextureAddressMode.MirrorOnce);
            Selected.Value = CastTo<SharpDX.Direct3D11.TextureAddressMode>.From(index);
        }

        [Input(Guid = "F50C736B-DC80-424B-8517-AF0CA4168666", MappedType = typeof(SharpDX.Direct3D11.TextureAddressMode))]
        public readonly InputSlot<int> ModeIndex = new(0);
    }
}