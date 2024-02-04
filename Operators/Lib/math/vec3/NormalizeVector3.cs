using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.math.vec3
{
	[Guid("7805285e-e74b-48f5-8228-20bbb178e828")]
    public class NormalizeVector3 : Instance<NormalizeVector3>
    {
        [Output(Guid = "1535925c-45d9-43d6-bf0a-ce7547c6bc4f")]
        public readonly Slot<System.Numerics.Vector3> Result = new();

        
        public NormalizeVector3()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var a = A.GetValue(context);
            var length = a.Length();
            if (length > 0.001f)
            {
                a /= length;    
            }
            var f = Factor.GetValue(context);
            Result.Value = a * f;
        }
        
        [Input(Guid = "2405182f-a918-451a-b039-46e71a541e4d")]
        public readonly InputSlot<System.Numerics.Vector3> A = new();

        [Input(Guid = "8ae77ac6-7417-4f74-9409-0ded96407f23")]
        public readonly InputSlot<float> Factor = new();
        
    }
}
