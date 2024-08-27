using System;
using System.Collections.Generic;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_05295c65_7dfd_4570_866e_9b5c4e735569
{
    public class SelectBoolFromFloatDict : Instance<SelectBoolFromFloatDict>
,IStatusProvider,ICustomDropdownHolder
    {

        [Output(Guid = "2C0F90BB-E7C8-43A7-A43F-9BAB5222753B")]
        public readonly Slot<bool> Result = new(false);

        
        public SelectBoolFromFloatDict()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            _dict = DictionaryInput.GetValue(context);
            _selectCommand = Select.GetValue(context);
            
            var floatValue = 0.0f;
            _dict?.TryGetValue(_selectCommand, out floatValue);

            Result.Value = floatValue > 0.5f;
        }

        private Dict<float> _dict;
        private string _selectCommand;
        
        
        #region implement status provider
        
        #region select dropdown
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return Select.Value;
        }

        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (inputId != Select.Id || _dict == null)
            {
                yield return "";
                yield break;
            }

            foreach (var key in _dict.Keys)
            {
                yield return key;
            }
        }

        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            Select.SetTypedInputValue(result);
        }
        #endregion        
        

        public IStatusProvider.StatusLevel GetStatusLevel() => _statusLevel;
        public string GetStatusMessage() => _lastWarningMessage;

        private string _lastWarningMessage = "Not updated yet.";
        private IStatusProvider.StatusLevel _statusLevel;
        #endregion
        
        
        [Input(Guid = "e09e3a45-e1de-430f-93ef-9d2412b22cda")]
        public readonly InputSlot<Dict<float>> DictionaryInput = new();

        [Input(Guid = "132db9de-cffe-4379-af85-2aca18b4f6e0")]
        public readonly InputSlot<string> Select = new();
    }
}
