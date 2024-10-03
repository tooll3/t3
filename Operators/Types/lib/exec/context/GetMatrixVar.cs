using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_a3700115_a15b_493a_8a07_dab16b4ee0af
{
    public class GetMatrixVar : Instance<GetMatrixVar>
,ICustomDropdownHolder
    {
        [Output(Guid = "1EEAB949-C741-4ABE-A5AA-BE0819097CDA", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector4[]> Result = new();

        public GetMatrixVar()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (VariableName.DirtyFlag.IsDirty)
            {
                _contextVariableNames = context.ObjectVariables
                                               .Where(k => k.Value is Vector4[])
                                               .Select(k=> k.Key)
                                               .ToList();
            }
            
            var variableName = VariableName.GetValue(context);

            if (context.ObjectVariables.TryGetValue(variableName, out var currentValue))
            {
                if(currentValue is Vector4[] vector4Array)
                    Result.Value = vector4Array;
                else
                    Log.Warning($"Can't cast {variableName} to Vector4[]", this);
            }
            else
            {
                Log.Warning($"Can't read undefined Vector4[] {variableName}.", this);
            }
        }
        
        [Input(Guid = "6ff40ed6-68d9-46b8-aaf3-d4276bedfd81")]
        public readonly InputSlot<string> VariableName = new();
        
        
        #region  ICustomDropdownHolder
        
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return VariableName.Value;
        }
        
        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (inputId != VariableName.Input.InputDefinition.Id)
            {
                Log.Warning("Unexpected input id {inputId} in GetOptionsForInput", inputId);
                return new List<string>(); 
            }
            
            // Update the list of available variables when dropdown is shown
            VariableName.DirtyFlag.Invalidate();
            return _contextVariableNames;
        }
        
        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            if (inputId != VariableName.Input.InputDefinition.Id)
            {
                Log.Warning("Unexpected input id {inputId} in HandleResultForInput", inputId);
                return;
            }
            VariableName.SetTypedInputValue(result);
        }
        
        private List<string> _contextVariableNames = new();
        
        #endregion
    }
}