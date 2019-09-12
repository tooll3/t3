using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class SamplerStateOp : Instance<SamplerStateOp>
    {
        [Output(Guid = "{0E45C596-C80F-4927-941F-E3199401AA10}")]
        public readonly Slot<SamplerState> SamplerState = new Slot<SamplerState>();

        private uint _samplerResId;

        public SamplerStateOp()
        {
            SamplerState.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            SamplerState.Value?.Dispose();
            var samplerDesc = new SamplerStateDescription()
                              {
                                  Filter = Filter.GetValue(context),
                                  AddressU = AddressU.GetValue(context),
                                  AddressV = AddressV.GetValue(context),
                                  AddressW = AddressW.GetValue(context),
                                  MipLodBias = MipLoadBias.GetValue(context),
                                  MaximumAnisotropy = MaximumAnisotropy.GetValue(context),
                                  ComparisonFunction = ComparisonFunction.GetValue(context),
//                                  BorderColor = BorderColor.GetValue(context),
                                  MinimumLod = MinimumLod.GetValue(context),
                                  MaximumLod = MaximumLod.GetValue(context)
                              };
            SamplerState.Value = new SamplerState(ResourceManager.Instance()._device, samplerDesc); // todo: put into resource manager
        }

        [Input(Guid = "{A870921F-A28C-4501-9F31-38A18B0ACDCC}")]
        public readonly InputSlot<Filter> Filter = new InputSlot<Filter>();

        [Input(Guid = "{E7C95FD5-14D1-434F-A140-F22EF69076AB}")]
        public readonly InputSlot<TextureAddressMode> AddressU = new InputSlot<TextureAddressMode>();

        [Input(Guid = "{FDEB503F-09C6-48D1-8853-7426F68CDEC3}")]
        public readonly InputSlot<TextureAddressMode> AddressV = new InputSlot<TextureAddressMode>();

        [Input(Guid = "{93D8BF26-5067-4CCC-B4CB-E03970686462}")]
        public readonly InputSlot<TextureAddressMode> AddressW = new InputSlot<TextureAddressMode>();

        [Input(Guid = "{4B51422E-1DA7-4A28-B55F-47881D42F801}")]
        public readonly InputSlot<float> MipLoadBias = new InputSlot<float>();

        [Input(Guid = "{1CCE7427-2062-42FD-838E-328DEF3ECB30}")]
        public readonly InputSlot<int> MaximumAnisotropy = new InputSlot<int>();

        [Input(Guid = "{393A3E40-5C58-48C7-84D2-C4E8D03F7373}")]
        public InputSlot<Comparison> ComparisonFunction = new InputSlot<Comparison>();

//        [Input(Guid = "{5A6E8282-EBF6-4641-9574-A29E04F08B2E}")]
//        public InputSlot<RawColor4> BorderColor = new InputSlot<RawColor4>();

        [Input(Guid = "{05531EF5-72AA-4868-915F-A40D26DA9E80}")]
        public InputSlot<float> MinimumLod = new InputSlot<float>();

        [Input(Guid = "{7D5346DB-DF9C-4BCE-9F39-065983253A7F}")]
        public InputSlot<float> MaximumLod = new InputSlot<float>();
    }
}