using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib._3d.draw
{
	[Guid("78ee50b0-910b-4edc-b8cd-5c24b2f1b7d9")]
    public class DrawRibbonsExample : Instance<DrawRibbonsExample>
    {
        [Output(Guid = "b443e732-759c-4fb9-959c-f12f7a17cbf7")]
        public readonly Slot<Texture2D> Output = new();


    }
}

