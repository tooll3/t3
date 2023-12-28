using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b732a668_8994_4f34_ab96_5245d355e33f
{
    public class Vec2ToVec3 : Instance<Vec2ToVec3>
    {
        [Output(Guid = "E12A6D13-3DC5-490A-B8C8-677B4671EE73")]
        public readonly Slot<System.Numerics.Vector3> Result = new ();

        public Vec2ToVec3()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var a = XY.GetValue(context);
            var z = Z.GetValue(context);
            Result.Value = new Vector3(a.X, a.Y, z);

        }
        
        [Input(Guid = "a71e7512-08ac-40b4-abdc-5b0835472e7f")]
        public readonly InputSlot<System.Numerics.Vector2> XY = new();
        
        [Input(Guid = "F1E036D8-61BC-4FDC-BB87-5A8A27982D65")]
        public readonly InputSlot<float> Z = new();
        
    }
}
