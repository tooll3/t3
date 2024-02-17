using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.learning.cs._05_physarum
{
	[Guid("796a66ff-93e7-4bf7-81c0-0fee903262b8")]
    public class Physarum02 : Instance<Physarum02>
    {
        [Output(Guid = "d7994d2f-a3ab-4adc-a522-4598bedc6ae2")]
        public readonly Slot<Texture2D> Output = new();


    }
}

