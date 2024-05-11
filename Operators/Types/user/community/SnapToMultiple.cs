using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c3090c65_194a_4f95_9b70_d003f54103f7 
{
    public class SnapToMultiple : Instance<SnapToMultiple>
    {
        [Output(Guid = "87a311ce-238c-472c-b43e-e4ed5268bbc5")]
        public readonly Slot<int> Result = new();

        public SnapToMultiple()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var v = Value.GetValue(context);
            var mod = Mod.GetValue(context);

            for(int nextLowestMod=mod;nextLowestMod>0;nextLowestMod--)
			{
				if(v%nextLowestMod == 0)
				{
					Result.Value = nextLowestMod;
					return;
				}
				
			}
			
        }
        
        [Input(Guid = "3bf35135-8fb1-46d3-95ea-008eded67060")]
        public readonly InputSlot<int> Value = new();

        [Input(Guid = "e122bf1d-8455-43f6-867f-4f43f3d6533c")]
        public readonly InputSlot<int> Mod = new();
    }
}
