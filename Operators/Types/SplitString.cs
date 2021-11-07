using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a0fcf7ed_1f14_4a8b_a57e_99e5b2407b1b
{
    public class SplitString : Instance<SplitString>
    {
        [Output(Guid = "52745502-3b69-4b2e-be47-d2660fe08e48")]
        public readonly Slot<List<string>> Fragments = new Slot<List<string>>();


        public SplitString()
        {
            Fragments.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var split = Split.GetValue(context);
            var c = (split.Length == 0 || split == "\\n")  
                        ? '\n'
                        : split[0];
            
            var str = String.GetValue(context);
            if (string.IsNullOrEmpty(str))
            {
                Fragments.Value = _emptyList;
                return;
            }
            
            Fragments.Value = str.Split(c).ToList();
        }

        private readonly List<string> _emptyList = new List<string>();

        [Input(Guid = "b1fd8b37-140e-487f-bfe2-bc426d8fe439")]
        public readonly InputSlot<string> String = new InputSlot<string>("Line\nLine");
        
        [Input(Guid = "c54e4b16-b185-41f8-bc50-230b7624d093")]
        public readonly InputSlot<string> Split = new InputSlot<string>("\n");
    }
}