using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9a36d1c3_cf55_4f3a_ae09_2f54eecb4642
{
    public class RecycleBuffer : Instance<RecycleBuffer>, IStatusProvider
    {
        [Output(Guid = "49E481DF-E23B-4D1B-AE94-2ACC67C56F9E")]
        public readonly Slot<object> Reference = new();
        
        [Output(Guid = "982016A9-C275-45F1-860E-9F1C3EC4BBFC", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<BufferWithViews> Buffer = new();
        
        
        public RecycleBuffer()
        {
            Reference.UpdateAction = UpdateTexture;
            Buffer.UpdateAction = UpdateTexture;
        }
        
        private void UpdateTexture(EvaluationContext context)
        {
            Reference.Value = _bufferList;
            
            if (_bufferList == null || _bufferList.Count != 1 || _bufferList[0] == null)
            {
                _lastErrorMessage= "Buffer list was not initialized correctly";
                return;
            }
            
            Buffer.Value = _bufferList[0]; 
            // Buffer.DirtyFlag.Clear();
            _lastErrorMessage = null;
        }

        private readonly RenderTargetReference _renderTargetReference = new();
        IStatusProvider.StatusLevel IStatusProvider.GetStatusLevel() => string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
        
        string IStatusProvider.GetStatusMessage() => _lastErrorMessage;
        
        private string _lastErrorMessage;
        private readonly List<BufferWithViews> _bufferList = new() { new BufferWithViews() };
    }
}