using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
	[Guid("a2612284-7b74-449e-903e-536eaab4833f")]
    public class HoneyCombTilesExample : Instance<HoneyCombTilesExample>
    {
        [Output(Guid = "b5799d99-23aa-4c92-824e-29bc45f1ecb5")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

