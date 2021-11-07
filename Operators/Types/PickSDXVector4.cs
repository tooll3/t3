using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a83f2e4f_cb4d_4a6f_9f7a_2ea7fdfab54b
{
    public class PickSDXVector4 : Instance<PickSDXVector4>
    {
        [Output(Guid = "B0A0DD4C-90BB-49E9-BA83-E96C3FAB2929")]
        public readonly Slot<float> Value1 = new Slot<float>();

        [Output(Guid = "C46BCD47-C620-4894-8E0D-9202C1790914")]
        public readonly Slot<float> Value2 = new Slot<float>();

        [Output(Guid = "3349F39A-7980-4EFD-849C-70A4C13D5177")]
        public readonly Slot<float> Value3 = new Slot<float>();

        [Output(Guid = "C5EA9711-6326-4EDC-932B-35FD11323E4F")]
        public readonly Slot<float> Value4 = new Slot<float>();

        
        public PickSDXVector4()
        {
            Value1.UpdateAction = Update;
            Value2.UpdateAction = Update;
            Value3.UpdateAction = Update;
            Value4.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var list = Input.GetValue(context);
            if (list == null || list.Length == 0)
            { 
                return;
            }

            var index = Index.GetValue(context);
            if (index < 0)
                index = -index;

            SharpDX.Vector4 v= list[index % list.Length];

            Value1.Value = v[0];
            Value2.Value = v[1];
            Value3.Value = v[2];
            Value4.Value = v[3];

            Value1.DirtyFlag.Clear();
            Value2.DirtyFlag.Clear();
            Value3.DirtyFlag.Clear();
            Value4.DirtyFlag.Clear();
        }

        [Input(Guid = "0f9eebb0-6f13-4751-abac-15a467ad56c2")]
        public readonly InputSlot<SharpDX.Vector4[]> Input = new InputSlot<SharpDX.Vector4[]>();

        [Input(Guid = "dbc92e88-cae2-44a8-b291-1a6168624244")]
        public readonly InputSlot<int> Index = new InputSlot<int>(0);
    }
}