using System.Collections.Generic;
using SharpDX;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_82fdcbe4_c21d_4e4b_a4f4_c945f1c40c0c
{
    public class ParticleConstants : Instance<ParticleConstants>
    {
        [Output(Guid = "9B7DCD58-CC08-479A-AF0B-C15B68768591")]
        public readonly Slot<int> Count = new Slot<int>();

        [Output(Guid = "3376C7A8-72BB-4DE1-8539-3B0EA817F66E")]
        public readonly Slot<Int3> DeadListInitDispatch = new Slot<Int3>();

        [Output(Guid = "FBB3D240-D59D-47B6-AE28-EDF7523686C9")]
        public readonly Slot<Int3> EmitDispatch = new Slot<Int3>();

        [Output(Guid = "9F60F331-58B0-4A1A-BA7B-CBDC25A4246A")]
        public readonly Slot<Int3> UpdateDispatch = new Slot<Int3>();
        
        public ParticleConstants()
        {
            Count.UpdateAction = Update;
            DeadListInitDispatch.UpdateAction = Update;
            EmitDispatch.UpdateAction = Update;
            UpdateDispatch.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (MaxCount.DirtyFlag.IsDirty)
            {
                int maxCount = MaxCount.GetValue(context);
                Count.Value = maxCount;
                DeadListInitDispatch.Value = new Int3(maxCount / 64, 1, 1);
                UpdateDispatch.Value = new Int3(maxCount / 64, 1, 1);
            }

            if (MaxEmitRatePerFrame.DirtyFlag.IsDirty)
            {
                EmitDispatch.Value = new Int3(MaxEmitRatePerFrame.GetValue(context), 1, 1);
            }
            Log.Info("constants updated");
        }

        [Input(Guid = "B0B1BCB1-9F02-4482-A323-89408B4AB347")]
        public readonly InputSlot<int> MaxCount = new InputSlot<int>(2048);
        [Input(Guid = "60318706-0594-4B02-9CA6-6FB9F94CF630")]
        public readonly InputSlot<int> MaxEmitRatePerFrame = new InputSlot<int>(16);
    }
}