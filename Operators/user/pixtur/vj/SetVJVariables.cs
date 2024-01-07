using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj
{
	[Guid("e14af8a3-8672-4348-af9e-735714c31c92")]
    public class SetVJVariables : Instance<SetVJVariables>
    {

        [Output(Guid = "a8127182-4b8d-4be2-8c50-9ce475d2699d")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();

        
        [Input(Guid = "693345bd-0cd8-4dca-9416-42a9bdcbc293")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();


    }
}

