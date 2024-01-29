using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_50aab941_0a29_474a_affd_13a74ea0c780
{
    public class PerlinNoise3 : Instance<PerlinNoise3>
    {
        [Output(Guid = "1666BC49-4DAE-4CB0-900B-80B50F913117")]
        public readonly Slot<Vector3> Result = new();

        public PerlinNoise3()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var value = OverrideTime.IsConnected
                            ? OverrideTime.GetValue(context)
                            : (float)context.LocalFxTime;
            
            var seed = Seed.GetValue(context);
            var period = Frequency.GetValue(context);
            var octaves = Octaves.GetValue(context);
            var rangeMin = RangeMin.GetValue(context);
            var rangeMax = RangeMax.GetValue(context);
            var scale = Amplitude.GetValue(context);
            var scaleXYZ = AmplitudeXYZ.GetValue(context);
            var gainBias = GainBias.GetValue(context);
            
            var scaleToUniformFactor = 1.37f;
            var x = ((MathUtils.PerlinNoise(value, period, octaves, seed)*scaleToUniformFactor + 1f) * 0.5f).ApplyGainBias(gainBias.X,gainBias.Y) 
                    * (rangeMax.X - rangeMin.X) + rangeMin.X;
            var y = ((MathUtils.PerlinNoise(value, period, octaves, seed + 123)*scaleToUniformFactor + 1f) * 0.5f).ApplyGainBias(gainBias.X,gainBias.Y)
                    * (rangeMax.Y - rangeMin.Y) + rangeMin.Y;
            var z = ((MathUtils.PerlinNoise(value, period, octaves, seed + 234)*scaleToUniformFactor + 1f) * 0.5f).ApplyGainBias(gainBias.X,gainBias.Y)
                    * (rangeMax.Z - rangeMin.Z) + rangeMin.Z;
            
            Result.Value  = new Vector3(x, y, z) * scaleXYZ  * scale;
        }

        [Input(Guid = "deddfbee-386d-4f8f-9339-ec6c01908a11")]
        public readonly InputSlot<float> OverrideTime = new();

        [Input(Guid = "03df41a8-3d72-47b1-b854-81e6e59e7cb4")]
        public readonly InputSlot<float> Frequency = new();

        [Input(Guid = "2693cb7d-33b3-4a0c-929f-e6911d2d4a0c")]
        public readonly InputSlot<int> Octaves = new();
        
        [Input(Guid = "E0F4333D-8BEE-4F9E-BB29-9F76BD72E61F")]
        public readonly InputSlot<float> Amplitude = new();
        
        [Input(Guid = "C427D83B-1046-4B8D-B44A-E616A64A702A")]
        public readonly InputSlot<Vector3> AmplitudeXYZ = new();
        
        
        [Input(Guid = "B4B38D87-F661-4B8B-B978-70BF34152422")]
        public readonly InputSlot<Vector3> RangeMin = new();

        [Input(Guid = "5401E715-7A82-43AB-BA16-D0E55A1D83D4")]
        public readonly InputSlot<Vector3> RangeMax = new();
        
        [Input(Guid = "B7B95FC2-C73F-4DB1-BDAB-2445FE62F032")]
        public readonly InputSlot<Vector2> GainBias = new();        

        [Input(Guid = "1cd2174e-aeb2-4258-8395-a9cc16f276b5")]
        public readonly InputSlot<int> Seed = new();

    }
}