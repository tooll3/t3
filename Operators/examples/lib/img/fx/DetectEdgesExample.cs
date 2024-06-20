using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
	[Guid("5f381544-0b6d-4e78-802a-c959c9686836")]
    public class DetectEdgesExample : Instance<DetectEdgesExample>
    {
        [Output(Guid = "333ed097-27f7-4ffa-b1c3-3d6c20cd25ac")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

