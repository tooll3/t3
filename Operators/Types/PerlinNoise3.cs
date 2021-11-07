using System;
using System.Numerics;
using System.Security.Policy;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_50aab941_0a29_474a_affd_13a74ea0c780
{
    public class PerlinNoise3 : Instance<PerlinNoise3>
    {
        [Output(Guid = "1666BC49-4DAE-4CB0-900B-80B50F913117")]
        public readonly Slot<Vector3> Result = new Slot<Vector3>();

        public PerlinNoise3()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var value = Value.GetValue(context);
            var seed = Seed.GetValue(context);
            var period = Frequency.GetValue(context);
            var octaves = Octaves.GetValue(context);
            var rangeMin = RangeMin.GetValue(context);
            var rangeMax = RangeMax.GetValue(context);
            var scale = Scale.GetValue(context);
            var scaleXYZ = ScaleXYZ.GetValue(context);

            Result.Value  = new Vector3(
                                          (MathUtils.PerlinNoise(value, period, octaves, seed) + 1f) * 0.5f * (rangeMax.X - rangeMin.X) + rangeMin.X,
                                          (MathUtils.PerlinNoise(value, period, octaves, seed+123) + 1f) * 0.5f * (rangeMax.Y - rangeMin.Y) + rangeMin.Y,
                                          (MathUtils.PerlinNoise(value, period, octaves, seed+234) + 1f) * 0.5f * (rangeMax.Z - rangeMin.Z) + rangeMin.Z) * scaleXYZ  * scale;
        }


        [Input(Guid = "deddfbee-386d-4f8f-9339-ec6c01908a11")]
        public readonly InputSlot<float> Value = new InputSlot<float>();

        [Input(Guid = "1cd2174e-aeb2-4258-8395-a9cc16f276b5")]
        public readonly InputSlot<int> Seed = new InputSlot<int>();

        [Input(Guid = "03df41a8-3d72-47b1-b854-81e6e59e7cb4")]
        public readonly InputSlot<float> Frequency = new InputSlot<float>();

        [Input(Guid = "2693cb7d-33b3-4a0c-929f-e6911d2d4a0c")]
        public readonly InputSlot<int> Octaves = new InputSlot<int>();
        
        [Input(Guid = "B4B38D87-F661-4B8B-B978-70BF34152422")]
        public readonly InputSlot<Vector3> RangeMin = new InputSlot<Vector3>();

        [Input(Guid = "5401E715-7A82-43AB-BA16-D0E55A1D83D4")]
        public readonly InputSlot<Vector3> RangeMax = new InputSlot<Vector3>();

        [Input(Guid = "C427D83B-1046-4B8D-B44A-E616A64A702A")]
        public readonly InputSlot<Vector3> ScaleXYZ = new InputSlot<Vector3>();
        
        [Input(Guid = "E0F4333D-8BEE-4F9E-BB29-9F76BD72E61F")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();
        
    }
}