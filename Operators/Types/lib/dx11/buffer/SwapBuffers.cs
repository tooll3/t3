using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_020573c5_acaa_442e_9b1b_01338b0f4b62
{
    public class SwapBuffers : Instance<SwapBuffers>
    {
        [Output(Guid = "908EB00E-8951-4412-9112-1C10E806D57D")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> BufferA = new();

        [Output(Guid = "99175A08-D0FF-417B-B65C-CA18938EA03C")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> BufferB = new();

        
        public SwapBuffers()
        {
            BufferA.UpdateAction = Update;
            BufferB.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (EnableSwap.GetValue(context))
            {
                BufferA.Value = BufferBInput.GetValue(context);
                BufferB.Value = BufferAInput.GetValue(context);
            }
            else
            {
                BufferA.Value = BufferAInput.GetValue(context);
                BufferB.Value = BufferBInput.GetValue(context);
            }
        }

        [Input(Guid = "243160DD-C19D-48F2-B966-C9B4EE79C2D6")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> BufferAInput = new();

        [Input(Guid = "DE95E30B-896E-4880-B940-82863554CE02")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> BufferBInput = new();

        [Input(Guid = "3DC564AD-B534-4170-92A2-BF860D3604CB")]
        public readonly InputSlot<bool> EnableSwap = new();
        
    }
}