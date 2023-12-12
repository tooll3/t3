using System.IO;
using SharpDX.D3DCompiler;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_a256d70f_adb3_481d_a926_caf35bd3e64c
{
    public class ComputeShader : Instance<ComputeShader>, IDescriptiveFilename, IStatusProvider
    {
        [Output(Guid = "{6C118567-8827-4422-86CC-4D4D00762D87}")]
        public readonly Slot<SharpDX.Direct3D11.ComputeShader> ComputerShader = new Slot<SharpDX.Direct3D11.ComputeShader>();
        
        [Output(Guid = "a6fe06e0-b6a9-463c-9e62-930c58b0a0a1")]
        public readonly Slot<Int3> ThreadCount = new Slot<Int3>();

        private uint _computeShaderResId;
        public ComputeShader()
        {
            ComputerShader.UpdateAction = Update;
            ThreadCount.UpdateAction = Update;
        }
        
        public InputSlot<string> GetSourcePathSlot()
        {
            return Source;
        }


        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            bool success;
            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                _sourcePath = Source.GetValue(context);
                var entryPoint = EntryPoint.GetValue(context);
                var debugName = DebugName.GetValue(context);
                if (string.IsNullOrEmpty(debugName) && !string.IsNullOrEmpty(_sourcePath))
                {
                    debugName = new FileInfo(_sourcePath).Name;
                }
                success= resourceManager.CreateComputeShaderFromFile(out _computeShaderResId, _sourcePath, entryPoint, debugName,
                                                                           () => ComputerShader.DirtyFlag.Invalidate());
            }
            else
            {
                success= ResourceManager.UpdateComputeShaderFromFile(Source.Value, _computeShaderResId, ref ComputerShader.Value);
            }

            if (success && _computeShaderResId != ResourceManager.NullResource)
            {
                ComputerShader.Value = resourceManager.GetComputeShader(_computeShaderResId);
                var computeShaderBytecode = resourceManager.GetComputeShaderBytecode(_computeShaderResId);
                if (computeShaderBytecode != null)
                {
                    var shaderReflection = new ShaderReflection(computeShaderBytecode);
                    shaderReflection.GetThreadGroupSize(out int x, out int y, out int z);
                    ThreadCount.Value = new Int3(x, y, z);
                    _statusWarning = null;
                }
                else
                {
                    _statusWarning = "Failed to access shader bytecode";
                    Log.Warning(_statusWarning, this);
                    ComputerShader.Value = null;
                }
            }
            else
            {
                _statusWarning = !File.Exists(_sourcePath) 
                                     ? $"Source file not found {_sourcePath}" 
                                     : "Compiling not successful\n" + ResourceManager.LastShaderError;
            }
            ComputerShader.DirtyFlag.Clear();
            ThreadCount.DirtyFlag.Clear();
        }

        [Input(Guid = "{AFB69C81-5063-4CB9-9D42-841B994B5EC0}")]
        public readonly InputSlot<string> Source = new InputSlot<string>();

        [Input(Guid = "{8AD9E58D-A767-4A5F-BFBF-D082B80901D6}")]
        public readonly InputSlot<string> EntryPoint = new InputSlot<string>();

        [Input(Guid = "{C0701D0B-D37F-4570-9E9A-EC2E88B919D1}")]
        public readonly InputSlot<string> DebugName = new InputSlot<string>();

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
    }
}