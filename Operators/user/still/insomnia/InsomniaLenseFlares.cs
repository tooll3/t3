using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.still.insomnia
{
	[Guid("41dbfc2d-06e7-40b2-a2f3-720fb1d28ed8")]
    public class InsomniaLenseFlares : Instance<InsomniaLenseFlares>
    {
        [Output(Guid = "b0ff0358-97cc-4952-a231-6c50b4038d7c")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "25c15c73-116b-4b58-8472-66f413d86bc1")]
        public readonly InputSlot<float> Brightness = new();

        [Input(Guid = "1aa4e644-d634-4d5e-a4fa-cdf14df08548")]
        public readonly InputSlot<int> RandomSeed = new();

        [Input(Guid = "487a77cf-e7a5-480f-8419-4069916c62b0")]
        public readonly InputSlot<int> LightIndex = new();

        [Input(Guid = "375d3301-9f6e-4c1d-996e-96d41d06aff1")]
        public readonly InputSlot<System.Numerics.Vector4> RandomizeColor = new();


    }
}

