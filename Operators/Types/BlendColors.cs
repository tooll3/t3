using System;
using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_6b7c541a_ca36_4f21_ac95_89e874820c5a
{
    public class BlendColors : Instance<BlendColors>
    {
        [Output(Guid = "66ce8660-253c-4a0b-8aec-f7a56751a1e4")]
        public readonly Slot<Vector4> Color = new Slot<Vector4>();

        public BlendColors()
        {
            Color.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var a = ColorA.GetValue(context);
            var b = ColorB.GetValue(context);
            var m = Factor.GetValue(context);
            var mode = (Modes)Mode.GetValue(context);

            Vector4 result; 
            switch (mode)
            {
                case Modes.Mix:
                    //result = Color.Value;
                    result = a * (1-m) + b * m;
                    break;
                case Modes.Multiply:
                    result = a *  Vector4.Lerp(new Vector4(1), b, m);
                    break;
                
                case Modes.Add:
                    result = a +  b * m;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
                
            }

            Color.Value = result; //Vector4.Max(result, Vector4.Zero);
        }
        
        [Input(Guid = "EB601C57-2025-4135-8292-223EAEDAF187")]
        public readonly InputSlot<Vector4> ColorA = new InputSlot<Vector4>();
        
        [Input(Guid = "B9E5C6F3-7052-456F-9D1B-C182B4412433")]
        public readonly InputSlot<Vector4> ColorB = new InputSlot<Vector4>();
        
        [Input(Guid = "40803D0E-C37C-4B5D-B64B-FD1EF090A4F7")]
        public readonly InputSlot<float> Factor = new InputSlot<float>(1f);

        [Input(Guid = "8D444E8C-D9B5-4206-9202-5ABF23B13BAF", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        private enum Modes
        {
            Mix,
            Multiply,
            Add,
        }
    }
}