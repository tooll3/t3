using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0946c48b_85d8_4072_8f21_11d17cc6f6cf
{
    public class Vector2ToFloat2 : Instance<Vector2ToFloat2>
    {
        [Output(Guid = "1cee5adb-8c3c-4575-bdd6-5669c04d55ce")]
        public readonly Slot<float> X = new Slot<float>();
        [Output(Guid = "305d321d-3334-476a-9fa3-4847912a4c58")]
        public readonly Slot<float> Y = new Slot<float>();

        public Vector2ToFloat2()
        {
            X.UpdateAction = Update;
            Y.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Vector2 value = Value.GetValue(context);
            X.Value = value.X;
            Y.Value = value.Y;
        }

        [Input(Guid = "36F14238-5BB8-4521-9533-F4D1E8FB802B")]
        public readonly InputSlot<System.Numerics.Vector2> Value = new InputSlot<Vector2>();

    }
}
