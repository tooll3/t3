using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.cynic.research
{
	[Guid("fd873111-23b6-458a-918a-eefe990c6fa3")]
    
    public class ForEachCodeTest : Instance<ForEachCodeTest>
    {
        [Output(Guid = "b924240c-bf06-4844-82fd-ae1b90f73053")]
        public readonly Slot<System.Collections.Generic.List<string>> OutputList = new();

        public ForEachCodeTest()
        {
            OutputList.UpdateAction = Update;
            OutputList.Value = new List<string>();
            ElementFunc = (index, indexNorm, element) => indexNorm.ToString() + element.ToUpper();
        }

        public void Update(EvaluationContext context)
        {
            var inputList = Input.GetValue(context);
            var outputList = OutputList.Value;
            outputList.Clear();
            int count = inputList.Count;
            outputList.Capacity = count;

            double indexNorm = 0.0;
            double normIncrement = 1.0 / Math.Max(count - 1, 1);
            for (int index = 0; index < count; index++, indexNorm += normIncrement)
            {
                var element = inputList[index];


                element = ElementFunc(index, indexNorm, element);
                
                
                outputList.Add(element);
            }
        }

        public Func<int, double, string, string> ElementFunc;

        [Input(Guid = "91368258-c25d-4a5f-890a-96a1c6695d74")]
        public readonly InputSlot<System.Collections.Generic.List<string>> Input = new();
    }

    // public class ForEachCodeTest : ForEachCodeTestBase
    // {
        // public ForEachCodeTest()
        // {
            // ElementFunc = (index, indexNorm, element) => indexNorm.ToString() + element.ToUpper();
        // }
    // }
}

