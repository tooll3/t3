using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib._3d.postfx
{
    [Guid("9a7063ee-abde-45b0-b3f0-73a12721488f")]
    public class MotionBlurExample2 : Instance<MotionBlurExample2>
    {

        [Output(Guid = "b9dd0fe9-6661-4ea0-8607-8d2020eaf110")]
        public readonly Slot<T3.Core.DataTypes.Command> Output = new Slot<T3.Core.DataTypes.Command>();


    }
}

