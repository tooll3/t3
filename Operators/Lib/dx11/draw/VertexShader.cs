using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using VertexShaderD3D = SharpDX.Direct3D11.VertexShader;

namespace lib.dx11.draw
{
	[Guid("646f5988-0a76-4996-a538-ba48054fd0ad")]
    public class VertexShader : Instance<VertexShader>, IDescriptiveFilename, IStatusProvider, IShaderOperator<VertexShaderD3D>
    {
        [Output(Guid = "ED31838B-14B5-4875-A0FC-DC427E874362")]
        public readonly Slot<VertexShaderD3D> Shader = new();

        public VertexShader()
        {
            Shader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var updated = ShaderOperatorImpl.TryUpdateShader(context, ref _cachedSource, out _warningMessage);
        }

        public InputSlot<string> SourcePathSlot => Source;

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_warningMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _warningMessage;
        }

        private string _warningMessage, _cachedSource;

        [Input(Guid = "78FB7501-74D9-4A27-8DB2-596F25482C87")]
        public readonly InputSlot<string> Source = new();

        [Input(Guid = "9A8B500E-C3B1-4BE1-8270-202EF3F90793")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "C8A59CF8-6612-4D57-BCFD-3AEEA351BA50")]
        public readonly InputSlot<string> DebugName = new();

        #region IShaderOperator implementation
        private IShaderOperator<VertexShaderD3D> ShaderOperatorImpl => this;
        InputSlot<string> IShaderOperator<VertexShaderD3D>.Source => Source;
        InputSlot<string> IShaderOperator<VertexShaderD3D>.EntryPoint => EntryPoint;
        InputSlot<string> IShaderOperator<VertexShaderD3D>.DebugName => DebugName;
        Slot<VertexShaderD3D> IShaderOperator<VertexShaderD3D>.Shader => Shader;
        ShaderResource<VertexShaderD3D> IShaderOperator<VertexShaderD3D>.ShaderResource { get; set; }
        bool IShaderOperator<VertexShaderD3D>.SourceIsSourceCode => false;
        Instance IShaderOperator<VertexShaderD3D>.Instance => this;
        #endregion
    }
}