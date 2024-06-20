using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.project.climatewatch
{
	[Guid("2442724b-5db8-4d3f-a888-9473070e4173")]
    public class ClimateWatch : Instance<ClimateWatch>
    {
        [Output(Guid = "439b9932-e288-4827-adff-8cf8454ec10f")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

