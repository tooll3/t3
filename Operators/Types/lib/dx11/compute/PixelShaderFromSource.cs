using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using PixelShaderD3D = SharpDX.Direct3D11.PixelShader;

namespace T3.Operators.Types.Id_9f784a4a_857f_41ad_afc1_0de08c1cfec6
{
    public class PixelShaderFromSource : Instance<PixelShaderFromSource>, IShaderOperator<PixelShaderD3D>
    {
        [Output(Guid = "C513F15D-3A7E-4501-B825-EF3E585293C7")]
        public readonly Slot<PixelShaderD3D> PixelShader = new();

        public PixelShaderFromSource()
        {
            PixelShader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var updated = ShaderOperatorImpl.TryUpdateShader(context, ref _source, out _);
        }

        [Input(Guid = "a192e8cc-2874-4f02-bbf1-4622e99666e1")]
        public readonly InputSlot<string> ShaderSource = new();

        [Input(Guid = "2b616fb0-2966-45a9-a0cc-da960ca509cf")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "baa49d7d-127c-4c93-ae90-7e4db3598af9")]
        public readonly InputSlot<string> DebugName = new();

        private string _source;

        #region IShaderOperator implementation
        private IShaderOperator<PixelShaderD3D> ShaderOperatorImpl => this;
        InputSlot<string> IShaderOperator<PixelShaderD3D>.Source => ShaderSource;
        InputSlot<string> IShaderOperator<PixelShaderD3D>.EntryPoint => EntryPoint;
        InputSlot<string> IShaderOperator<PixelShaderD3D>.DebugName => DebugName;
        Slot<PixelShaderD3D> IShaderOperator<PixelShaderD3D>.Shader => PixelShader;
        ShaderResource<PixelShaderD3D> IShaderOperator<PixelShaderD3D>.ShaderResource { get; set; }
        bool IShaderOperator<PixelShaderD3D>.SourceIsSourceCode => true;
        Instance IShaderOperator<PixelShaderD3D>.Instance => this;
        #endregion
    }
}