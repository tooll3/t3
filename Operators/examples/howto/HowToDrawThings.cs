using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.howto
{
	[Guid("40757a26-1cd8-477c-a774-7463aadd6f0f")]
    public class HowToDrawThings : Instance<HowToDrawThings>
    {
        [Output(Guid = "85adcfb7-480a-4f41-87cb-bd7819467a68")]
        public readonly Slot<Texture2D> Texture = new();


    }
}

