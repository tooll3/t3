using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_108a0a0d_a8d9_4898_af04_0dea9eef8968
{
    public class ComputeShaderCourse : Instance<ComputeShaderCourse>
    {

        [Output(Guid = "4716a457-7d92-481a-b651-566bc453cfb9")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> output = new();

    }
}

