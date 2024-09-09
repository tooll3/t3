using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2496ff9d_2953_4ed0_9550_b4d2ce322fa7
{
    public class ProceduralMoon1x : Instance<ProceduralMoon1x>
    {
        [Output(Guid = "e712ab39-2cfd-4b5e-94b8-4b2119e41139")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "198d1eb4-26df-4783-9069-2bc4cb1ea188")]
        public readonly InputSlot<float> MaterialResolution = new InputSlot<float>();

        [Input(Guid = "586559f0-bb73-42e7-9c95-5219e2f1ebf2")]
        public readonly InputSlot<float> CraterSize = new InputSlot<float>();

        [Input(Guid = "c4bad73d-6d57-4e39-9435-0e6975fb2a44")]
        public readonly InputSlot<float> MacroLandscapeScaling = new InputSlot<float>();

        [Input(Guid = "d8a1d61b-b32d-43bb-a74d-6da5efb3e2a8")]
        public readonly InputSlot<float> DetailLandscapeScaling = new InputSlot<float>();


    }
}

