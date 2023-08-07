using System;
using System.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_f7c625da_fede_4993_976c_e259e0ee4985
{
    public class PixelShader : Instance<PixelShader>, IDescriptiveFilename, IStatusProvider
    {
        [Output(Guid = "9C6E72F8-5CE6-42C3-ABAA-1829D2C066C1")]
        public readonly Slot<SharpDX.Direct3D11.PixelShader> Shader = new();

        private uint _pixelShaderResId;

        public PixelShader()
        {
            Shader.UpdateAction = Update;
        }

        
        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            var success = true;
            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                _sourcePath = Source.GetValue(context);
                var entryPoint = EntryPoint.GetValue(context);
                var debugName = DebugName.GetValue(context);
                if (string.IsNullOrEmpty(debugName) && !string.IsNullOrEmpty(_sourcePath))
                {
                    try
                    {
                        debugName = new FileInfo(_sourcePath).Name;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Invalid _sourcePath for shader: {_sourcePath}: " + e.Message);
                        return;
                    }
                }
                success= resourceManager.CreatePixelShaderFromFile(out _pixelShaderResId, _sourcePath, entryPoint, debugName,
                                                                              () => Shader.DirtyFlag.Invalidate());
            }
            else
            {
                _warningMessage = ResourceManager.LastShaderError;
                success = ResourceManager.UpdatePixelShaderFromFile(Source.Value, _pixelShaderResId, ref Shader.Value);
            }

            if (!success || _pixelShaderResId == ResourceManager.NullResource)
            {
                //Log.Debug("Compiling pixel shader failed");
                if (string.IsNullOrEmpty(_sourcePath) || !File.Exists(_sourcePath))
                {
                    _warningMessage = $"Can't read file {_sourcePath}";
                }
                else
                {
                    _warningMessage = ResourceManager.LastShaderError;
                }
                return;
            }
            
            _warningMessage = null;
            Shader.Value = resourceManager.GetPixelShader(_pixelShaderResId);
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
        private string _warningMessage;
        private string _sourcePath;
        
        [Input(Guid = "24646F06-1509-43CE-94C6-EEB608AD97CD")]
        public readonly InputSlot<string> Source = new();

        [Input(Guid = "501338B3-F432-49A5-BDBD-BCF209671305")]
        public readonly InputSlot<string> EntryPoint = new();

        [Input(Guid = "BE9B3DC1-7122-4B3D-B936-CCCF2581B69E")]
        public readonly InputSlot<string> DebugName = new();
    }
}