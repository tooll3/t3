using System;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types
{
    public class ComputeShaderStage : Instance<ComputeShaderStage>
    {
        [Output(Guid = "{C382284F-7E37-4EB0-B284-BC735247F26B}")]
        public readonly Slot<int> Output = new Slot<int>();

        public ComputeShaderStage()
        {
            Output.UpdateAction = Update;
            Output.DirtyFlag.Trigger = DirtyFlagTrigger.Always; // always render atm
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager._device;
            var deviceContext = device.ImmediateContext;
            var csStage = deviceContext.ComputeShader;

            _cs = ComputeShader.GetValue(context);
            _constantBuffer = ConstantBuffer.GetValue(context);
            _samplerState = SamplerState.GetValue(context);

            if (ShaderResources.DirtyFlag.IsDirty)
            {
                _srv?.Dispose();
                Texture2D inputTexture = ShaderResources.GetValue(context);
                if (inputTexture != null)
                {
                    _srv = new ShaderResourceView(device, inputTexture);
                }
            }

            if (OutputUav.DirtyFlag.IsDirty)
            {
                _uav?.Dispose();
                Texture2D outputTexture = OutputUav.GetValue(context);
                if (outputTexture != null)
                {
                    _uav = new UnorderedAccessView(device, outputTexture);
                }
            }

            if (_uav == null || _cs == null)
                return;

            csStage.Set(_cs);
            csStage.SetConstantBuffer(0, _constantBuffer);
            csStage.SetShaderResource(0, _srv);
            csStage.SetSampler(0, _samplerState);
            csStage.SetUnorderedAccessView(0, _uav);

            int width = OutputUav.Value.Description.Width;
            int height = OutputUav.Value.Description.Height;
            deviceContext.Dispatch(width / 16, height / 16, 1);

            csStage.SetUnorderedAccessView(0, null);
            csStage.SetSampler(0, null);
            csStage.SetShaderResource(0, null);
            csStage.SetConstantBuffer(0, null);
        }

        private SharpDX.Direct3D11.ComputeShader _cs;
        private Buffer _constantBuffer;
        private ShaderResourceView _srv;
        private SamplerState _samplerState;
        private UnorderedAccessView _uav;
        
        [Input(Guid = "{5C0E9C96-9ABA-4757-AE1F-CC50FB6173F1}")]
        public readonly InputSlot<SharpDX.Direct3D11.ComputeShader> ComputeShader = new InputSlot<SharpDX.Direct3D11.ComputeShader>();
        [Input(Guid = "{34CF06FE-8F63-4F14-9C59-35A2C021B817}")]
        public readonly InputSlot<Buffer> ConstantBuffer = new InputSlot<Buffer>();
        [Input(Guid = "{88938B09-D5A7-437C-B6E1-48A5B375D756}")]
        public readonly InputSlot<Texture2D> ShaderResources = new InputSlot<Texture2D>();
        [Input(Guid = "{4047C9E7-1EDB-4C71-B85C-C1B87058C81C}")]
        public readonly InputSlot<SamplerState> SamplerState = new InputSlot<SamplerState>();
        [Input(Guid = "{CEC84992-8525-4242-B3C3-C94FE11C2A15}")]
        public readonly InputSlot<Texture2D> OutputUav = new InputSlot<Texture2D>();
    }
}