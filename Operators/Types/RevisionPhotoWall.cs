using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fe766595_854e_4f84_80f0_06f2bc9e8ef4
{
    public class RevisionPhotoWall : Instance<RevisionPhotoWall>
    {
        [Output(Guid = "4e152b23-485d-4032-a62d-5c94153cdb45")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

