using System.Runtime.InteropServices;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace lib.dx11.compute
{
	[Guid("8bef116d-7d1c-4c1b-b902-25c1d5e925a9")]
    public class ComputeShaderStage : Instance<ComputeShaderStage>, IRenderStatsProvider
    {
        [Output(Guid = "{C382284F-7E37-4EB0-B284-BC735247F26B}")]
        public readonly Slot<Command> Output = new();

        public ComputeShaderStage()
        {
            Output.UpdateAction = Update;
            if (!_statsRegistered)
            {
                RenderStatsCollector.RegisterProvider(this);
                _statsRegistered = true;
            }
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var csStage = deviceContext.ComputeShader;

            _cs = ComputeShader.GetValue(context);
            
            Int3 dispatchCount = Dispatch.GetValue(context);
            int count = DispatchCallCount.GetValue(context).Clamp(1, 10);

            ConstantBuffers.GetValues(ref _constantBuffers, context);
            ShaderResources.GetValues(ref _shaderResourceViews, context);
            SamplerStates.GetValues(ref _samplerStates, context);
            Uavs.GetValues(ref _uavs, context);
            int counter = UavBufferCounter.GetValue(context);

            if (_uavs.Length == 0 || _uavs[0] == null || _cs == null)
                return;

            csStage.Set(_cs);
            csStage.SetConstantBuffers(0, _constantBuffers.Length, _constantBuffers);
            csStage.SetShaderResources(0, _shaderResourceViews.Length, _shaderResourceViews);
            
            csStage.SetSamplers(0, _samplerStates);
            if (_uavs.Length == 4)
            {
                csStage.SetUnorderedAccessViews(0, _uavs, new[] { -1, 0, -1, -1 });
            }
            else if (_uavs.Length == 1)
            {
                if (counter == -1)
                    csStage.SetUnorderedAccessView(0, _uavs[0]);
                else
                    csStage.SetUnorderedAccessViews(0, _uavs, new[] { counter });
            }
            else if (_uavs.Length == 2)
            {
                csStage.SetUnorderedAccessViews(0, _uavs, new[] { 0, 0 });
            }
            else if (_uavs.Length == 3)
            {
                csStage.SetUnorderedAccessViews(0, _uavs, new[] { counter, -1, -1 });
            }
            else
            {
                csStage.SetUnorderedAccessViews(0, _uavs);
            }

            for (int i = 0; i < count; i++)
            {
                deviceContext.Dispatch(dispatchCount.X, dispatchCount.Y, dispatchCount.Z);
            }

            // unbind resources
            for (int i = 0; i < _uavs.Length; i++)
                csStage.SetUnorderedAccessView(i, null);
            for (int i = 0; i < _samplerStates.Length; i++)
                csStage.SetSampler(i, null);
            for (int i = 0; i < _shaderResourceViews.Length; i++)
                csStage.SetShaderResource(i, null);
            for (int i = 0; i < _constantBuffers.Length; i++)
                csStage.SetConstantBuffer(i, null);
            
            _statsUpdateCount++;
            _statsDispatchCount += dispatchCount.X * dispatchCount.Y * dispatchCount.Z;
        }

        private SharpDX.Direct3D11.ComputeShader _cs;
        private Buffer[] _constantBuffers = new Buffer[0];
        private ShaderResourceView[] _shaderResourceViews = new ShaderResourceView[0];
        private SamplerState[] _samplerStates = new SamplerState[0];
        private UnorderedAccessView[] _uavs = new UnorderedAccessView[0];
        
        
        public IEnumerable<(string, int)> GetStats()
        {
            yield return ("Compute shaders", _statsUpdateCount);
            yield return ("Dispatches", _statsDispatchCount);
        }

        public void StartNewFrame()
        {
            _statsUpdateCount = 0;
            _statsDispatchCount = 0;
        }
        
        private static int _statsUpdateCount;
        private static int _statsDispatchCount;
        private static bool _statsRegistered;        

        [Input(Guid = "5c0e9c96-9aba-4757-ae1f-cc50fb6173f1")]
        public readonly InputSlot<T3.Core.DataTypes.ComputeShader> ComputeShader = new();

        [Input(Guid = "180cae35-10e3-47f3-8191-f6ecea7d321c")]
        public readonly InputSlot<Int3> Dispatch = new();

        [Input(Guid = "34cf06fe-8f63-4f14-9c59-35a2c021b817")]
        public readonly MultiInputSlot<Buffer> ConstantBuffers = new();

        [Input(Guid = "88938b09-d5a7-437c-b6e1-48a5b375d756")]
        public readonly MultiInputSlot<ShaderResourceView> ShaderResources = new();

        [Input(Guid = "4047c9e7-1edb-4c71-b85c-c1b87058c81c")]
        public readonly MultiInputSlot<SamplerState> SamplerStates = new();

        [Input(Guid = "599384c2-bf6c-4953-be74-d363292ab1c7")]
        public readonly MultiInputSlot<UnorderedAccessView> Uavs = new();

        [Input(Guid = "0105aca4-5fd5-40c8-82a5-e919bb7dd507")]
        public readonly InputSlot<int> UavBufferCounter = new();
        
        [Input(Guid = "1495157D-601F-4054-84E2-29EBEBB461D8")]
        public readonly InputSlot<int> DispatchCallCount = new();

        
    }
}