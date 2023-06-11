using SharpDX;
using SharpDX.D3DCompiler;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_4e5bc624_9cda_46a8_9681_7fd412ea3893
{
    public class ComputeShaderFromSource : Instance<ComputeShaderFromSource>, IStatusProvider
    {
        [Output(Guid = "190e262f-6554-4b34-b5b6-6617a98ab123")]
        public readonly Slot<SharpDX.Direct3D11.ComputeShader> ComputerShader = new();

        [Output(Guid = "a3e0a72f-68d0-4278-8b9b-f4cf33603305")]
        public readonly Slot<SharpDX.Int3> ThreadCount = new();

        private uint _computeShaderResId;

        public ComputeShaderFromSource()
        {
            ComputerShader.UpdateAction = Update;
        }
        
        
        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            if (ShaderSource.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                var shaderSource = ShaderSource.GetValue(context);
                var entryPoint = EntryPoint.GetValue(context);
                var debugName = DebugName.GetValue(context);

                if (_code != shaderSource)
                {
                    _code = shaderSource;
                    if (string.IsNullOrEmpty(debugName) && !string.IsNullOrEmpty(shaderSource))
                    {
                        debugName = "customComputeShader"; //new FileInfo(shaderSource).Name;
                    }
                    
                    var success = resourceManager.CreateComputeShaderFromSource(shaderSource: shaderSource,
                                                                                name: debugName,
                                                                                entryPoint: entryPoint,
                                                                                ref _computeShaderResId);
                }
            }

            if (_computeShaderResId == ResourceManager.NullResource)
            {
                _warningMessage = ResourceManager.LastShaderError;
                return;
            }

            _warningMessage = null;
            
            ComputerShader.Value = resourceManager.GetComputeShader(_computeShaderResId);
            
            var shaderReflection = new ShaderReflection(resourceManager.GetComputeShaderBytecode(_computeShaderResId));
            shaderReflection.GetThreadGroupSize(out int x, out int y, out int z);
            ThreadCount.Value = new Int3(x, y, z);
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


    }
}