using System;
using SharpDX.Direct3D11;
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
            ComputerShader.UpdateAction = UpdateComputeShader;

            // this ensures for now that an asynchronous change of the compute shader resource is picked up
            // probably a different handling is needed later on, either use a callback or simply output
            // the resource id instead of the shader object. The user has then to get the shader from the
            // resource manager which would always be the actual one
            ComputerShader.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
        }

        private void UpdateComputeShader(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty)
            {
                string sourcePath = Source.GetValue(context);
                string entryPoint = EntryPoint.GetValue(context);
                _computeShaderResId = resourceManager.CreateComputeShader(sourcePath, entryPoint, "bla");
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
    }
}