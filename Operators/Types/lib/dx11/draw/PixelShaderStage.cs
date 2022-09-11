using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_75306997_4329_44e9_a17a_050dae532182
{
    public class PixelShaderStage : Instance<PixelShaderStage>
    {
        [Output(Guid = "76E7AD5D-A31D-4B1F-9C42-B63C5161117C", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>(new Command());

        public PixelShaderStage()
        {
            Output.UpdateAction = Update;
            Output.Value.RestoreAction = Restore;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var device = resourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var psStage = deviceContext.PixelShader;

            var ps = PixelShader.GetValue(context);
            
            ConstantBuffers.GetValues(ref _constantBuffers, context);
            ShaderResources.GetValues(ref _shaderResourceViews, context);
            SamplerStates.GetValues(ref _samplerStates, context);

            _prevPixelShader = psStage.Get();
            _prevConstantBuffers = psStage.GetConstantBuffers(0, _constantBuffers.Length);
            _prevShaderResourceViews = psStage.GetShaderResources(0, _shaderResourceViews.Length);
            _prevSamplerStates = psStage.GetSamplers(0, _samplerStates.Length);

            if (ps == null)
                return;

            psStage.Set(ps);
            psStage.SetConstantBuffers(0, _constantBuffers.Length, _constantBuffers);
            psStage.SetShaderResources(0, _shaderResourceViews.Length, _shaderResourceViews);
            psStage.SetSamplers(0, _samplerStates.Length, _samplerStates);
        }

        private void Restore(EvaluationContext context)
        {
            var deviceContext = ResourceManager.Instance().Device.ImmediateContext;
            var psStage = deviceContext.PixelShader;

            psStage.Set(_prevPixelShader);
            psStage.SetConstantBuffers(0, _prevConstantBuffers.Length, _prevConstantBuffers);
            psStage.SetShaderResources(0, _prevShaderResourceViews.Length, _prevShaderResourceViews);
            psStage.SetSamplers(0, _prevSamplerStates.Length, _prevSamplerStates);
        }

        private Buffer[] _constantBuffers = new Buffer[0];
        private ShaderResourceView[] _shaderResourceViews = new ShaderResourceView[0];
        private SamplerState[] _samplerStates = new SamplerState[0];

        private SharpDX.Direct3D11.PixelShader _prevPixelShader;
        private Buffer[] _prevConstantBuffers;
        private ShaderResourceView[] _prevShaderResourceViews;
        private SamplerState[] _prevSamplerStates = new SamplerState[0];

        [Input(Guid = "1B9BE6EB-96C8-4B1C-B854-99B64EAF5618")]
        public readonly InputSlot<SharpDX.Direct3D11.PixelShader> PixelShader = new InputSlot<SharpDX.Direct3D11.PixelShader>();
        [Input(Guid = "BE02A84B-A666-4119-BB6E-FEE1A3DF0981")]
        public readonly MultiInputSlot<Buffer> ConstantBuffers = new MultiInputSlot<Buffer>();
        [Input(Guid = "50052906-4691-4A84-A69D-A109044B5300")]
        public readonly MultiInputSlot<ShaderResourceView> ShaderResources = new MultiInputSlot<ShaderResourceView>();
        [Input(Guid = "C4E91BC6-1691-4EB4-AED5-DD4CAE528149")]
        public readonly MultiInputSlot<SamplerState> SamplerStates = new MultiInputSlot<SamplerState>();
    }
}