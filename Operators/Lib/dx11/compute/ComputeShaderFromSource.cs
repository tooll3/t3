using Operators.Utils;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using ComputeShaderD3D = SharpDX.Direct3D11.ComputeShader;

namespace T3.Operators.Types.Id_4e5bc624_9cda_46a8_9681_7fd412ea3893
{
    public class ComputeShaderFromSource : Instance<ComputeShaderFromSource>, IStatusProvider, IShaderOperator<ComputeShaderD3D>
    {
        [Output(Guid = "190e262f-6554-4b34-b5b6-6617a98ab123")]
        public readonly Slot<ComputeShaderD3D> ComputerShader = new();

        [Output(Guid = "a3e0a72f-68d0-4278-8b9b-f4cf33603305")]
        public readonly Slot<Int3> ThreadCount = new();

        public ComputeShaderFromSource()
        {
            ComputerShader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var updated = ShaderOperatorImpl.TryUpdateShader(context, ref _code, out _warningMessage);

            if (updated)
            {
                if (ShaderOperatorImpl.ShaderResource.TryGetThreadGroups(out var threadCount))
                    ThreadCount.Value = threadCount;
            }
        }

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_warningMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _warningMessage;
        }

        private string _code;
        private string _warningMessage;

        [Input(Guid = "a8ee59c3-cb62-42e5-a3c9-f4968876c9cc")]
        public readonly InputSlot<string> ShaderSource = new();

        [Input(Guid = "d1cbd9eb-5e5a-499d-b7af-0cfe283f896b")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "08399b7a-a390-4a11-83eb-36ac68f76bc6")]
        public readonly InputSlot<string> DebugName = new();

        #region IShaderOperator implementation
        private IShaderOperator<ComputeShaderD3D> ShaderOperatorImpl => this;
        InputSlot<string> IShaderOperator<ComputeShaderD3D>.Source => ShaderSource;
        InputSlot<string> IShaderOperator<ComputeShaderD3D>.EntryPoint => EntryPoint;
        InputSlot<string> IShaderOperator<ComputeShaderD3D>.DebugName => DebugName;
        Slot<ComputeShaderD3D> IShaderOperator<ComputeShaderD3D>.Shader => ComputerShader;
        ShaderResource<ComputeShaderD3D> IShaderOperator<ComputeShaderD3D>.ShaderResource { get; set; }
        bool IShaderOperator<ComputeShaderD3D>.SourceIsSourceCode => true;
        #endregion
    }
}