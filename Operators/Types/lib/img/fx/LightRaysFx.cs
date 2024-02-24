using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_53ff1a68_e888_444f_9ccc_23239c94d6db
{
    public class LightRaysFx : Instance<LightRaysFx>
    {
        [Output(Guid = "bdc413f2-9a15-4333-baba-aa57f73dda1a")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Input(Guid = "9d4f4e29-b2fe-415b-90f1-18390b520346")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "e110c111-34d5-4251-87eb-6cb56d2a026e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> TextureFX = new();

        [Input(Guid = "7d60c1e1-4924-48e8-991f-df83370c9a30")]
        public readonly InputSlot<System.Numerics.Vector2> Direction = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "c661ae7d-1cac-482f-b200-2d47946c22db")]
        public readonly InputSlot<int> Samples = new InputSlot<int>();

        [Input(Guid = "a1c47663-95a2-4a26-9b9a-f7e109868a21")]
        public readonly InputSlot<float> Density = new InputSlot<float>();

        [Input(Guid = "3f41e61f-52bb-41b4-b33c-3c0a00b2ff6e")]
        public readonly InputSlot<float> Weight = new InputSlot<float>();

        [Input(Guid = "06a0cd42-55b7-481e-91f5-ccdf914b3956")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "b26c20c8-ec60-4215-a53a-3e21cd98ea03")]
        public readonly InputSlot<float> Decay = new InputSlot<float>();

    }
}

