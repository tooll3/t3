using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_05cf9ea7_045d_421f_8ed3_2c2f6b325a46 
{
    public class CompareInt : Instance<CompareInt>
    {
        [Output(Guid = "ff14eb99-aafd-46e1-9d24-ca6647f700d1")]
        public readonly Slot<bool> IsTrue = new Slot<bool>();

        public CompareInt()
        {
            IsTrue.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            var test = TestValue.GetValue(context);
            //var mod = Mod.GetValue(context);
            switch ((Modes)Mode.GetValue(context))
            {
                case Modes.IsSmaller:
                    IsTrue.Value =  v < test;
                    break;
                case Modes.IsEqual:
                    IsTrue.Value =  Math.Abs(v-test)< Precision.GetValue(context);
                    break;
                case Modes.IsLarger:
                    IsTrue.Value =  v > test;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum Modes
        {
            IsSmaller,
            IsEqual,
            IsLarger,
        }
        
        [Input(Guid = "e2d09267-1227-40ed-8c91-0d44f4e00ee5")]
        public readonly InputSlot<float> Value = new InputSlot<float>();

        [Input(Guid = "9814676e-07dc-40de-98a5-f69b94f58f89")]
        public readonly InputSlot<float> TestValue = new InputSlot<float>();
        
        [Input(Guid = "5bf37ae4-bb84-42ee-96f9-52c2adefa669")]
        public readonly InputSlot<int> Mode = new InputSlot<int>();
        
        [Input(Guid = "e4ccfad6-6894-4abe-a8f6-742af53b1a60")]
        public readonly InputSlot<float> Precision = new InputSlot<float>(0.001f);

    }
}
