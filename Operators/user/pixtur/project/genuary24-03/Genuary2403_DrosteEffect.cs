using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.project.@genuary24_03
{
	[Guid("c9464818-294a-44d9-bcba-ec447d43ba52")]
    public class Genuary2403_DrosteEffect : Instance<Genuary2403_DrosteEffect>
    {
        [Output(Guid = "2a92f03c-6d86-4813-a96c-fe8fb391c931")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

