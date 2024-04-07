using System.Runtime.InteropServices;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using ComputeShaderD3D = SharpDX.Direct3D11.ComputeShader;

namespace lib.dx11.compute
{
	[Guid("a256d70f-adb3-481d-a926-caf35bd3e64c")]
    public class ComputeShader : Instance<ComputeShader>, IDescriptiveFilename, IStatusProvider, IShaderOperator<ComputeShaderD3D>
    {
        [Output(Guid = "{6C118567-8827-4422-86CC-4D4D00762D87}")]
        public readonly Slot<ComputeShaderD3D> Shader = new();

        [Output(Guid = "a6fe06e0-b6a9-463c-9e62-930c58b0a0a1")]
        public readonly Slot<Int3> ThreadCount = new();

        public ComputeShader()
        {
            Shader.UpdateAction = Update;
            ThreadCount.UpdateAction = Update;
        }

        public InputSlot<string> SourcePathSlot => Source;

        private void Update(EvaluationContext context)
        {
            var updated = ShaderOperatorImpl.TryUpdateShader(context, ref _sourcePath, out _statusWarning);

            if (updated)
            {
                if (ShaderOperatorImpl.ShaderResource.TryGetThreadGroups(out var threadCount))
                    ThreadCount.Value = threadCount;
            }

            Shader.DirtyFlag.Clear();
            ThreadCount.DirtyFlag.Clear();
        }

        [Input(Guid = "{AFB69C81-5063-4CB9-9D42-841B994B5EC0}")]
        public readonly InputSlot<string> Source = new();

        [Input(Guid = "{8AD9E58D-A767-4A5F-BFBF-D082B80901D6}")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "{C0701D0B-D37F-4570-9E9A-EC2E88B919D1}")]
        public readonly InputSlot<string> DebugName = new();

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_statusWarning) ? IStatusProvider.StatusLevel.Undefined : IStatusProvider.StatusLevel.Warning;
        }

        public string GetStatusMessage()
        {
            return _statusWarning;
        }

        private string _statusWarning;
        private string _sourcePath;
        public IEnumerable<string> FileFilter => FileFilters;
        private static readonly string[] FileFilters = ["*.hlsl"];
        #region IShaderOperator implementation
        private IShaderOperator<ComputeShaderD3D> ShaderOperatorImpl => this;
        InputSlot<string> IShaderOperator<ComputeShaderD3D>.Source => Source;
        InputSlot<string> IShaderOperator<ComputeShaderD3D>.EntryPoint => EntryPoint;
        InputSlot<string> IShaderOperator<ComputeShaderD3D>.DebugName => DebugName;
        Slot<ComputeShaderD3D> IShaderOperator<ComputeShaderD3D>.Shader => Shader;
        ShaderResource<ComputeShaderD3D> IShaderOperator<ComputeShaderD3D>.ShaderResource { get; set; }
        bool IShaderOperator<ComputeShaderD3D>.SourceIsSourceCode => false;
        Instance IShaderOperator<ComputeShaderD3D>.Instance => this;
        #endregion
    }
}