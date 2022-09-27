using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e14af8a3_8672_4348_af9e_735714c31c92
{
    public class SetVJVariables : Instance<SetVJVariables>
    {

        [Output(Guid = "a8127182-4b8d-4be2-8c50-9ce475d2699d")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();

        
        [Input(Guid = "693345bd-0cd8-4dca-9416-42a9bdcbc293")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "dc9285f2-dab8-4960-b74f-783cc675e5b8")]
        public readonly InputSlot<float> BeatThreshold = new InputSlot<float>();

        [Input(Guid = "4e8d6ee1-b210-48fb-ac32-e093f877de25")]
        public readonly InputSlot<float> HihatThreshold = new InputSlot<float>();

        [Input(Guid = "51bb8ea2-3afd-41ff-a9e6-7ac5a3c3fc9a")]
        public readonly InputSlot<float> AudioInGain = new InputSlot<float>();


    }
}

