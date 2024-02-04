using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.drd5
{
	[Guid("ce469ec7-708e-492f-8b6a-44eda127c28e")]
    public class EvokeInvitationExperiment : Instance<EvokeInvitationExperiment>
    {
        [Output(Guid = "ec890378-3b41-4d1a-9580-757bcba2d461")]
        public readonly Slot<Texture2D> Output = new();


    }
}

