using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.io.midi
{
    [Guid("cb2cdd1d-811a-4a30-a5bc-289f36c31028")]
    public class MidiNoteOutputExample : Instance<MidiNoteOutputExample>
    {
        [Output(Guid = "3a972c37-fcf8-4c5f-b932-97ac9f6d5d99")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

