using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.math.vec3
{
	[Guid("646d6a5d-a1d7-471e-86ab-e1fb2542a2c2")]
    public class ScaleVector3 : Instance<ScaleVector3>
    {
        [Output(Guid = "956F31AA-8C84-4D2D-9FC7-E63D753F6941")]
        public readonly Slot<System.Numerics.Vector3> Result = new();

        
        public ScaleVector3()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var a = A.GetValue(context);
            var b = B.GetValue(context);
            Result.Value = a * b;
        }
        
        [Input(Guid = "DE6BFE5A-EBCD-4DA6-8C8A-79989A31DD9F")]
        public readonly InputSlot<System.Numerics.Vector3> A = new();

        [Input(Guid = "4AB40AA5-B390-4042-A959-8EDDF9CBC9B0")]
        public readonly InputSlot<float> B = new();
        
    }
}
