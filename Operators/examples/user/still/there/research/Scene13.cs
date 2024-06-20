using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.still.there.research
{
	[Guid("0c3a8cc9-85bf-4ded-b35b-7b447c7e13dd")]
    public class Scene13 : Instance<Scene13>
    {
        [Output(Guid = "1fa366e9-d82c-468d-b026-2e484a2b88a0")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "c790d267-bd75-4bc4-a6bb-e89bcccbe6ae")]
        public readonly Slot<Texture2D> DepthBuffer = new();


    }
}

