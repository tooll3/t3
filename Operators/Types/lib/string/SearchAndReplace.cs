using System;
using System.Text.RegularExpressions;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b7910fc6_c3b2_4daf_93cd_010dcfe22a57
{
    public class SearchAndReplace : Instance<SearchAndReplace>
    {
        [Output(Guid = "15672e8f-c483-432e-8ced-f2bd18c1be67")]
        public readonly Slot<string> Result = new();

        public SearchAndReplace()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var content = OriginalString.GetValue(context);
            var replacement = Replace.GetValue(context)?.Replace("\\n","\n");
            var pattern = SearchPattern.GetValue(context);
            if (string.IsNullOrEmpty(content) 
                || string.IsNullOrEmpty(replacement)
                || string.IsNullOrEmpty(pattern))
            {
                Result.Value = string.Empty;
                return;
            }
            
            try
            {
                Result.Value= Regex.Replace(content, pattern, replacement, RegexOptions.Multiline| RegexOptions.Singleline);
            }
            catch (Exception)
            {
                Log.Error($"'{pattern}' is an incorrect search pattern", this);
            }
        }
        
        [Input(Guid = "3ca66cbd-a16a-479c-b858-84732e5023ad")]
        public readonly InputSlot<string> OriginalString = new();
        
        [Input(Guid = "4FE3F641-1C36-4970-BE71-DAFB5632FB53")]
        public readonly InputSlot<string> SearchPattern = new();
        
        [Input(Guid = "DE8297AE-C7D8-414A-8825-D0FF9C2E3D78")]
        public readonly InputSlot<string> Replace = new();
    }
}