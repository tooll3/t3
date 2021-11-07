using System.IO;
using SharpDX;
using SharpDX.D3DCompiler;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a256d70f_adb3_481d_a926_caf35bd3e64c
{
    public class ComputeShader : Instance<ComputeShader>, IDescriptiveGraphNode
    {
        [Output(Guid = "{6C118567-8827-4422-86CC-4D4D00762D87}")]
        public readonly Slot<SharpDX.Direct3D11.ComputeShader> ComputerShader = new Slot<SharpDX.Direct3D11.ComputeShader>();
        
        [Output(Guid = "a6fe06e0-b6a9-463c-9e62-930c58b0a0a1")]
        public readonly Slot<SharpDX.Int3> ThreadCount = new Slot<SharpDX.Int3>();

        private uint _computeShaderResId;
        public ComputeShader()
        {
            ComputerShader.UpdateAction = Update;
        }
        
        public string GetDescriptiveString()
        {
            return _description;
        }

        private string _description = "ComputeShader";

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                string sourcePath = Source.GetValue(context);
                string entryPoint = EntryPoint.GetValue(context);
                string debugName = DebugName.GetValue(context);
                if (string.IsNullOrEmpty(debugName) && !string.IsNullOrEmpty(sourcePath))
                {
                    debugName = new FileInfo(sourcePath).Name;
                }
                _computeShaderResId = resourceManager.CreateComputeShaderFromFile(sourcePath, entryPoint, debugName,
                                                                                  () => ComputerShader.DirtyFlag.Invalidate());
                Log.Debug($"compute shader {sourcePath}:{entryPoint}", SymbolChildId);

                try
                {
                    _description =  "ComputeShader\n" + Path.GetFileName(sourcePath);
                }
                catch
                {
                    Log.Warning($"Unable to get filename from {sourcePath}", SymbolChildId);
                }
            }
            else
            {
                resourceManager.UpdateComputeShaderFromFile(Source.Value, _computeShaderResId, ref ComputerShader.Value);
            }

            if (_computeShaderResId != ResourceManager.NullResource)
            {
                ComputerShader.Value = resourceManager.GetComputeShader(_computeShaderResId);
                var shaderReflection = new ShaderReflection(resourceManager.GetComputeShaderBytecode(_computeShaderResId));
                shaderReflection.GetThreadGroupSize(out int x, out int y, out int z);
                ThreadCount.Value = new Int3(x, y, z);
            }
        }

        [Input(Guid = "{AFB69C81-5063-4CB9-9D42-841B994B5EC0}")]
        public readonly InputSlot<string> Source = new InputSlot<string>();

        [Input(Guid = "{8AD9E58D-A767-4A5F-BFBF-D082B80901D6}")]
        public readonly InputSlot<string> EntryPoint = new InputSlot<string>();

        [Input(Guid = "{C0701D0B-D37F-4570-9E9A-EC2E88B919D1}")]
        public readonly InputSlot<string> DebugName = new InputSlot<string>();
    }
}