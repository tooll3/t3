using Operators.Utils;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using GeometryShaderD3D = SharpDX.Direct3D11.GeometryShader;

namespace T3.Operators.Types.Id_a908cc64_e8cb_490c_ae45_c2c5fbfcedfb
{
    public class GeometryShader : Instance<GeometryShader>, IShaderOperator<GeometryShaderD3D>
    {
        [Output(Guid = "85B65C27-D5B3-4FE1-88AF-B1F6ABAA4515")]
        public readonly Slot<GeometryShaderD3D> Shader = new();

        public GeometryShader()
        {
            Shader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var updated = ShaderOperatorImpl.TryUpdateShader(context, ref _cachedSource, out _warning);
        }

        [Input(Guid = "258c53e6-7708-49b7-88e2-1e40d2a4f88d")]
        public readonly InputSlot<string> Source = new();

        [Input(Guid = "9675eb2e-ae6a-4826-a53e-07bed7d5b8a0")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "08789371-8193-49af-9ef4-97b12d9e6981")]
        public readonly InputSlot<string> DebugName = new();

        private string _cachedSource, _warning;
        
        #region IShaderOperator implementation
        private IShaderOperator<GeometryShaderD3D> ShaderOperatorImpl => this;
        InputSlot<string> IShaderOperator<GeometryShaderD3D>.Source => Source;
        InputSlot<string> IShaderOperator<GeometryShaderD3D>.EntryPoint => EntryPoint;
        InputSlot<string> IShaderOperator<GeometryShaderD3D>.DebugName => DebugName;
        Slot<GeometryShaderD3D> IShaderOperator<GeometryShaderD3D>.Shader => Shader;
        ShaderResource<GeometryShaderD3D> IShaderOperator<GeometryShaderD3D>.ShaderResource { get; set; }
        bool IShaderOperator<GeometryShaderD3D>.SourceIsSourceCode => false;
        #endregion
    }
}