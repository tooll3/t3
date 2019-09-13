using System;
using System.Collections.Generic;
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
            if (ConstantBuffers.DirtyFlag.IsDirty)
            {
                var connectedConstBuffers = ConstantBuffers.GetCollectedTypedInputs();
                _constantBuffers.Clear();
                _constantBuffers.Capacity = connectedConstBuffers.Count;
                foreach (var input in connectedConstBuffers)
                {
                    _constantBuffers.Add(input.GetValue(context));
                }
            }
            _samplerState = SamplerState.GetValue(context);

            if (ShaderResources.DirtyFlag.IsDirty)
            {
                var connectedResources = ShaderResources.GetCollectedTypedInputs();
                if (connectedResources.Count != _shaderResourceViews.Length)
                {
                    _shaderResourceViews = new ShaderResourceView[connectedResources.Count];
                }

                for (int i = 0; i < connectedResources.Count; i++)
                {
                    _shaderResourceViews[i] = connectedResources[i].GetValue(context);
                }
                ShaderResources.DirtyFlag.Clear();
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
            for (int i = 0; i < _constantBuffers.Count; i++)
                csStage.SetConstantBuffer(i, _constantBuffers[i]);
            csStage.SetShaderResources(0, _shaderResourceViews.Length, _shaderResourceViews);
            csStage.SetSampler(0, _samplerState);
            csStage.SetUnorderedAccessView(0, _uav);

            int width = OutputUav.Value.Description.Width;
            int height = OutputUav.Value.Description.Height;
            deviceContext.Dispatch(width / 16, height / 16, 1);

            csStage.SetUnorderedAccessView(0, null);
            csStage.SetSampler(0, null);
            for (int i = 0; i < _shaderResourceViews.Length; i++)
                csStage.SetShaderResource(i, null);
            for (int i = 0; i < _constantBuffers.Count; i++)
                csStage.SetConstantBuffer(i, null);
        }

        private SharpDX.Direct3D11.ComputeShader _cs;
        private List<Buffer> _constantBuffers = new List<Buffer>();
        private ShaderResourceView[] _shaderResourceViews = new ShaderResourceView[0];
        private SamplerState _samplerState;
        private UnorderedAccessView _uav;
        
        [Input(Guid = "{5C0E9C96-9ABA-4757-AE1F-CC50FB6173F1}")]
        public readonly InputSlot<SharpDX.Direct3D11.ComputeShader> ComputeShader = new InputSlot<SharpDX.Direct3D11.ComputeShader>();
        [Input(Guid = "{34CF06FE-8F63-4F14-9C59-35A2C021B817}")]
        public readonly MultiInputSlot<Buffer> ConstantBuffers = new MultiInputSlot<Buffer>();
        [Input(Guid = "{88938B09-D5A7-437C-B6E1-48A5B375D756}")]
        public readonly MultiInputSlot<ShaderResourceView> ShaderResources = new MultiInputSlot<ShaderResourceView>();
        [Input(Guid = "{4047C9E7-1EDB-4C71-B85C-C1B87058C81C}")]
        public readonly InputSlot<SamplerState> SamplerState = new InputSlot<SamplerState>();
        [Input(Guid = "{CEC84992-8525-4242-B3C3-C94FE11C2A15}")]
        public readonly InputSlot<Texture2D> OutputUav = new InputSlot<Texture2D>();
    }
}