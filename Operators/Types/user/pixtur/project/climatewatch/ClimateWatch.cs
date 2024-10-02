using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2442724b_5db8_4d3f_a888_9473070e4173
{
    public class ClimateWatch : Instance<ClimateWatch>
    {
        [Output(Guid = "439b9932-e288-4827-adff-8cf8454ec10f")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

