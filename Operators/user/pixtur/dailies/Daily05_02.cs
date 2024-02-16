using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.dailies
{
	[Guid("fa3d2c7b-5adb-4590-8ed7-4822d9dbad7d")]
    public class Daily05_02 : Instance<Daily05_02>
    {
        [Output(Guid = "36cdf18d-8687-4526-a54a-3961cba1c5ff")]
        public readonly Slot<Texture2D> Output = new();


    }
}

