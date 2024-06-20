using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.sprites
{
	[Guid("876e870a-2e34-4adb-bdc9-878b312e633b")]
    public class DrawPointSpritesShadedExample : Instance<DrawPointSpritesShadedExample>
    {
        [Output(Guid = "7c5d452d-25fd-4f2f-b3a2-161dcbca3e38")]
        public readonly Slot<Texture2D> Output = new();


    }
}

