using System.Numerics;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_10673c38_8c7e_4aa1_8dcd_3f2711c709b5
{
    public class Random : Instance<Random>
    {
        [Output(Guid = "{DFB39F6E-7B1C-41F3-9F31-B71CAEE629F9}")]
        public readonly Slot<float> Result = new();

        public Random()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var seed = (uint)Seed.GetValue(context);

            var makeUniqueForChild = UniqueForChild.GetValue(context);
            
            var childId = this.SymbolChildId;
            var bigInteger= new BigInteger(childId.ToByteArray());
            var childSeed = makeUniqueForChild 
                                ? (uint)(bigInteger & 0xFFFFFFFF) 
                                : 0;
            
            var randomValue = MathUtils.Hash01(childSeed + seed);
            
            Result.Value = MathUtils.RemapAndClamp(randomValue, 
                                                   0f,1f,
                                                   Min.GetValue(context), Max.GetValue(context));
        }

        [Input(Guid = "{F2513EAD-7022-4774-8767-7F33D1B92B26}")]
        public readonly InputSlot<int> Seed = new();
        
        [Input(Guid = "48762E06-8377-464B-8FB9-C7D3B51C3F8E")]
        public readonly InputSlot<float> Min = new();

        [Input(Guid = "5755454F-98FE-49EF-9611-A7C3750C4F9A")]
        public readonly InputSlot<float> Max = new();

        [Input(Guid = "A16F6862-868B-4D83-88F5-C7AAC7616A38")]
        public readonly InputSlot<bool> UniqueForChild = new();
    }
}