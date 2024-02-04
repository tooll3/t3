using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.worksforeverybody
{
	[Guid("00485ce8-d342-4c97-aac3-1af8a7f03897")]
    public class WorksForEverybody : Instance<WorksForEverybody>
    {
        [Output(Guid = "de57341f-86c1-4426-b3c6-a5dc36490759")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

