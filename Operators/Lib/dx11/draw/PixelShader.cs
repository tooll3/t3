using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using PixelShaderD3D = SharpDX.Direct3D11.PixelShader;

namespace lib.dx11.draw
{
	[Guid("f7c625da-fede-4993-976c-e259e0ee4985")]
    public class PixelShader : Instance<PixelShader>, IDescriptiveFilename, IStatusProvider, IShaderOperator<PixelShaderD3D>
    {
        [Output(Guid = "9C6E72F8-5CE6-42C3-ABAA-1829D2C066C1")]
        public readonly Slot<PixelShaderD3D> Shader = new();
        
        [Output(Guid = "5D24B1D4-79E4-4AF9-BBC3-78F9ACE1BE98")]
        public readonly Slot<string> Warning = new();

        public PixelShader()
        {
            Shader.UpdateAction = Update;
            Warning.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            var updated = ShaderOperatorImpl.TryUpdateShader(context, ref _sourcePath, out var warningMessage);

            if (warningMessage != _warningMessage)
            {
                Warning.Value = _warningMessage;
                _warningMessage = warningMessage;
            }
        }

        public InputSlot<string> GetSourcePathSlot()
        {
            return Source;
        }

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_warningMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _warningMessage;
        }
        
        private string _warningMessage = string.Empty;
        private string _sourcePath = string.Empty;

        [Input(Guid = "24646F06-1509-43CE-94C6-EEB608AD97CD")]
        public readonly InputSlot<string> Source = new();

        [Input(Guid = "501338B3-F432-49A5-BDBD-BCF209671305")]
        public readonly InputSlot<string> EntryPoint  = new();

        [Input(Guid = "BE9B3DC1-7122-4B3D-B936-CCCF2581B69E")]
        public readonly InputSlot<string> DebugName = new();


        #region IShaderOperator implementation
        private IShaderOperator<PixelShaderD3D> ShaderOperatorImpl => this;
        InputSlot<string> IShaderOperator<PixelShaderD3D>.Source => Source;
        InputSlot<string> IShaderOperator<PixelShaderD3D>.EntryPoint => EntryPoint;
        InputSlot<string> IShaderOperator<PixelShaderD3D>.DebugName => DebugName;
        Slot<PixelShaderD3D> IShaderOperator<PixelShaderD3D>.Shader => Shader;
        ShaderResource<PixelShaderD3D> IShaderOperator<PixelShaderD3D>.ShaderResource { get; set; }
        Instance IShaderOperator<PixelShaderD3D>.Instance => this;
        bool IShaderOperator<PixelShaderD3D>.SourceIsSourceCode => false;
        #endregion
    }
}