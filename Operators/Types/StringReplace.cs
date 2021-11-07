using System;
using System.Text.RegularExpressions;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b7910fc6_c3b2_4daf_93cd_010dcfe22a57
{
    public class StringReplace : Instance<StringReplace>
    {
        [Output(Guid = "15672e8f-c483-432e-8ced-f2bd18c1be67")]
        public readonly Slot<string> Result = new Slot<string>();

        public StringReplace()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var content = OriginalString.GetValue(context);
            if (string.IsNullOrEmpty(content))
            {
                Result.Value = string.Empty;
                return;
            }
            
            
            //const string pattern = @"(-)(\d+)(-)";
            var pattern = SearchPattern.GetValue(context);
            
            var replace = Replace.GetValue(context).Replace("\\n","\n");
            
            try
            {
                Result.Value= Regex.Replace(content, pattern, replace, RegexOptions.Multiline| RegexOptions.Singleline);
            }
            catch (Exception)
            {
                Log.Error($"'{pattern}' is an incorrect search pattern", SymbolChildId);
            }
        }
        
        [Input(Guid = "3ca66cbd-a16a-479c-b858-84732e5023ad")]
        public readonly InputSlot<string> OriginalString = new InputSlot<string>();
        
        [Input(Guid = "4FE3F641-1C36-4970-BE71-DAFB5632FB53")]
        public readonly InputSlot<string> SearchPattern = new InputSlot<string>();
        
        [Input(Guid = "DE8297AE-C7D8-414A-8825-D0FF9C2E3D78")]
        public readonly InputSlot<string> Replace = new InputSlot<string>();
    }
}