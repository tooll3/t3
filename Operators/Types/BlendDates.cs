using System;
using System.Globalization;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_9cb4d49e_135b_400b_a035_2b02c5ea6a72;

namespace T3.Operators.Types.Id_d3080dc9_98dc_43d5_be08_2ddfe971de98
{
    public class BlendDates : Instance<BlendDates>
    {
        [Output(Guid = "7a1575c3-5c35-4b0b-bde7-5746a3172502")]
        public readonly Slot<string> Output = new Slot<string>();

        public BlendDates()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var now = DateTime.Now;

            try
            {
                var timeA = DateTime.Today;
                var timeB = DateTime.Today;
                var timeAString = TimeA.GetValue(context);
                var timeBString = TimeB.GetValue(context);
                var mix = Mix.GetValue(context);
                
                if(DateTime.TryParse(timeAString, out var d1))
                {
                    timeA = d1;
                }
                else
                {
                    Log.Warning($"invalid format for lauchTime '{timeAString}'");
                }
                
                if(DateTime.TryParse(timeBString, out var d2))
                {
                    timeB = d2;
                }
                else
                {
                    Log.Warning($"invalid format for lauchTime '{timeBString}'");
                }
                
                //var v = Duration.GetValue(context);
                var duration = timeB - timeA;
                
                var format = Format.GetValue(context);
                
                var outString = (timeA + TimeSpan.FromHours(duration.TotalHours * mix))
                   .ToString(format, CultureInfo.InvariantCulture);
                
                Output.Value = outString;
            }
            catch (System.FormatException)
            {
                Output.Value = "Invalid Format";
            }
        }

        [Input(Guid = "cf78f62d-afae-4b5a-9de6-ad26f2b34317")]
        public readonly InputSlot<string> TimeA = new InputSlot<string>();

        [Input(Guid = "F2A0AAD4-DDA9-4E75-B62C-E5472EF925E5")]
        public readonly InputSlot<string> TimeB = new InputSlot<string>();

        [Input(Guid = "0F980D15-CBAE-481E-8EB5-DEDC334F9733")]
        public readonly InputSlot<float> Mix = new InputSlot<float>();

        [Input(Guid = "67c37764-7897-4047-8ba1-0dfe21d5e616")]
        public readonly InputSlot<string> Format = new InputSlot<string>();
        

    }
}