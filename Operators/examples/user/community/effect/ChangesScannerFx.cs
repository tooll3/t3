using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dbd8e542_730a_4417_badf_acc721a3eca8
{
    public class ChangesScannerFx : Instance<ChangesScannerFx>
    {
        [Output(Guid = "43eb6605-b042-4516-80bf-8326adb86c1e")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();


    }
}

