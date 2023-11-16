using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using SharpDX.Direct3D11;

namespace T3.Operators.Types.Id_54ba8673_ff58_48d1_ae2e_ee2b83bc6860
{
    public class PickWrapMode : Instance<PickWrapMode>
    {
        [Output(Guid = "D3E48911-F6A6-439F-B34A-84FE9D75B388")]
        public readonly Slot<SharpDX.Direct3D11.TextureAddressMode> Selected = new();

        public PickWrapMode()
        {
            Selected.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
           var index = Index.GetValue(context);
            if (index < 0)
                index = -index;
            var connections = Input.GetCollectedTypedInputs();
            if (connections == null || connections.Count == 0)
            {
                Selected.Value = GetTextureAddressModeAtIndex(index);
                return;
            }

            index %= connections.Count;
           
            
        }

        private TextureAddressMode GetTextureAddressModeAtIndex(int index)
        {
            
            switch (index)
            {
                case 0:
                    return TextureAddressMode.Wrap;

                case 1:
                    return TextureAddressMode.Mirror;

                case 2:
                    return TextureAddressMode.Clamp;

                case 3:
                    return TextureAddressMode.Border;

                case 4:
                    return TextureAddressMode.MirrorOnce;

                default:
                    return TextureAddressMode.Clamp;
            }
        }

        [Input(Guid = "D59B2FFC-7EC8-4F56-8F4D-EC998A6673E8")]
        public readonly MultiInputSlot<SharpDX.Direct3D11.TextureAddressMode> Input = new();

        [Input(Guid = "F50C736B-DC80-424B-8517-AF0CA4168666")]
        public readonly InputSlot<int> Index = new(0);
    }
}