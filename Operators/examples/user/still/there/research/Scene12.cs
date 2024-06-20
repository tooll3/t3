using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.still.there.research
{
	[Guid("12cbdef0-bcca-4bc7-96f5-d5806ca295af")]
    public class Scene12 : Instance<Scene12>
    {
        [Output(Guid = "b5cbda1b-d44f-4195-9ee4-f743b1591df7")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "2f952eff-24b7-4497-a52d-f4cd72a779d7")]
        public readonly Slot<Texture2D> DepthBuffer = new();


    }
}

