using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.wake.summer2022.elements
{
	[Guid("46231897-662d-481c-97d9-1cea1fdb67be")]
    public class WS4_Boids : Instance<WS4_Boids>
    {
        [Output(Guid = "a772602a-7598-4e1b-9c43-e2fe8b24694d")]
        public readonly Slot<Texture2D> Output = new();


    }
}

