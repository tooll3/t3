using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.community.effect
{
    [Guid("dbd8e542-730a-4417-badf-acc721a3eca8")]
    public class ChangesScannerFx : Instance<ChangesScannerFx>
    {
        [Output(Guid = "43eb6605-b042-4516-80bf-8326adb86c1e")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();


    }
}

