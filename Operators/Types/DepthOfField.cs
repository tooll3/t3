using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2cd650a1_5b77_4040_895b_6049dc09206e
{
    public class DepthOfField : Instance<DepthOfField>
    {
        [Output(Guid = "6771dc30-32e5-49af-a059-58de21e5155e")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "70bedeea-1594-4b2d-8ce2-e5573a57cde6")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Color = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "5c23dc93-192c-4aa7-a265-2b6bc407283d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "cde20e8a-a7f3-47b9-9336-2e03a4c98f43")]
        public readonly InputSlot<float> Near = new InputSlot<float>();

        [Input(Guid = "ac494dbc-0dd0-4d72-8a82-59c8166b7333")]
        public readonly InputSlot<float> Far = new InputSlot<float>();

        [Input(Guid = "6f1cfe39-d3eb-4e9c-904e-5c72920288f9")]
        public readonly InputSlot<float> FocusCenter = new InputSlot<float>();

        [Input(Guid = "616b0d12-f4d6-4b54-9745-25c510cab04f")]
        public readonly InputSlot<float> FocusRange = new InputSlot<float>();

        [Input(Guid = "30038b30-7247-4395-8fae-d69c2b4c0ec6")]
        public readonly InputSlot<float> BlurSize = new InputSlot<float>();

        [Input(Guid = "3ea66775-0864-40af-ab73-7f5321ef81aa")]
        public readonly InputSlot<float> QualityScale = new InputSlot<float>();

        [Input(Guid = "f66e71e9-aef9-41da-b722-843951e0dbd5")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> OutputTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

    }
}

