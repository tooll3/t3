using T3.Core;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class ComputeShader : Instance<ComputeShader>
    {
        [Output(Guid = "{6C118567-8827-4422-86CC-4D4D00762D87}")]
        public readonly Slot<SharpDX.Direct3D11.ComputeShader> ComputerShader = new Slot<SharpDX.Direct3D11.ComputeShader>();

        private uint _computeShaderResId;
        public ComputeShader()
        {
            ComputerShader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                string sourcePath = Source.GetValue(context);
                string entryPoint = EntryPoint.GetValue(context);
                string debugName = DebugName.GetValue(context);
                _computeShaderResId = resourceManager.CreateComputeShaderFromFile(sourcePath, entryPoint, debugName,
                                                                                  () => ComputerShader.DirtyFlag.Invalidate());
            }
            else
            {
                resourceManager.UpdateComputeShaderFromFile(Source.Value, _computeShaderResId, ref ComputerShader.Value);
            }

            if (_computeShaderResId != ResourceManager.NULL_RESOURCE)
            {
                ComputerShader.Value = resourceManager.GetComputeShader(_computeShaderResId);
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