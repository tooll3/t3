using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_eb13f8e8_0fe1_47f0_8e86_45f85cf2f0f6
{
    public class RevisionPanic : Instance<RevisionPanic>
    {

        [Output(Guid = "bf45ca13-829b-46e4-ba46-886e3890c1be")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Output(Guid = "b5891c89-a8cf-4325-81be-f869b395e60c")]
        public readonly Slot<T3.Core.DataTypes.Command> Draws = new();


    }
}

