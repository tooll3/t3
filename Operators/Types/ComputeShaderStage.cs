using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types
{
    public class ComputeShaderStage : Instance<ComputeShaderStage>
    {
        [Output(Guid = "{C382284F-7E37-4EB0-B284-BC735247F26B}")]
        public readonly Slot<Scene> Output = new Slot<Scene>();

        public ComputeShaderStage()
        {
            Output.UpdateAction = Update;
            Output.DirtyFlag.Trigger = DirtyFlagTrigger.Always; // always render atm
        }

        private void UpdateMultiInput<T>(MultiInputSlot<T> input, ref T[] resources, EvaluationContext context)
        {
            if (input.DirtyFlag.IsDirty)
            {
                var connectedConstBuffers = input.GetCollectedTypedInputs();
                if (connectedConstBuffers.Count != resources.Length)
                {
                    resources = new T[connectedConstBuffers.Count];
                }

                for (int i = 0; i < connectedConstBuffers.Count; i++)
                {
                    resources[i] = connectedConstBuffers[i].GetValue(context);
                }

                input.DirtyFlag.Clear();
            }
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager._device;
            var deviceContext = device.ImmediateContext;
            var csStage = deviceContext.ComputeShader;

            _cs = ComputeShader.GetValue(context);

            UpdateMultiInput(ConstantBuffers, ref _constantBuffers, context);
            UpdateMultiInput(ShaderResources, ref _shaderResourceViews, context);
            UpdateMultiInput(SamplerStates, ref _samplerStates, context);
            UpdateMultiInput(Uavs, ref _uavs, context);

            if (_uavs.Length == 0 || _uavs[0] == null || _cs == null)
                return;

            csStage.Set(_cs);
            csStage.SetConstantBuffers(0, _constantBuffers.Length, _constantBuffers);
            csStage.SetShaderResources(0, _shaderResourceViews.Length, _shaderResourceViews);
            csStage.SetSamplers(0, _samplerStates);
            csStage.SetUnorderedAccessViews(0, _uavs);

            Int3 dispatchCount = Dispatch.GetValue(context);
            deviceContext.Dispatch(dispatchCount.X, dispatchCount.Y, dispatchCount.Z);

            // unbind resources
            for (int i = 0; i < _uavs.Length; i++)
                csStage.SetUnorderedAccessView(i, null);
            for (int i = 0; i < _samplerStates.Length; i++)
                csStage.SetSampler(i, null);
            for (int i = 0; i < _shaderResourceViews.Length; i++)
                csStage.SetShaderResource(i, null);
            for (int i = 0; i < _constantBuffers.Length; i++)
                csStage.SetConstantBuffer(i, null);
        }

        private SharpDX.Direct3D11.ComputeShader _cs;
        private Buffer[] _constantBuffers = new Buffer[0];
        private ShaderResourceView[] _shaderResourceViews = new ShaderResourceView[0];
        private SamplerState[] _samplerStates = new SamplerState[0];
        private UnorderedAccessView[] _uavs = new UnorderedAccessView[0];
        
        [Input(Guid = "{180CAE35-10E3-47F3-8191-F6ECEA7D321C}")]
        public readonly InputSlot<Int3> Dispatch = new InputSlot<Int3>(new Int3(16, 16, 1));
        [Input(Guid = "{5C0E9C96-9ABA-4757-AE1F-CC50FB6173F1}")]
        public readonly InputSlot<SharpDX.Direct3D11.ComputeShader> ComputeShader = new InputSlot<SharpDX.Direct3D11.ComputeShader>();
        [Input(Guid = "{34CF06FE-8F63-4F14-9C59-35A2C021B817}")]
        public readonly MultiInputSlot<Buffer> ConstantBuffers = new MultiInputSlot<Buffer>();
        [Input(Guid = "{88938B09-D5A7-437C-B6E1-48A5B375D756}")]
        public readonly MultiInputSlot<ShaderResourceView> ShaderResources = new MultiInputSlot<ShaderResourceView>();
        [Input(Guid = "{4047C9E7-1EDB-4C71-B85C-C1B87058C81C}")]
        public readonly MultiInputSlot<SamplerState> SamplerStates = new MultiInputSlot<SamplerState>();
        [Input(Guid = "{599384C2-BF6C-4953-BE74-D363292AB1C7}")]
        public readonly MultiInputSlot<UnorderedAccessView> Uavs = new MultiInputSlot<UnorderedAccessView>();
    }
}