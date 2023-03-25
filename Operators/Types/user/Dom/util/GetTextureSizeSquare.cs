using SharpDX.Direct3D11;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4bee2cb2_effe_4129_8d0e_183374159459
{
    public class GetTextureSizeSquare : Instance<GetTextureSizeSquare>
    {
        [Output(Guid = "5edc74b5-7b9a-4f84-b505-a691aebedfb0")]
        public readonly Slot<Size2> Result = new Slot<Size2>();

        [Output(Guid = "c88a65e1-194a-4a61-abeb-a64cd4a3983b")]
        public readonly Slot<int> ResultInt = new Slot<int>();


        [Input(Guid = "a6c7f778-26db-4e7c-b19c-1c462ee87f01")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();

        [Input(Guid = "34461e92-7217-4c2e-be63-27dca0eddb8d")]
        public readonly InputSlot<bool> UseMaxSize = new InputSlot<bool>();

    }
}

